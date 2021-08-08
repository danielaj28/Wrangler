using System;

namespace Wrangler
{
	public class Device
	{
		public string name { get; set; }
		public string driveLetter { get; set; }

		public decimal used { get; set; }
		public decimal total { get; set; }

		public string displayLabel
		{
			get
			{
				return $"{name} {driveLetter} {Math.Round(used / 1024, 2)} GB used of {Math.Round(total / 1024, 2)} GB";
			}
		}
	}
}