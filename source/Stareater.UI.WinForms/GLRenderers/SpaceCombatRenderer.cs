﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NGenerics.DataStructures.Mathematical;
using OpenTK;
using Stareater.Controllers;
using Stareater.Controllers.Views;
using Stareater.Controllers.Views.Combat;
using Stareater.Controllers.Views.Ships;
using Stareater.Galaxy;
using Stareater.Utils;
using Stareater.Utils.NumberFormatters;
using Stareater.GLData;
using Stareater.GLData.OrbitShader;
using Stareater.GraphicsEngine;
using Stareater.GLData.SpriteShader;

namespace Stareater.GLRenderers
{
	class SpaceCombatRenderer : AScene
	{
		private const float FarZ = 1;
		private const float Layers = 16.0f;
		
		private const float CellBackgroundZ = 6 / Layers;
		private const float GridZ = 5 / Layers;
		private const float PlanetColorZ = 4 / Layers;
		private const float StarColorZ = 4 / Layers;
		private const float CombatantZ = 3 / Layers;
		private const float MoreCombatantsZ = 2 / Layers;
		private const float MovemenentZ = 1 / Layers;
		
		private const float DefaultViewSize = 17;
		private const float HexHeightScale = 0.9f;
		private static readonly float HexHeight = (float)Math.Sqrt(3) * HexHeightScale;
		private static readonly Matrix4 PopulationTransform = Matrix4.CreateScale(0.2f, 0.2f, 1) * Matrix4.CreateTranslation(0.5f, -0.5f, 0);
		
		private const double AnimationPeriod = 1.5;
		private static readonly Color SelectionColor = Color.Yellow;
		
		private double animationTime = 0;
		private SceneObject gridLines = null;
		private IEnumerable<SceneObject> movementSprites = null;
		private IEnumerable<SceneObject> planetSprites = null;
		private IEnumerable<SceneObject> unitSprites = null;
		private SceneObject starSprite = null;
		
		private CombatantInfo currentUnit = null;
		private PolygonData currentUnitDrawable = null;
		
		public SpaceBattleController Controller { get; private set; }

		public AbilityInfo SelectedAbility { private get; set; }
		
		public void StartCombat(SpaceBattleController controller)
		{
			this.Controller = controller;
			this.ResetLists();
		}
		
		#region ARenderer implementation
		protected override void FrameUpdate(double deltaTime)
		{
			this.animationTime += deltaTime;
			
			double animationPhase = Methods.GetPhase(this.animationTime, AnimationPeriod);
			var alpha = Math.Abs(animationPhase - 0.5) * 0.6 + 0.4;
			if (this.Controller.Units.Select(x => x.Owner).Distinct().All(x => this.currentUnit.CloakedFor(x) || x == currentUnit.Owner))
				alpha *= 0.65;
			
			//TODO(v0.6) update selected unit draw data
			//Color = new Color4(this.currentUnit.Owner.Color.R, this.currentUnit.Owner.Color.G, this.currentUnit.Owner.Color.B, (byte)(alpha * 255));
		}
		
		//TODO(0.6) refactor and remove
		public void ResetLists()
		{
			this.setupBodies();
			this.setupGrid();
			this.setupUnits();
		}
		
		protected override Matrix4 calculatePerspective()
		{
			var aspect = canvasSize.X / canvasSize.Y;
			return calcOrthogonalPerspective(aspect * DefaultViewSize, DefaultViewSize, FarZ, new Vector2());
		}
		#endregion
		
		#region Mouse events
		public override void OnMouseClick(MouseEventArgs e)
		{
			Vector4 mousePoint = Vector4.Transform(mouseToView(e.X, e.Y), invProjection);
			
			var hexX = Math.Round(mousePoint.X / 1.5, MidpointRounding.AwayFromZero);
			var hex = new NGenerics.DataStructures.Mathematical.Vector2D(
				hexX,
				Math.Round(mousePoint.Y / HexHeight - ((int)Math.Abs(hexX) % 2 != 0 ? 0.5 : 0), MidpointRounding.AwayFromZero)
			);
			
			var enemies = this.Controller.Units.Where(x => x.Position == hex && x.Owner != this.currentUnit.Owner).ToList();
			if (enemies.Any() && this.SelectedAbility != null)
				this.Controller.UseAbility(this.SelectedAbility, biggestStack(enemies));
			else if (this.Controller.Planets.Any(x => x.Position == hex && x.Owner != this.currentUnit.Owner) && this.SelectedAbility != null)
				this.Controller.UseAbility(this.SelectedAbility, this.Controller.Planets.First(x => x.Position == hex));
			else
				this.Controller.MoveTo(hex);
			
			this.ResetLists();
		}
		#endregion
		
