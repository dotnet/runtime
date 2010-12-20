using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections;
using System.Text;
using System.Threading;

namespace Test {

	public class c_code {

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void my_c_func(int x, string s, double d);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void my_c_pass(int x);
	}

	public class HelloWorld
	{
		static public void Main ()
		{
		}

		static public void Foobar (int x, string s)
		{
			// first line is a simple test
			// 1. call back into c code 2. use mscorlib Math.Sqrt()
			c_code.my_c_func(x, s, Math.Sqrt(3.1415 * 3.1415));

			// second part of this test:
			// attempt a try/catch, generate exception w/ throw
			try {
				c_code.my_c_pass(0);
				// attempt an invalid cast
				throw new InvalidCastException();
				c_code.my_c_pass(1);
			}
			catch (InvalidCastException e) {
				c_code.my_c_pass(2);
			}
			c_code.my_c_pass(3);

			// third part of this test:
			// attempt an invalid cast again, this time generating
			// exception instead of using explicit throw.
			try {
				c_code.my_c_pass(0);
				StringBuilder reference1 = new StringBuilder();
				object reference2 = reference1;
				// attempt invalid cast
				int reference3 = (int)reference2;
				c_code.my_c_pass(4);
			}
			catch (InvalidCastException e) {
				c_code.my_c_pass(5);
			}
			c_code.my_c_pass(3);
		}
	} 
}
