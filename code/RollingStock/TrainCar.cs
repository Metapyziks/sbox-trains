using Sandbox;

namespace Ziks.Trains.RollingStock
{
	public enum TrainCarModel
	{
		Engine,
		Hopper
	}

	public partial class TrainCar : ModelEntity
	{
		[Net]
		public float EngineForce { get; set; } = 0f;

		[Net]
		public float Mass { get; set; } = 1f;

		[Net]
		public float Length { get; set; } = 1f / 3f;

		[Net]
		public float DistanceFromHead { get; set; }

		public TrainCar()
		{
			Transmit = TransmitType.Pvs;
		}
	}
}
