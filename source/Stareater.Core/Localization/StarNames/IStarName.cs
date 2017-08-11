﻿using Ikadn.Ikon.Types;
using Stareater.Galaxy;
using Stareater.Utils.StateEngine;

namespace Stareater.Localization.StarNames
{
	[StateBaseType("loadName", typeof(StarData))]
	public interface IStarName
	{
		string ToText(Language language);
		IkonBaseObject Save(SaveSession session);

		//TODO(v0.7) remove
		IkonBaseObject Save();
	}
}
