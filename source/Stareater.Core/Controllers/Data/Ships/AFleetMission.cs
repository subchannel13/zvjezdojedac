﻿using System;
using Stareater.Ships.Missions;

namespace Stareater.Controllers.Data.Ships
{
	public abstract class AFleetMission
	{
		public abstract FleetMissionType Type { get; }
	}
}
