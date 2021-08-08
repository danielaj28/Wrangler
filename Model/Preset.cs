using System;
using System.Collections.Generic;

namespace Wrangler
{
	public class Preset
	{
		public string name { get; set; }
		public List<String> paths { get; set; } = new List<String>();
		public int cardCounter { get; set; } = 0;
	}
}