using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ziks.Trains.Track
{
	/// <summary>
	/// Enum for a single piece of track.
	/// </summary>
	public enum RailPiece : byte
	{
		BottomTop = 0,
		TopBottom = BottomTop,

		BottomLeftTopRight = 1,
		TopRightBottomLeft = BottomLeftTopRight,

		BottomRightTopLeft = 2,
		TopLeftBottomRight = BottomRightTopLeft,

		BottomTopLeft = 3,
		TopLeftBottom = BottomTopLeft,

		TopBottomLeft = 4,
		BottomLeftTop = TopBottomLeft,

		TopLeftTopRight = 5,
		TopRightTopLeft = TopLeftTopRight,

		TopBottomRight = 6,
		BottomRightTop = TopBottomRight,

		BottomTopRight = 7,
		TopRightBottom = BottomTopRight,

		BottomLeftBottomRight = 8,
		BottomRightBottomLeft = BottomLeftBottomRight,

		TopBuffer = 9,
		TopRightBuffer = 10,
		BottomRightBuffer = 11,
		BottomBuffer = 12,
		BottomLeftBuffer = 13,
		TopLeftBuffer = 14,

		Platform = 15
	}
}
