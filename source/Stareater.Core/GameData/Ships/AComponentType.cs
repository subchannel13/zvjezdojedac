﻿using System;
using System.Collections.Generic;
using System.Linq;
using Stareater.Ships;
using Stareater.Utils;

namespace Stareater.GameData.Ships
{
	abstract class AComponentType
	{
		public const string LevelKey = "lvl";
		public const string LevelSuffix = "_lvl";
		public const string SizeKey = "size";
		
		public string IdCode { get; private set; }
		public string NameCode { get; private set; }
		public string DescCode { get; private set; }

		public IEnumerable<Prerequisite> Prerequisites { get; private set; }
		public int MaxLevel { get; private set; }
		public bool CanPick { get; private set; }
		
		protected AComponentType(string code, string nameCode, string descCode,
		                      IEnumerable<Prerequisite> prerequisites, int maxLevel, bool canPick)
		{
			this.IdCode = code;
			this.NameCode = nameCode;
			this.DescCode = descCode;
			this.Prerequisites = prerequisites;
			this.MaxLevel = maxLevel;
			this.CanPick = canPick;
		}
		
		public bool IsAvailable(IDictionary<string, double> techLevels)
		{
			return Prerequisite.AreSatisfied(Prerequisites, 0, techLevels);
		}
		
		public int HighestLevel(IDictionary<string, double> techLevels)
		{
			if (!IsAvailable(techLevels))
				throw new InvalidOperationException();
			
			for(int level = MaxLevel; level > 0; level--)
				if (Prerequisite.AreSatisfied(Prerequisites, level, techLevels))
					return level;
			
			return 0;
		}
		
		public virtual bool CanHaveMultiple
		{
			get { return false; }
		}
		
		public static Component<T> MakeBest<T>(IEnumerable<T> assortment, IDictionary<string, double> techLevels) where T: AComponentType, IIncrementalComponent
		{
			return Methods.FindBest(
				assortment.Where(x => x.IsAvailable(techLevels) && x.CanPick).Select(x => new Component<T>(x, x.HighestLevel(techLevels))),
				x => x.TypeInfo.ComparisonValue(x.Level)
			);
		}
	}
}
