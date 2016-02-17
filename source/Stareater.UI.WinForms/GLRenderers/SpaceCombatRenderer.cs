﻿using System;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Stareater.Controllers.Views;

namespace Stareater.GLRenderers
{
	class SpaceCombatRenderer : ARenderer
	{
		private const double DefaultViewSize = 20;
		private const double HexHeightScale = 0.9;
		
		private const float FarZ = -1;
		private const float Layers = 16.0f;
		
		private const float GridZ = -8 / Layers;
		
		private Matrix4 invProjection;
		private int gridList = NoCallList;
		
		public SpaceBattleController controller { get; private set; }

		#region ARenderer implementation
		public override void Draw(double deltaTime)
		{
			base.checkPerspective();
			
			drawGrid();
		}
		
		protected override void attachEventHandlers()
		{
			//no op
		}
		
		protected override void detachEventHandlers()
		{
			//no op
		}
		
		public override void ResetLists()
		{
			GL.DeleteLists(gridList, 1);
			this.gridList = NoCallList;
		}
		
		protected override void setupPerspective()
		{
			double aspect = eventDispatcher.Width / (double)eventDispatcher.Height;
			const double semiRadius = 0.5 * DefaultViewSize;

			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadIdentity();
			GL.Ortho(
				-aspect * semiRadius, aspect * semiRadius,
				-semiRadius, semiRadius, 
				0, -FarZ);

			GL.GetFloat(GetPName.ProjectionMatrix, out invProjection);
			invProjection.Invert();
			GL.MatrixMode(MatrixMode.Modelview);
		}
		#endregion
		
		private void drawGrid()
		{
			if (gridList == NoCallList)
			{
				this.gridList = GL.GenLists(1);
				GL.NewList(gridList, ListMode.CompileAndExecute);
				
				GL.Disable(EnableCap.Texture2D);
				GL.Color4(Color.Green);
				
				double yDist = Math.Sqrt(3) * HexHeightScale;
				for(int x = -SpaceBattleController.BattlefieldRadius; x <= SpaceBattleController.BattlefieldRadius; x++)
				{
					int yHeight = (SpaceBattleController.BattlefieldRadius * 2 + 1 - Math.Abs(x));
					double yOffset = - yHeight * yDist / 2.0;
						
					for(int y = 0; y < yHeight; y++)
					{
						GL.Begin(PrimitiveType.TriangleStrip);
						for(int i = 0; i <= 6; i++)
						{
							GL.Vertex3(0.95 * Math.Cos(i * Math.PI / 3) + x * 1.5, 0.95 * Math.Sin(i * Math.PI / 3) * HexHeightScale + y * yDist + yOffset, GridZ);
							GL.Vertex3(1.05 * Math.Cos(i * Math.PI / 3) + x * 1.5, 1.05 * Math.Sin(i * Math.PI / 3) * HexHeightScale + y * yDist + yOffset, GridZ);
						}
						GL.End();
					}
				}
				
				GL.EndList();
			}
			else
				GL.CallList(gridList);
		}
	}
}