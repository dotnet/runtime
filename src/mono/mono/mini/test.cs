using System;

namespace SSA {
	class Test {

		static void empty () {
		}
		static int ret_int () {
			return 1;
		}
		static int simple_add (int a) {
			int b = 5;
			return a + b;
		}

		static int cmov (int a) {
			return a >= 10? 1: 2;
		}

		static int many_shifts (int a, int b, int c) {
			return a << b << c << 1;
		}

		static void test2 (int a) {
			int x, y, z;
			
			z = 1;
			if (z > a) {
				x = 1;
				if (z > 2) {
					y = x + 1;
					return;
				}
			} else {
				x = 2;
			}
			z = x -3;
			x = 4;
			goto next;
			next:
			z = x + 7;
		}

		static int rfib (int n) {
			if (n < 2)
				return 1;
			return rfib (n - 2) + rfib (n - 1);
		}

		static int test1 (int v) {
			int x, y;

			x = 1;
			if (v != 0) {
				y = 2;
			} else {
				y = x + 1;
			}
			return y;
		}

		static int for_loop () {
			int j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			return j;
		}

		static int many_bb2 () {
			int j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			return j;
		}

		static int many_bb4 () {
			int j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			return j;
		}

		static int many_bb8 () {
			int j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			return j;
		}

		static int many_bb16 () {
			int j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			j = 0;
			for (int i = 0; i < 5; i++) {
				j += i;
			}
			return j;
		}

		static int many_bb32 () {
			int j;
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }

			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }

			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }

			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }

			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }

			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			j = 0; for (int i = 0; i < 5; i++) { j += i; }
			return j;
		}

		/*static int fib (int n) {
			int f0 = 0, f1 = 1, f2 = 0, i;

			if (n <= 1) goto L3;
			i = 2;
			L1:
			if (i <= n) goto L2;
			return f2;
			L2:
			f2 = f0 + f1;
			f0 = f1;
			f1 = f2;
			i++;
			goto L1;
			L3:
			return n;
		}*/

		static int nested_loops (int n) {
			int m = 1000;
			int a = 0;
			for (int i = 0; i < n; ++i) {
				for (int j = 0; j < m; ++j) {
					a++;
				}
			}
			return a;
		}

#if __MOBILE__
		public static test_2_old_test_suite () {
			return test1 (1);
		}
#else
		static int Main() {
			if (test1 (1) != 2)
				return 1;
			return 0;
		}
#endif
	}
}

