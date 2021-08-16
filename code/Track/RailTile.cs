using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ziks.Trains.Track
{
	/// <summary>
	/// Enum for a tile, that is composed of a set of <see cref="RailPiece"/>s.
	/// </summary>
	[Flags]
	public enum RailTile : ushort
	{
		None = 0,

		BottomTop = 1 << RailPiece.BottomTop,
		TopBottom = BottomTop,

		BottomLeftTopRight = 1 << RailPiece.BottomLeftTopRight,
		TopRightBottomLeft = BottomLeftTopRight,

		BottomRightTopLeft = 1 << RailPiece.BottomRightTopLeft,
		TopLeftBottomRight = BottomRightTopLeft,

		BottomTopLeft = 1 << RailPiece.BottomTopLeft,
		TopLeftBottom = BottomTopLeft,

		TopBottomLeft = 1 << RailPiece.TopBottomLeft,
		BottomLeftTop = TopBottomLeft,

		TopLeftTopRight = 1 << RailPiece.TopLeftTopRight,
		TopRightTopLeft = TopLeftTopRight,

		TopBottomRight = 1 << RailPiece.TopBottomRight,
		BottomRightTop = TopBottomRight,

		BottomTopRight = 1 << RailPiece.BottomTopRight,
		TopRightBottom = BottomTopRight,

		BottomLeftBottomRight = 1 << RailPiece.BottomLeftBottomRight,
		BottomRightBottomLeft = BottomLeftBottomRight,

		TopBuffer = 1 << RailPiece.TopBuffer,
		TopRightBuffer = 1 << RailPiece.TopRightBuffer,
		BottomRightBuffer = 1 << RailPiece.BottomRightBuffer,
		BottomBuffer = 1 << RailPiece.BottomBuffer,
		BottomLeftBuffer = 1 << RailPiece.BottomLeftBuffer,
		TopLeftBuffer = 1 << RailPiece.TopLeftBuffer,

		/// <summary>
		/// All straight pieces.
		/// </summary>
		Straight = BottomTop | BottomLeftTopRight | BottomRightTopLeft,

		/// <summary>
		/// All curved pieces.
		/// </summary>
		Curved = BottomTopLeft | BottomTopRight | TopBottomLeft | TopBottomRight | TopLeftTopRight | BottomLeftBottomRight,

		/// <summary>
		/// All end pieces.
		/// </summary>
		Buffer = TopBuffer | TopRightBuffer | BottomRightBuffer | BottomBuffer | BottomLeftBuffer | TopLeftBuffer
	}
}
