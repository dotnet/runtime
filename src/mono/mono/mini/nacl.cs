using System;
using Mono.Simd;

class Tests {
	struct myvt {
	  public int X;
	  public int Y;
	}

	static int test_0_vector4i_cmp_gt () {
 	        Vector4i a = new Vector4i (10, 5, 12, -1);
		Vector4i b = new Vector4i (-1, 5, 10, 10);

		Vector4i c = a.CompareGreaterThan (b);
	
		if (c.X != -1)
			return 1;
		if (c.Y != 0)
			return 2;
		if (c.Z != -1)
		  return 3;
		if (c.W != 0)
		  return 4;
		return 0;
	}

	static myvt CompareGT(myvt a, myvt b) {
	  myvt r;
	  r.X = a.X > b.X ? -1 : 0;
	  r.Y = a.Y > b.Y ? -1 : 0;
	  return r;
	}

	static int test_0_struct2i_cmp_gt() {
	  myvt a;
	  myvt b;
	  a.X = 10;
	  a.Y = 5;
	  b.X = -1;
	  b.Y = 5;
	  myvt c = CompareGT(a, b);
	  if (c.X != -1)
	    return 1;
	  if (c.Y != 0)
	    return 2;
	  return 0;
	}

	static int vararg_sum(params int[] args) {
	  int sum = 0;
	  foreach(int arg in args) {
	    sum += arg;
	  }
	  return sum;
	}
	static int test_21_vararg_test() {
	  int sum = 0;
	  sum += vararg_sum();
	  sum += vararg_sum(1);
	  sum += vararg_sum(2, 3);
	  sum += vararg_sum(4, 5, 6);
	  return sum;
	}
	public static int Main(String[] args) {
	  return TestDriver.RunTests(typeof(Tests));
	}
}
