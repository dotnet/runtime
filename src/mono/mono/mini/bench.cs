using System;
using System.Reflection;

/*
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 * Regression tests for the mono JIT.
 *
 * Each test needs to be of the form:
 *
 * static int test_<result>_<name> ();
 *
 * where <result> is an integer (the value that needs to be returned by
 * the method to make it pass.
 * <name> is a user-displayed name used to identify the test.
 *
 * The tests can be driven in two ways:
 * *) running the program directly: Main() uses reflection to find and invoke
 * 	the test methods (this is useful mostly to check that the tests are correct)
 * *) with the --regression switch of the jit (this is the preferred way since
 * 	all the tests will be run with optimizations on and off)
 *
 * The reflection logic could be moved to a .dll since we need at least another
 * regression test file written in IL code to have better control on how
 * the IL code looks.
 */

class Tests {

	static int Main (string[] args) {
		return TestDriver.RunTests (typeof (Tests), args);
	}
	
	static public int test_0_many_nested_loops () {
		// we do the loop a few times otherwise it's too fast
		for (int i = 0; i < 5; ++i) {
		int n = 16;
		int x = 0;
		int a = n;
		while (a-- != 0) {
		    int b = n;
		    while (b-- != 0) {
			int c = n;
			while (c-- != 0) {
			    int d = n;
	    		while (d-- != 0) {
				int e = n;
				while (e-- != 0) {
				    int f = n;
				    while (f-- != 0) {
					x++;
				    }
				}
	    		}
			}
		    }
		}
		if (x != 16777216)
			return 1;
		}
		return 0;
	}

	public static int test_0_logic_run ()
	{
		// GPL: Copyright (C) 2001  Southern Storm Software, Pty Ltd.
		int iter, i = 0;

		while (i++ < 10) {
		// Initialize.
		bool flag1 = true;
		bool flag2 = true;
		bool flag3 = true;
		bool flag4 = true;
		bool flag5 = true;
		bool flag6 = true;
		bool flag7 = true;
		bool flag8 = true;
		bool flag9 = true;
		bool flag10 = true;
		bool flag11 = true;
		bool flag12 = true;
		bool flag13 = true;

		// First set of tests.
		for(iter = 0; iter < 2000000; ++iter) {
			if((flag1 || flag2) && (flag3 || flag4) &&
			   (flag5 || flag6 || flag7))
				{
				flag8 = !flag8;
				flag9 = !flag9;
				flag10 = !flag10;
				flag11 = !flag11;
				flag12 = !flag12;
				flag13 = !flag13;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
				flag1 = !flag1;
				flag2 = !flag2;
				flag3 = !flag3;
				flag4 = !flag4;
				flag5 = !flag5;
				flag6 = !flag6;
			}
		}
		}
		return 0;
	}
	static public int test_1028_sieve () {
	    //int NUM = ((argc == 2) ? atoi(argv[1]) : 1);
	    int NUM = 2000;
	    byte[] flags = new byte[8192 + 1];
	    int i, k;
	    int count = 0;

	    while (NUM-- != 0) {
		count = 0; 
		for (i=2; i <= 8192; i++) {
		    flags[i] = 1;
		}
		for (i=2; i <= 8192; i++) {
		    if (flags[i] != 0) {
			// remove all multiples of prime: i
			for (k=i+i; k <= 8192; k+=i) {
			    flags[k] = 0;
			}
			count++;
		    }
		}
	    }
	    //printf("Count: %d\n", count);
	    return(count);
	}
	
	public static int fib (int n) {
		if (n < 2)
			return 1;
		return fib(n-2)+fib(n-1);
	}

	public static int test_3524578_fib () {
		for (int i = 0; i < 10; i++)
			fib (32);
		
		return fib (32);
	}

        private static ulong numMoves;

        static void movetower (int disc, int from, int to, int use) {
		if (disc > 0) { 	
			numMoves++;
			movetower (disc-1, from, use, to);
			movetower (disc-1, use, to, from);
		}
        }

        public static int test_0_hanoi () {
		int iterations = 5000;
		int numdiscs = 12;
		
		numMoves = 0;
		while (iterations > 0) {
			iterations--;
			movetower (numdiscs, 1, 3, 2);
		}
		if (numMoves != 20475000)
			return 1;
		return 0;
        }

	public static int test_0_castclass () {
		object a = "a";

		for (int i = 0; i < 100000000; i++) {
			string b = (string)a;
			if ((object)a != (object)b)
				return 1;
		}
		return 0;
	}
	
	public static int test_23005000_float () {
		double a, b, c, d;
		bool val;
		int loops = 0;
		a = 0.0;
		b = 0.0001;
		c = 2300.5;
		d = 1000.0;

		while (a < c) {
			if (a == d)
				b *= 2;
			a += b;
			val = b >= c;
			if (val) break;
			loops++;
		}
		return loops;
	}

	/*
        /// Gaussian blur of a generated grayscale picture
        private int test_0_blur(int size) {
		const int num  = 5; // Number of time to blur
		byte[,] arr1 = new byte[size, size];
		byte[,] arr2 = new byte[size, size];

		int iterations = 1;
		
		while(iterations-- > 0) {

			// Draw fake picture
			for(int i = 0; i < size; i++) {
				for(int j = 0; j < size; j++) {   
					arr1[i, j] = (byte) (i%255); 
				}
			}

			for(int n = 0; n < num; n++) { // num rounds of blurring
				for(int i = 3; i < size-3; i++) // vertical blur arr1 -> arr2
					for(int j = 0; j < size; j++)
						arr2[i, j] = (byte)((arr1[i-3, j] + arr1[i+3, j]
								     + 6*(arr1[i-2, j]+arr1[i+2, j])
								     + 15*(arr1[i-1, j]+arr1[i+1, j])
								     + 20*arr1[i, j] + 32)>>6);

				for(int j = 3; j < size-3; j++) // horizontal blur arr1 -> arr2
					for(int i = 0; i < size; i++)
						arr1[i, j] = (byte)((arr2[i, j-3] + arr2[i, j+3]
								     + 6*(arr2[i, j-2]+arr2[i, j+2])
								     + 15*(arr2[i, j-1]+arr2[i, j+1])
								     + 20*arr2[i, j] + 32)>>6);
			}
		}

		return 0;
        }
 	*/
}

