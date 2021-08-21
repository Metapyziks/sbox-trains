using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox;

namespace Ziks.Trains.Facilities
{

	public partial class FacilityManager : Entity
	{
		public FacilityManager()
		{
			EnableDrawing = false;
			Transmit = TransmitType.Always;
		}

		[Net, OnChangedCallback]
		public List<Facility> Facilities { get; private set; } = new();

		private int _initializedFacilityCount;

		private readonly Dictionary<HexCoord, Facility> _footprintCoords = new();
		private readonly Dictionary<HexCoord, Facility> _catchmentCoords = new();

		public Facility SpawnFacility( HexCoord origin, FacilityType type )
		{
			Host.AssertServer();

			var facility = new Facility
			{
				Type = type,
				HexPosition = origin,
				Position = Game.Current.HexGrid.GetWorldPosition( origin ),
				Rotation = Rotation.FromYaw( Facilities.Count * 30f )
			};

			Facilities.Add( facility );

			UpdateCoordDictionaries( facility );

			return facility;
		}

		private void OnFacilitiesChanged()
		{
			UpdateCoordDictionaries();
		}

		private void UpdateCoordDictionaries()
		{
			var hexGrid = Game.Current.HexGrid;

			foreach ( var facility in Facilities.Skip( _initializedFacilityCount ) )
			{
				if ( facility.IsInitialized ) continue;

				facility.HexPosition = hexGrid.GetHexCoord( facility.Position );

				UpdateCoordDictionaries( facility );
			}

			_initializedFacilityCount = Facilities.Count;
		}

		private void UpdateCoordDictionaries( Facility facility )
		{
			facility.IsInitialized = true;

			foreach ( var localBounds in facility.LocalFootprint )
			{
				var footprintBounds = localBounds + facility.HexPosition;
				var catchmentBounds = footprintBounds.Expand( facility.CatchmentRadius );

				var footprintCoordCount = footprintBounds.CoordinateCount;
				var catchmentCoordCount = catchmentBounds.CoordinateCount;

				for ( var i = 0; i < footprintCoordCount; ++i )
				{
					var coord = footprintBounds.GetCoordinate( i );

					if ( _footprintCoords.TryGetValue( coord, out var other ) )
					{
						if ( other == facility ) continue;

						Log.Error( "Two facilities have intersecting footprints!" );
					}
					else
					{
						_footprintCoords.Add( coord, facility );
					}
				}

				for ( var i = footprintCoordCount; i < catchmentCoordCount; ++i )
				{
					var coord = catchmentBounds.GetCoordinate( i );

					if ( _catchmentCoords.TryGetValue( coord, out var other ) )
					{
						if ( other == facility ) continue;

						Log.Error( "Two facilities have intersecting catchments!" );
					}
					else
					{
						_catchmentCoords.Add( coord, facility );
					}
				}
			}
		}

		public Facility GetFacilityFromFootprint( HexCoord coord )
		{
			return _footprintCoords.TryGetValue( coord, out var facility ) ? facility : null;
		}

		public Facility GetFacilityFromCatchment( HexCoord coord )
		{
			return _catchmentCoords.TryGetValue( coord, out var facility ) ? facility : null;
		}
	}
}
