using System;
using System.Collections.Generic;

namespace Ziks.Trains.Track
{
	public static class RailExtensions
	{
		public static RailTile Combine( this RailTile tile, RailTile other )
		{
			var combined = tile | other;

			if ( (combined & RailTile.Buffer) == 0 )
			{
				return combined;
			}

			var combinedWithoutBuffer = combined & ~RailTile.Buffer;

			for ( var i = 0; i < 6; ++i )
			{
				var edge = (HexEdge)i;
				var buffer = (RailTile)(1 << (i + 9));

				if ( combinedWithoutBuffer.Touches( edge ) )
				{
					combined &= ~buffer;
				}
			}

			return combined;
		}

		public static bool Touches( this RailPiece piece, HexEdge edge )
		{
			return piece.ToTile().Touches( edge );
		}

		public static bool Touches( this RailTile tile, HexEdge edge )
		{
			return tile.FilterByEdge( edge ) != 0;
		}

		public static RailTile FilterByEdge( this RailTile tile, HexEdge edge )
		{
			switch ( edge )
			{
				case HexEdge.Top:
					return tile & (RailTile.TopBottomRight | RailTile.TopBottom | RailTile.TopBottomLeft |
					               RailTile.TopBuffer);

				case HexEdge.TopRight:
					return tile & (RailTile.TopRightBottom | RailTile.TopRightBottomLeft | RailTile.TopRightTopLeft |
					               RailTile.TopRightBuffer);

				case HexEdge.BottomRight:
					return tile & (RailTile.BottomRightBottomLeft | RailTile.BottomRightTopLeft |
					               RailTile.BottomRightTop | RailTile.BottomRightBuffer);

				case HexEdge.Bottom:
					return tile & (RailTile.BottomTopLeft | RailTile.BottomTop | RailTile.BottomTopRight |
					               RailTile.BottomBuffer);

				case HexEdge.BottomLeft:
					return tile & (RailTile.BottomLeftTop | RailTile.BottomLeftTopRight |
					               RailTile.BottomLeftBottomRight | RailTile.BottomLeftBuffer);

				case HexEdge.TopLeft:
					return tile & (RailTile.TopLeftTopRight | RailTile.TopLeftBottomRight | RailTile.TopLeftBottom |
					               RailTile.TopLeftBuffer);

				default:
					return RailTile.None;
			}
		}

		public static HexEdge GetConnectedEdge( this RailPiece piece, HexEdge edge )
		{
			switch (piece, edge)
			{
				case (RailPiece.TopBottom, HexEdge.Top):
					return HexEdge.Bottom;
				case (RailPiece.TopBottom, HexEdge.Bottom):
					return HexEdge.Top;

				case (RailPiece.BottomLeftTopRight, HexEdge.TopRight):
					return HexEdge.BottomLeft;
				case (RailPiece.BottomLeftTopRight, HexEdge.BottomLeft):
					return HexEdge.TopRight;

				case (RailPiece.BottomRightTopLeft, HexEdge.BottomRight):
					return HexEdge.TopLeft;
				case (RailPiece.BottomRightTopLeft, HexEdge.TopLeft):
					return HexEdge.BottomRight;

				case (RailPiece.BottomTopLeft, HexEdge.Bottom):
					return HexEdge.TopLeft;
				case (RailPiece.BottomTopLeft, HexEdge.TopLeft):
					return HexEdge.Bottom;

				case (RailPiece.TopBottomLeft, HexEdge.Top):
					return HexEdge.BottomLeft;
				case (RailPiece.TopBottomLeft, HexEdge.BottomLeft):
					return HexEdge.Top;

				case (RailPiece.TopLeftTopRight, HexEdge.TopLeft):
					return HexEdge.TopRight;
				case (RailPiece.TopLeftTopRight, HexEdge.TopRight):
					return HexEdge.TopLeft;

				case (RailPiece.TopBottomRight, HexEdge.Top):
					return HexEdge.BottomRight;
				case (RailPiece.TopBottomRight, HexEdge.BottomRight):
					return HexEdge.Top;

				case (RailPiece.BottomTopRight, HexEdge.Bottom):
					return HexEdge.TopRight;
				case (RailPiece.BottomTopRight, HexEdge.TopRight):
					return HexEdge.Bottom;

				case (RailPiece.BottomLeftBottomRight, HexEdge.BottomLeft):
					return HexEdge.BottomRight;
				case (RailPiece.BottomLeftBottomRight, HexEdge.BottomRight):
					return HexEdge.BottomLeft;

				case (RailPiece.TopBuffer, _):
				case (RailPiece.TopRightBuffer, _):
				case (RailPiece.BottomRightBuffer, _):
				case (RailPiece.BottomBuffer, _):
				case (RailPiece.BottomLeftBuffer, _):
				case (RailPiece.TopLeftBuffer, _):
					throw new ArgumentOutOfRangeException( nameof(piece) );

				default:
					throw new ArgumentOutOfRangeException( nameof(edge) );
			}
		}

