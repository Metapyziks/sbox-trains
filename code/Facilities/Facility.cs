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
		private HexBounds[] _localFootprint = { HexCoord.Zero };

		public FacilityType Type { get; set; }

		public bool IsInitialized { get; set; }

		[Net]
		public HexBounds LocalBounds { get; set; }

		public HexCoord HexPosition { get; set; }

		[Net]
		public HexBounds[] LocalFootprint
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
			LocalBounds = HexBounds.Empty;

			foreach ( var bounds in LocalFootprint )
			{
				LocalBounds = LocalBounds.Union( bounds );
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
			var catchmentBounds = ((HexBounds)localHexCoord).Expand( CatchmentRadius );

			if ( !LocalBounds.Intersects( catchmentBounds ) ) return false;

			foreach ( var bounds in LocalFootprint )
			{
				if ( bounds.Intersects( catchmentBounds ) ) return true;
			}

			return false;
		}
	}
}
