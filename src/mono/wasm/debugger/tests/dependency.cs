using System;

namespace Simple {
	public class Complex {
		public int A { get; set; }
		public string B { get; set; }
		object c;

		public Complex (int a, string b) {
			A = a;
			B = b;
			this.c = this;
		}

		public int DoStuff () {
			return DoOtherStuff ();
		}

		public int DoOtherStuff () {
			return DoEvenMoreStuff () - 1;
		}

		public int DoEvenMoreStuff () {
			return 1 + BreakOnThisMethod ();
		}

		public int BreakOnThisMethod () {
			var x = A + 10;
			c = $"{x}_{B}";

			return x;
		}
	}
}