namespace Wrangler
{
	public class Device
	{
		public string name { get; set; }
		public string driveLetter { get; set; }

		public long used { get; set; }
		public long total { get; set; }

		public string displayLabel
		{
			get
			{
				return $"{name} {driveLetter} {used}GB used of {total}GB";
			}
		}
	}
}