
namespace Test {
	public struct Struct {
		public int a;

		public Struct (int val) {
			a = val;
		}

		public static int Main () {
			object o = new Struct (1);
			Struct s = new Struct (2);

			if (s.a != 2)
				return 1;
			if (((Struct)o).a != 1)
				return 2;
			return 0;
		}
	}
}
