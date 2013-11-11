﻿using System;
using System.Collections.Generic;
using Stareater.GameData;
using Stareater.Players;

namespace Stareater.GameLogic
{
	class PlayerProcessor
	{
		public const string LevelSufix = "Lvl";
		
		public Player Player { get; private set; }
		
		public PlayerProcessor(Player player, IEnumerable<Technology> technologies)
		{
			this.Player = player;
			
			this.TechLevels = new Dictionary<string, double>();
			foreach (var tech in technologies) {
				this.TechLevels.Add(tech.IdCode + LevelSufix, TechnologyProgress.NotStarted);
			}
		}

		public PlayerProcessor(Player player)
		{
			this.Player = player;
		}

		public IDictionary<string, double> TechLevels { get; private set; }

		public void Calculate(IEnumerable<TechnologyProgress> techAdvances)
		{
			foreach (var tech in techAdvances) {
				TechLevels[tech.Topic.IdCode + LevelSufix] = tech.Level;
			}
		}

		internal PlayerProcessor Copy(PlayersRemap playersRemap)
		{
			PlayerProcessor copy = new PlayerProcessor(playersRemap.Players[this.Player]);
			
			copy.TechLevels = new Dictionary<string, double>(this.TechLevels);

			return copy;
		}
	}
}
