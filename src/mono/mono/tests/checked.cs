using System;

namespace test {
	public class tester {
		public tester() {}

		public static int Main () {
			float a  = 1e20f;
			int i = 0;
			uint ui = 0;

			bool exception = false;

			try {
				int b = (int) a;
				checked {
					i = (int)a;
				}
			}
			catch (OverflowException) {
				exception = true;
			}
			catch (Exception) {
			}			


			if (!exception)
				return 1;

			exception = false;

			a  = 1e5f;
			
			try {
				int b = (int) a;
				checked {
					i = (int)a;
				}
			} catch (Exception) {
				return 2;
			}


			if (i != 100000)
				return 3;
		
			exception = false;

			a  = -1e30f;
			try {
				int b = (int) a;
				checked {
					i = (int)a;
				}
			} 
			catch (OverflowException) {
				exception = true;
			}
			catch (Exception) {
			}			
			exception = false;


			a  = -1e30f;
			try {
				uint b = (uint) a;
				checked {
					ui = (uint)a;
				}

				Console.WriteLine("No Exception");
			} 
			catch (OverflowException) {
				exception = true;
			}
			catch (Exception) {
			}


			if (!exception)
				return 4;

			a  = 1e5f;
			try {
				uint b = (uint) a;
				checked {
					ui = (uint)a;
				}
			} 
			catch (Exception) {
				return 5;
			}

			if (ui != 100000)
				return 6;

			// Check mul.ovf
			checked {
				int l;
				int m;

				int[][] cases = new int [][] { 
					new int [] {0, 0, 0},
					new int [] {-5, 0, 0},
					new int [] {3, -5, -15},
					new int [] {3, 5, 15},
					new int [] {-3, -5, 15},
					new int [] {-3, 5, -15},
					new int [] {-1, 32767, -32767},
					new int [] {32767, -1, -32767}};


				for (int j = 0; j < cases.Length; ++j)
					if (cases [j][0] * cases [j][1] != cases [j][2])
						return 7 + j;
			}

			checked {
				int j;
				int k;

				j = k = 0;
				if (j * k != 0)
					return 20;

				j = -5;
				k = 0;
				if (j * k != 0)
					return 21;

				j = 0;
				k = -5;
				if (j * k != 0)
					return 22;

				j = 3;
				k = -5;
				if (j * k != -15)
					return 23;

				j = 3;
				k = 5;
				if (j * k != 15)
					return 24;

				j = -3;
				k = -5;
				if (j * k != 15)
					return 25;

				j = -3;
				k = 5;
				if (j * k != -15)
					return 26;

				j = -1;
				k = 32767;
				if (j * k != -32767)
					return 27;
				
				j = 32767;
				k = -1;
				if (j * k != -32767)
					return 28;
			}

			checked {
				long l;
				long m;

				long[][] cases = new long [][] { 
					new long [] {0, 0, 0},
					new long [] {-5, 0, 0},
					new long [] {3, -5, -15},
					new long [] {3, 5, 15},
					new long [] {-3, -5, 15},
					new long [] {-3, 5, -15},
					new long [] {-1, 2147483647, -2147483647},
					new long [] {2147483647, -1, -2147483647}};

				for (int j = 0; j < cases.Length; ++j)
					if (cases [j][0] * cases [j][1] != cases [j][2])
						return 29 + j;
			}
				
			Console.WriteLine("test-ok");

			return 0;
		}
	}
}
