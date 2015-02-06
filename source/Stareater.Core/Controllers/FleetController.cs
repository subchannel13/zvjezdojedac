﻿using System;
using System.Collections.Generic;
using System.Linq;

using NGenerics.DataStructures.Mathematical;
using Stareater.Controllers.Data;
using Stareater.Controllers.Data.Ships;
using Stareater.Galaxy;
using Stareater.Ships.Missions;

namespace Stareater.Controllers
{
	public class FleetController
	{
		private Game game;
		private Fleet fleet;
		
		private HashSet<ShipGroup> selection = new HashSet<ShipGroup>();
		private List<Vector2D> simulationWaypoints = null;
		
		internal FleetController(Fleet fleet, Game game)
		{
			this.fleet = fleet;
			this.game = game;
		}
		
		public bool Valid
		{
			get { return this.game.States.Fleets.Contains(this.fleet); }
		}
		
		public IEnumerable<ShipGroupInfo> ShipGroups
		{
			get
			{
				return this.fleet.Ships.Select(x => new ShipGroupInfo(x));
			}
		}
		
		public bool CanMove
		{
			get { return true; }
		}
		
		public IList<Vector2D> SimulationWaypoints
		{
			get { return this.simulationWaypoints; }
		}
		
		public void DeselectGroup(ShipGroupInfo group)
		{
			selection.Remove(group.Data);
		}
		
		public void SelectGroup(ShipGroupInfo group)
		{
			selection.Add(group.Data);
		}
		
		public FleetController Send(IEnumerable<Vector2D> waypoints)
		{
			//TODO(0.5) fleet splitting and merging
			this.game.CurrentPlayer.Orders.ShipOrders[this.fleet] = new MoveMission(waypoints);
			
			return new FleetController(this.fleet, this.game);
		}
		
		public void SimulateTravel(StarData destination)
		{
			this.simulationWaypoints = new List<Vector2D>();
			//TODO(later): find shortest path
			this.simulationWaypoints.Add(this.fleet.Position);
			this.simulationWaypoints.Add(destination.Position);
		}
	}
}
