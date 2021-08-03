using System;
using System.Collections.Generic;

namespace Wrangler
{
	public class Preset
	{
		public string name { get; set; }
		public List<String> paths = new List<String>();
		public int cardCounter = 0;
	}
}