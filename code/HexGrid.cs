using System;
using System.Collections.Generic;
using Sandbox;

namespace Ziks.Trains
{
	public enum HexEdge : byte
	{
		Top,
		TopRight,
		BottomRight,
		Bottom,
		BottomLeft,
		TopLeft
	}

	public readonly struct HexCoordEdge : IEquatable<HexCoordEdge>
	{
		public static bool operator ==( HexCoordEdge a, HexCoordEdge b )
		{
			return a.Coord == b.Coord && a.Edge == b.Edge;
		}

		public static bool operator !=( HexCoordEdge a, HexCoordEdge b )
		{
			return a.Coord != b.Coord || a.Edge != b.Edge;
		}

		public HexCoord Coord { get; init; }
		public HexEdge Edge { get; init; }

		public HexCoordEdge( int x, int y, HexEdge hexEdge )
		{
			Coord = new HexCoord( x, y );
			Edge = hexEdge;
		}

		public HexCoordEdge( HexCoord hexCoord, HexEdge hexEdge )
		{
			Coord = hexCoord;
			Edge = hexEdge;
		}

		public HexCoordEdge Opposite => new( Coord + Edge, Edge.Opposite() );

		public bool Equals(HexCoordEdge other)
		{
			return Coord.Equals(other.Coord) && Edge == other.Edge;
		}

		public override bool Equals(object obj)
		{
			return obj is HexCoordEdge other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Coord, (int)Edge);
		}
	}

	public static class HexEdgeExtensions
	{
		public static HexEdge Opposite( this HexEdge edge )
		{
			return (HexEdge)(((int)edge + 3) % 6);
		}
	}

	public readonly struct HexCoord : IEquatable<HexCoord>
	{
		public static HexCoord Zero => new HexCoord();

		public static HexCoord operator +( HexCoord a, HexCoord b )
		{
			return new(a.X + b.X, a.Y + b.Y);
		}

		public static HexCoord operator -( HexCoord a, HexCoord b )
		{
			return new(a.X - b.X, a.Y - b.Y);
		}

		public static HexCoord operator *( HexCoord a, int scale )
		{
			return new( a.X * scale, a.Y * scale );
		}

		public static bool operator ==( HexCoord a, HexCoord b )
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public static bool operator !=( HexCoord a, HexCoord b )
		{
			return a.X != b.X || a.Y != b.Y;
		}

		public static implicit operator HexCoord( HexEdge edge )
		{
			switch ( edge )
			{
				case HexEdge.Top:
					return new HexCoord( 0, 1 );
				case HexEdge.TopRight:
					return new HexCoord( 1, 0 );
				case HexEdge.BottomRight:
					return new HexCoord( 1, -1 );
				case HexEdge.Bottom:
					return new HexCoord( 0, -1 );
				case HexEdge.BottomLeft:
					return new HexCoord( -1, 0 );
				case HexEdge.TopLeft:
					return new HexCoord( -1, 1 );
				default:
					throw new ArgumentOutOfRangeException( nameof( edge ) );
			}
		}

		public int X { get; init; }
		public int Y { get; init; }
		public int Z => X - Y;

		public int Length => Math.Min(
			Math.Abs( X ) + Math.Abs( Y ),
			Math.Min(
				Math.Abs( X ) + Math.Abs( Z ),
				Math.Abs( Y ) + Math.Abs( Z ) ) );

		public HexCoord( int x, int y )
		{
			X = x;
			Y = y;
		}

		public HexCoord Neighbor( HexEdge edge )
		{
			return this + edge;
		}

		public override string ToString()
		{
			return $"({X}, {Y}, {Z})";
		}

		public bool Equals(HexCoord other)
		{
			return X == other.X && Y == other.Y;
		}

		public override bool Equals(object obj)
		{
			return obj is HexCoord other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(X, Y);
		}
	}

	public readonly struct HexCircle : IEquatable<HexCircle>
	{
		public static implicit operator HexCircle( HexCoord hexCoord )
		{
			return new HexCircle( hexCoord, 0 );
		}

		public static HexCircle operator +( HexCircle ring, HexCoord coord )
		{
			return new HexCircle( ring.HexCoord + coord, ring.Radius );
		}

		public static HexCircle operator -( HexCircle ring, HexCoord coord )
		{
			return new HexCircle( ring.HexCoord - coord, ring.Radius );
		}

		public static bool operator ==( HexCircle a, HexCircle b )
		{
			return a.HexCoord == b.HexCoord && a.Radius == b.Radius;
		}

		public static bool operator !=( HexCircle a, HexCircle b )
		{
			return a.HexCoord != b.HexCoord || a.Radius != b.Radius;
		}

		public HexCoord HexCoord { get; init; }
		public int Radius { get; init; }

		public HexCircle( HexCoord hexCoord, int radius )
		{
			HexCoord = hexCoord;
			Radius = radius;
		}

		public int DistanceTo( HexCoord coord )
		{
			return (HexCoord - coord).Length;
		}

		public bool Contains( HexCircle other )
		{
			return DistanceTo( other.HexCoord ) <= Radius - other.Radius;
		}

		public bool Intersects( HexCircle other )
		{
			return DistanceTo( other.HexCoord ) <= Radius + other.Radius;
		}

		public HexCircle Expand( int amount )
		{
			return new HexCircle( HexCoord, Radius + amount );
		}

		public HexCircle ExpandToContain( HexCircle other )
		{
			var expandAmount = DistanceTo( other.HexCoord ) + other.Radius - Radius;
			return expandAmount > 0 ? Expand( expandAmount ) : this;
		}

		public int EdgeCoordinateCount => GetEdgeCoordinateCount( Radius );
		public int CoordinateCount => GetCoordinateCount( Radius );

		public static int GetEdgeCoordinateCount( int radius )
		{
			return Math.Max( 1, radius * 6 );
		}

		public static int GetCoordinateCount( int radius )
		{
			return 3 * radius * (radius + 1) + 1;
		}

		public HexCoord GetEdgeCoordinate( int index )
		{
			if ( Radius == 0 ) return HexCoord;

			var edgeA = (HexEdge) ((index / Radius) % 6);
			var edgeB = (HexEdge) (((int) edgeA + 2) % 6);

			return HexCoord + (HexCoord)edgeA * Radius + (HexCoord)edgeB * (index % Radius);
		}

		public HexCoord GetCoordinate( int index )
		{
			if ( index == 0 ) return HexCoord;

			var radius = (int) ((-3f + MathF.Sqrt( -3 + 12 * index )) / 6f) + 1;

			return new HexCircle( HexCoord, radius ).GetEdgeCoordinate( index - GetCoordinateCount( radius - 1 ) );
		}

		public bool Equals(HexCircle other)
		{
			return HexCoord == other.HexCoord && Radius == other.Radius;
		}

		public override bool Equals(object obj)
		{
			return obj is HexCircle other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(HexCoord, Radius);
		}
	}

	public partial class HexGrid : Entity
	{
		public const float PiOver6 = MathF.PI / 6f;
		public const float HalfRoot3 = 0.8660254037844386467637231707529f;
		public const float UnitScale = 128f / 3f;

		private static readonly Vector3 Axis0 = new Vector3( MathF.Cos( PiOver6 ), MathF.Sin( PiOver6 ) ) * UnitScale;
		private static readonly Vector3 Axis1 = new Vector3( 0f, 1f ) * UnitScale;

		private static readonly Vector3[] Directions;

		static HexGrid()
		{
			Directions = new Vector3[6];

			for ( var i = 0; i < 6; ++i )
			{
				var edge = (HexEdge)i;
				var dir = (HexCoord) edge;

				Directions[i] = (dir.X * Axis0 + dir.Y * Axis1).Normal;
			}
		}

		public HexGrid()
		{
			EnableDrawing = false;
			Transmit = TransmitType.Always;
		}

		public HexCoord GetHexCoord( Vector3 worldPos )
		{
			Vector2 localPos = GetLocalPosition( worldPos );

			var col = (int)MathF.Floor( localPos.x / Axis0.x );
			var row = (int)MathF.Floor( localPos.y / Axis1.y - 0.5f * (localPos.x / Axis0.x) );

			// There's probably a smarter way to do this...
			// Let's find the 4 closest cells, and compare which is closest

			Vector2 a = GetLocalPosition( new HexCoord( col, row ) );
			Vector2 b = GetLocalPosition( new HexCoord( col + 1, row ) );
			Vector2 c = GetLocalPosition( new HexCoord( col, row + 1 ) );
			Vector2 d = GetLocalPosition( new HexCoord( col + 1, row + 1 ) );

			var distA = (localPos - a).LengthSquared;
			var distB = (localPos - b).LengthSquared;
			var distC = (localPos - c).LengthSquared;
			var distD = (localPos - d).LengthSquared;

			float leftDist, rightDist;
			int leftRow, rightRow;

			if ( distA < distC )
			{
				leftDist = distA;
				leftRow = row;
			}
			else
			{
				leftDist = distC;
				leftRow = row + 1;
			}

			if ( distB < distD )
			{
				rightDist = distB;
				rightRow = row;
			}
			else
			{
				rightDist = distD;
				rightRow = row + 1;
			}

			return leftDist < rightDist ? new HexCoord( col, leftRow ) : new HexCoord( col + 1, rightRow );
		}

		public HexCoordEdge GetHexCoordEdge( Vector3 worldPos )
		{
			var hexCoord = GetHexCoord( worldPos );
			var localPos = GetLocalPosition( worldPos );
			var centerPos = GetLocalPosition( hexCoord );
			var diff = centerPos - localPos;

			var angle = -MathF.Atan2( diff.y, diff.x );
			var rawIndex = (int)MathF.Round( angle * 3f / MathF.PI + 4.5f );

			return new(hexCoord, (HexEdge)(rawIndex % 6));
		}

		public Vector3? Trace( Ray ray, double maxDistance = double.PositiveInfinity )
		{
			var plane = new Plane( Position, Rotation * Vector3.Up );
			return plane.Trace( ray, true, maxDistance );
		}

		public Vector3 GetWorldPosition( Vector3 localPos )
		{
			return Position + Rotation * localPos * Scale;
		}

		public Vector3 GetWorldPosition( HexCoord gridPos )
		{
			return GetWorldPosition( GetLocalPosition( gridPos ) );
		}

		public Vector3 GetWorldPosition( HexCoord gridPos, HexEdge edge )
		{
			return GetWorldPosition( GetLocalPosition( gridPos, edge ) );
		}

		public Vector3 GetWorldPosition( HexCoordEdge coordEdge )
		{
			return GetWorldPosition( GetLocalPosition( coordEdge ) );
		}

		public Vector3 GetWorldVector( Vector3 localVector )
		{
			return Rotation * localVector * Scale;
		}

		public Vector3 GetWorldDirection( HexEdge edge )
		{
			return Rotation * GetLocalDirection( edge );
		}

		public static Vector3 GetLocalPosition( HexCoord gridPos )
		{
			return gridPos.X * Axis0 + gridPos.Y * Axis1;
		}
		
		public static Vector3 GetLocalPosition( HexCoord gridPos, HexEdge edge )
		{
			var prevPos = gridPos;
			var nextPos = gridPos + edge;

			var a = prevPos.X * Axis0 + prevPos.Y * Axis1;
			var b = nextPos.X * Axis0 + nextPos.Y * Axis1;

			return a * 0.5f + b * 0.5f;
		}

		public static Vector3 GetLocalPosition( HexCoordEdge coordEdge )
		{
			return GetLocalPosition( coordEdge.Coord, coordEdge.Edge );
		}

		public Vector3 GetLocalPosition( Vector3 worldPos )
		{
			return Rotation.Inverse * (worldPos - Position) / Scale;
		}

		public Vector3 GetLocalDirection( HexEdge edge )
		{
			return Directions[(int)edge];
		}
		
		public bool GetWorldCurve( HexCoord gridPos, HexEdge from, HexEdge to, out float radius, out Vector3 center )
		{
			if ( !GetLocalCurve( gridPos, from, to, out radius, out center ) ) return false;

			radius = Scale * radius;
			center = GetWorldPosition( center );
			return true;
		}

		public bool GetLocalCurve( HexCoord gridPos, HexEdge from, HexEdge to, out float radius, out Vector3 center )
		{
			radius = default;
			center = default;

			if ( from == to || from.Opposite() == to )
			{
				return false;
			}

			var fromPos = GetLocalPosition( gridPos, from );
			var fromDir = GetLocalDirection( from.Opposite() );
			var fromNormal = new Vector3( fromDir.y, -fromDir.x );

			if ( from == (HexEdge)(((int)to + 2) % 6) )
			{
				radius = -UnitScale * HalfRoot3;
				center = fromPos - radius * fromNormal;
				return true;
			}

			if ( from == (HexEdge)(((int)to + 4) % 6) )
			{
				radius = UnitScale * HalfRoot3;
				center = fromPos - radius * fromNormal;
				return true;
			}

			return false;
		}
		
		private static readonly ThirdParty.PriorityQueue<float, HexCoordEdge> AStarOpenQueue = new();
		private static readonly HashSet<HexCoordEdge> AStarOpenSet = new();
		private static readonly Dictionary<HexCoordEdge, (HexCoordEdge From, float G, float F)> AStarCameFrom = new();

		private const float StraightCost = UnitScale;
		private const float CurvedCost = StraightCost * MathF.PI / 3f;

		private static float H( HexCoordEdge node, Vector2 dest )
		{
			var localPos = (Vector2) GetLocalPosition( node );

			return (dest - localPos).Length;
		}

		private static void ReconstructPath( List<HexCoordEdge> outPath,
			Dictionary<HexCoordEdge, (HexCoordEdge From, float G, float F)> cameFrom,
			HexCoordEdge current )
		{
			var startIndex = outPath.Count;

			while (true)
			{
				outPath.Add( current );
				
				if ( !cameFrom.TryGetValue( current, out var pair ) ) break;
				if ( pair.From == current ) break;

				current = pair.From;
			}

			outPath.Reverse( startIndex, outPath.Count - startIndex );
		}

		private static readonly int[] EdgeOffsets = { 0, 1, 5 };

		public static bool GetShortestPath( List<HexCoordEdge> outPath, HexCoordEdge start, HexCoordEdge end, bool allowSharpTurns )
		{
			if ( allowSharpTurns )
			{
				throw new NotImplementedException();
			}

			if ( start == end ) return false;

			var facilityManager = Game.Current.FacilityManager;
			var facility = facilityManager.GetFacilityFromFootprint( start.Coord )
               ?? facilityManager.GetFacilityFromFootprint( end.Coord )
               ?? facilityManager.GetFacilityFromFootprint( start.Coord + start.Edge )
               ?? facilityManager.GetFacilityFromFootprint( end.Coord + end.Edge );

			if ( facility != null ) return false;

			start = start.Opposite;

			var openQueue = AStarOpenQueue;
			var openSet = AStarOpenSet;
			var cameFrom = AStarCameFrom;

			openQueue.Clear();
			openSet.Clear();
			cameFrom.Clear();

			try
			{
				var endLocalPos = (Vector2)GetLocalPosition( end );

				openQueue.Enqueue( H( start, endLocalPos ), start );
				openSet.Add( start );
				cameFrom.Add( start, (start, 0, -1) );

				while ( !openQueue.IsEmpty )
				{
					var node = openQueue.DequeueValue();
					openSet.Remove( node );

					if ( node == end )
					{
						ReconstructPath( outPath, cameFrom, node );
						return true;
					}

					var nodeCost = cameFrom[node].G;
					var nextCoord = node.Coord + node.Edge;

					for ( var i = 2; i >= 0; --i )
					{
						var nextEdge = (HexEdge)(((int)node.Edge + EdgeOffsets[i]) % 6);

						var next = new HexCoordEdge( nextCoord, nextEdge );

						if ( facilityManager.GetFacilityFromFootprint( next.Coord + next.Edge ) != null )
						{
							continue;
						}

						var newCost = nodeCost + (i == 0 ? StraightCost : CurvedCost);
						var existing = cameFrom.TryGetValue( next, out var oldCosts );

						if ( existing && newCost >= oldCosts.G ) continue;

						var newCosts = (From: node, G: newCost, F: newCost + H( next, endLocalPos ));

						if ( existing )
						{
							cameFrom[next] = newCosts;
						}
						else
						{
							cameFrom.Add( next, newCosts );
						}

						if ( !openSet.Add( next ) )
						{
							openQueue.Remove( new(oldCosts.F, next) );
						}

						openQueue.Enqueue( newCosts.F, next );
					}
				}

				return false;
			}
			finally
			{
				openQueue.Clear();
				openSet.Clear();
				cameFrom.Clear();
			}
		}
	}
}
