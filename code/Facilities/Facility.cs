using System;
using System.Linq;
using Sandbox;
using Ziks.Trains;

namespace Ziks.Trains.Facilities
{
	public enum FacilityType
	{
		Mine,
		PowerPlant
	}

	public partial class Facility : ModelEntity
	{
		private HexCircle[] _localFootprint = { HexCoord.Zero };

		public FacilityType Type { get; set; }

		public bool IsInitialized { get; set; }

		[Net]
		public HexCircle LocalBounds { get; set; }

		public HexCoord HexPosition { get; set; }

		[Net]
		public HexCircle[] LocalFootprint
		{
			get => _localFootprint;
			set
			{
				_localFootprint = value?.Distinct().ToArray() ?? throw new ArgumentNullException();
				UpdateLocalBounds();
			}
		}

		[Net]
		public int CatchmentRadius { get; set; } = 2;

		public Facility()
		{
			if ( Host.IsServer ) SetModel( "models/facility.vmdl" );
		}

		private void UpdateLocalBounds()
		{
			LocalBounds = new HexCircle( HexCoord.Zero, 0 );

			foreach ( var bounds in LocalFootprint )
			{
				LocalBounds = LocalBounds.ExpandToContain( bounds );
			}
		}

		public bool IsWithinFootprint( HexCoord hexCoord )
		{
			var localHexCoord = hexCoord - HexPosition;

			if ( !LocalBounds.Contains( localHexCoord ) ) return false;

			foreach ( var bounds in LocalFootprint )
			{
				if ( bounds.Contains( localHexCoord ) ) return true;
			}

			return false;
		}

		public bool IsWithinCatchment( HexCoord hexCoord )
		{
			var localHexCoord = hexCoord - HexPosition;
			var catchmentBounds = ((HexCircle)localHexCoord).Expand( CatchmentRadius );

			if ( !LocalBounds.Intersects( catchmentBounds ) ) return false;

			foreach ( var bounds in LocalFootprint )
			{
				if ( bounds.Intersects( catchmentBounds ) ) return true;
			}

			return false;
		}
	}
}