		public void OnUnitTurn(CombatantInfo unitInfo)
		{
			this.currentUnit = unitInfo;
			this.SelectedAbility = unitInfo.Abilities.FirstOrDefault(x => x.Quantity > 0);
			this.setupBodies();
			this.setupUnits();
		}
		
		public void OnUnitDone()
		{
			this.Controller.UnitDone();
		}
		
		private IEnumerable<PolygonData> planetSpriteData(CombatPlanetInfo planet)
		{
			var planetTransform = Matrix4.CreateTranslation(hexX(planet.Position), hexY(planet.Position), 0);
			var sprite = new TextureInfo();

			switch(planet.Type)
			{
				case PlanetType.Asteriod:
					sprite = GalaxyTextures.Get.Asteroids;
					break;
				case PlanetType.GasGiant:
					sprite = GalaxyTextures.Get.GasGiant;
					break;
				case PlanetType.Rock:
					sprite = GalaxyTextures.Get.RockPlanet;
					break;
			}
			
			yield return new PolygonData(
				PlanetColorZ,
				new SpriteData(planetTransform, PlanetColorZ, sprite.Id, Color.White),
				SpriteHelpers.UnitRectVertexData(sprite)
			);
			
			if (planet.Population > 0)
				yield return new PolygonData(
					MoreCombatantsZ,
					new SpriteData(PopulationTransform * planetTransform, MoreCombatantsZ, TextRenderUtil.Get.TextureId, planet.Owner != null ? planet.Owner.Color : Color.Gray),
					TextRenderUtil.Get.BufferText(new ThousandsFormatter().Format(planet.Population), -1, Matrix4.Identity).ToList()
				);
		}
		
		private IEnumerable<PolygonData> unitSpriteData(IGrouping<Vector2D, CombatantInfo> hex, IEnumerable<PlayerInfo> players)
		{
			var hexTransform = Matrix4.CreateTranslation(hexX(hex.Key), hexY(hex.Key), 0);
			
			var unitSelected = (this.currentUnit != null && this.currentUnit.Position == hex.Key);
			var unit = unitSelected ? this.currentUnit : biggestStack(hex);
			var unitSprite = GalaxyTextures.Get.Sprite(unit.Design.ImagePath);
			var alpha = players.All(x => unit.CloakedFor(x) || x == unit.Owner) ? 0.65 : 1;
			
			var unitDrawable = new PolygonData(
				CombatantZ,
				new SpriteData(hexTransform, CombatantZ, unitSprite.Id, Color.FromArgb((int)(alpha * 255), unit.Owner.Color)),
				SpriteHelpers.UnitRectVertexData(unitSprite)
			);
			if (unitSelected)
				this.currentUnitDrawable = unitDrawable;
			
			yield return unitDrawable;
			
			var otherUnits = hex.Where(x => x != unit).Select(x => x.Owner).Distinct().ToList();
			for(int i = 0; i < otherUnits.Count; i++)
				yield return new PolygonData(
					CombatantZ,
					new SpriteData(
						Matrix4.CreateScale(0.2f, 0.2f, 1) * Matrix4.CreateTranslation(0.5f, 0.2f * i + 0.5f, 0) * hexTransform, 
						MoreCombatantsZ, 
						GalaxyTextures.Get.FleetIndicator.Id,
						otherUnits[i].Color
					),
					SpriteHelpers.UnitRectVertexData(GalaxyTextures.Get.FleetIndicator)
				);
			
			yield return new PolygonData(
				CombatantZ,
				new SpriteData(
					Matrix4.CreateScale(0.2f, 0.2f, 1) * Matrix4.CreateTranslation(0.5f, -0.5f, 0) * hexTransform,
					MoreCombatantsZ,
					TextRenderUtil.Get.TextureId,
					Color.Gray
				),
				TextRenderUtil.Get.BufferText(new ThousandsFormatter().Format(unit.Count), -1, Matrix4.Identity).ToList()
			);
		}
		
		private void setupBodies()
		{
			this.UpdateScene(
				ref this.starSprite,
				new SceneObject(new PolygonData(
					StarColorZ,
					new SpriteData(Matrix4.Identity, StarColorZ, GalaxyTextures.Get.StarColor.Id, this.Controller.Star.Color),
					SpriteHelpers.UnitRectVertexData(GalaxyTextures.Get.SystemStar)
				))
			);

			this.UpdateScene(
				ref this.planetSprites,
				this.Controller.Planets.Select(planet => new SceneObject(planetSpriteData(planet))).ToList()
			);
		}
		
