﻿using System;
using System.Linq;
using Ikadn.Ikon.Types;
using Stareater.Controllers;
using Stareater.Controllers.Views;
using Stareater.Controllers.Views.Combat;

namespace Stareater.Players.DefaultAI
{
	class DefaultAIPlayer : IOffscreenPlayer, IBattleEventListener
	{
		private readonly Random random = new Random();
		private PlayerController playerController;
		private SpaceBattleController battleController;
		
		public PlayerController Controller 
		{ 
			set { this.playerController = value; }
		}
		
		public void PlayTurn()
		{
			foreach(var stellaris in this.playerController.Stellarises())
			{
				StarSystemController starSystem = this.playerController.OpenStarSystem(stellaris.HostStar);
				manage(starSystem.StellarisController());
				
				foreach(var planet in starSystem.Planets)
					if (starSystem.BodyType(planet.Position) == BodyType.OwnColony)
						manage(starSystem.ColonyController(planet.Position));
			}
			
			this.playerController.EndGalaxyPhase();
		}

		public IBattleEventListener StartBattle(SpaceBattleController controller)
		{
			this.battleController = controller;
			return this;
		}

		#region IBattleEventListener implementation
		public void PlayUnit(CombatantInfo unitInfo)
		{
			this.battleController.UnitDone();
		}
		#endregion
		
		public Ikadn.IkadnBaseObject Save()
		{
			var data = new IkonComposite(PlayerType.AiControllerTag);
			data.Add(PlayerType.FactoryIdKey, new IkonText(DefaultAIFactory.FactoryId));
			
			return data;
		}

		private void manage(AConstructionSiteController controller)
		{
			var options = controller.ConstructableItems.ToArray();
			if (options.Length == 0)
				return;
			
			while(controller.ConstructionQueue.Any())
				controller.Dequeue(0);
			controller.Enqueue(options[random.Next(options.Length)]);
		}
	}
}
