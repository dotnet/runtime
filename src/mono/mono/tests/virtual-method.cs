using System;

namespace Obj {
	interface Bah {
		int H ();
	}
	class A : Bah {
		public int F () {return 1;}
		public virtual int G () {return 2;}
		public int H () {return 10;}
	}
	class B : A {
		public new int F () {return 3;}
		public override int G () {return 4;}
		public new int H () {return 11;}
	}
	class Test {
		static public int Main () {
			int result = 0;
			B b = new B ();
			A a = b;
			if (a.F () != 1)
				result |= 1 << 0;
			if (b.F () != 3)
				result |= 1 << 1;
			if (b.G () != 4)
				result |= 1 << 2;
			if (a.G () != 4)
				result |= 1 << 3;
			if (a.H () != 10)
				result |= 1 << 4;
			if (b.H () != 11)
				result |= 1 << 5;
			if (((A)b).H () != 10)
				result |= 1 << 6;
			if (((B)a).H () != 11)
				result |= 1 << 7;
			return result;
		}
	};
};