		private void setupGrid()
		{
			var gridVertexData = new List<float>();
			
			for(int x = -SpaceBattleController.BattlefieldRadius; x <= SpaceBattleController.BattlefieldRadius; x++)
			{
				int yHeight = (SpaceBattleController.BattlefieldRadius * 2 - Math.Abs(x));
				var yOffset = Math.Abs(x) % 2 != 0 ? HexHeight / 2 : 0;
					
				for(int y = -(int)Math.Ceiling(yHeight / 2.0); y <= (int)Math.Floor(yHeight / 2.0); y++)
				{
					var nearPoints = new List<Vector2>();
					var farPoints = new List<Vector2>();
					for(int i = 0; i < 6; i++)
					{
						var cos = (float)Math.Cos(i * Math.PI / 3);
						var sin = (float)Math.Sin(i * Math.PI / 3);
						nearPoints.Add(new Vector2(0.95f * cos + x * 1.5f, 0.95f * sin * HexHeightScale + y * HexHeight + yOffset));
						farPoints.Add(new Vector2(1.05f * cos + x * 1.5f, 1.05f * sin * HexHeightScale + y * HexHeight + yOffset));
					}
					
					for(int i = 0; i < 6; i++)
					{
						var j = (i + 1) % 6;
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(nearPoints[j].X, nearPoints[j].Y));
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(farPoints[j].X, farPoints[j].Y));
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(farPoints[i].X, farPoints[i].Y));
						
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(farPoints[i].X, farPoints[i].Y));
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(nearPoints[i].X, nearPoints[i].Y));
						gridVertexData.AddRange(OrbitHelpers.FlatOrbitVertex(nearPoints[j].X, nearPoints[j].Y));
					}
				}
			}
			
			this.UpdateScene(
				ref this.gridLines,
				new SceneObject(new PolygonData(
					GridZ,
					new OrbitData(GridZ, 0, 1, Color.Green, Matrix4.Identity),
					gridVertexData
				))
			);
		}
		
		private void setupUnits()
		{
			var units = this.Controller.Units.GroupBy(x => x.Position);
			var players = this.Controller.Units.Select(x => x.Owner).Distinct();
			
			this.UpdateScene(
				ref this.unitSprites,
				units.Select(hex => new SceneObject(unitSpriteData(hex, players))).ToList()
			);
			
			if (this.currentUnit != null)
				this.setupValidMoves();
		}
		
		private void setupValidMoves()
		{
			var center = new Vector2(hexX(this.currentUnit.Position), hexY(this.currentUnit.Position));
			var arrowData = new List<SpriteData>();
			
			foreach(var move in this.currentUnit.ValidMoves)
			{
				var moveTransform = Matrix4.CreateTranslation(hexX(move), hexY(move), 0);
				
				var direction = new Vector2(hexX(move), hexY(move)) - center;
				if (direction.LengthSquared > 0)
				{
					direction.Normalize();
					moveTransform = new Matrix4(
						direction.X, direction.Y, 0, 0,
						direction.Y, -direction.X, 0, 0,
						0, 0, 1, 0,
						0, 0, 0, 1
					) * moveTransform;
				}
				
				arrowData.Add(new SpriteData(
					Matrix4.CreateScale(0.4f, 0.4f, 1) * Matrix4.CreateTranslation(-0.25f, 0, 0) * moveTransform, 
					MovemenentZ, 
					GalaxyTextures.Get.MoveToArrow.Id, 
					Methods.HexDistance(move) <= SpaceBattleController.BattlefieldRadius ? Color.Green : Color.White
				));
			}
			
			this.UpdateScene(
				ref this.movementSprites,
				arrowData.Select(arrow => new SceneObject(new PolygonData(
					MovemenentZ,
					arrow,
					SpriteHelpers.UnitRectVertexData(GalaxyTextures.Get.MoveToArrow)
				))).ToList()
			);
		}
		
		#region Helper methods
		private CombatantInfo biggestStack(IEnumerable<CombatantInfo> combatants)
		{
			return combatants.Aggregate((a, b) => a.Count * a.Design.Size > b.Count * b.Design.Size ? a : b);
		}

		private static float hexX(Vector2D coordinate)
		{
			return (float)(coordinate.X * 1.5);
		}
		
		private static float hexY(Vector2D coordinate)
		{
			return (float)(HexHeight * (coordinate.Y + ((int)Math.Abs(coordinate.X) % 2 != 0 ? 0.5 : 0)));
		}
		#endregion
	}
}
