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
				return -1;

			exception = false;

			a  = 1e5f;
			
			try {
				int b = (int) a;
				checked {
					i = (int)a;
				}
			} catch (Exception) {
				return -2;
			}


			if (i != 100000)
				return -3;
		
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
				return -4;

			a  = 1e5f;
			try {
				uint b = (uint) a;
				checked {
					ui = (uint)a;
				}
			} 
			catch (Exception) {
				return -5;
			}

			if (ui != 100000)
				return -6;
			
			
			Console.WriteLine("test-ok");

			return 0;
		}
	}
}