		public static RailTile ToTile( this RailPiece railPiece )
		{
			return (RailTile)(1 << (int)railPiece);
		}

		public static bool TryGetRailPiece( HexEdge a, HexEdge b, out RailPiece railPiece )
		{
			if ( a > b )
			{
				var temp = a;
				a = b;
				b = temp;
			}

			switch (a, b)
			{
				case (HexEdge.Top, HexEdge.BottomRight):
					railPiece = RailPiece.TopBottomRight;
					return true;

				case (HexEdge.Top, HexEdge.Bottom):
					railPiece = RailPiece.TopBottom;
					return true;

				case (HexEdge.Top, HexEdge.BottomLeft):
					railPiece = RailPiece.TopBottomLeft;
					return true;

				case (HexEdge.TopRight, HexEdge.Bottom):
					railPiece = RailPiece.TopRightBottom;
					return true;

				case (HexEdge.TopRight, HexEdge.BottomLeft):
					railPiece = RailPiece.TopRightBottomLeft;
					return true;

				case (HexEdge.TopRight, HexEdge.TopLeft):
					railPiece = RailPiece.TopLeftTopRight;
					return true;

				case (HexEdge.BottomRight, HexEdge.BottomLeft):
					railPiece = RailPiece.BottomRightBottomLeft;
					return true;

				case (HexEdge.BottomRight, HexEdge.TopLeft):
					railPiece = RailPiece.BottomRightTopLeft;
					return true;

				case (HexEdge.Bottom, HexEdge.TopLeft):
					railPiece = RailPiece.BottomTopLeft;
					return true;

				default:
					railPiece = default;
					return false;
			}
		}

		public static bool TryGetSinglePiece( this RailTile tile, out RailPiece railPiece )
		{
			if ( tile == 0 || (tile & (tile - 1)) != 0 )
			{
				railPiece = default;
				return false;
			}

			railPiece = (RailPiece)Utils.Log2( (uint)tile );
			return true;
		}

		public static HexEdge GetFirstEdge( this RailPiece piece )
		{
			for ( var i = 0; i < 6; ++i )
			{
				if ( piece.Touches( (HexEdge)i ) ) return (HexEdge)i;
			}

			throw new Exception( $"The given {nameof(RailPiece)} touches no edge." );
		}

		private static readonly List<RailPiece> PossiblePieces = new List<RailPiece>();

		public static RailPiece GetRandomPiece( this RailTile tile, Random random )
		{
			PossiblePieces.Clear();

			for ( var i = 0; i < 9 + 6; ++i )
			{
				var piece = (RailPiece)i;

				if ( (tile & piece.ToTile()) != 0 ) PossiblePieces.Add( piece );
			}

			if ( PossiblePieces.Count == 0 )
			{
				throw new ArgumentException( "The given tile must contain at least one piece.", nameof(tile) );
			}

			return PossiblePieces[random.Next( 0, PossiblePieces.Count )];
		}

		public static RailPiece GetBufferRailPiece( this HexEdge edge )
		{
			switch ( edge )
			{
				case HexEdge.Top:
					return RailPiece.TopBuffer;

				case HexEdge.TopRight:
					return RailPiece.TopRightBuffer;

				case HexEdge.BottomRight:
					return RailPiece.BottomRightBuffer;

				case HexEdge.Bottom:
					return RailPiece.BottomBuffer;

				case HexEdge.BottomLeft:
					return RailPiece.BottomLeftBuffer;

				case HexEdge.TopLeft:
					return RailPiece.TopLeftBuffer;

				default:
					throw new ArgumentOutOfRangeException( nameof(edge) );
			}
		}
	}
}
