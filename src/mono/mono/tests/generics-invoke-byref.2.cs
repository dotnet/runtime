using System;
using System.Collections.Generic;

namespace TestConsole
{
    class Program
    {
        static int Main(string[] args)
        {
			List<string> str = null;
	  
			object[] methodArgs = new object[] { str };
	  
			Program p = new Program();
			p.GetType().GetMethod("TestMethod").Invoke(p, methodArgs);

			/* Byref nullable tests */
			object[] a = new object [1];
			int? i = 5;
			object o = i;
			a [0] = o;
			typeof (Program).GetMethod ("TestMethodNullable").Invoke (p, a);
			if ((int)a [0] != 6)
				return 1;
			if ((int)o != 5)
				return 2;

			a [0] = null;
			typeof (Program).GetMethod ("TestMethodNullable").Invoke (p, a);
			if ((int)a [0] != 0)
				return 3;

			return 0;
        }
      
		public Program()
		{
		}

	public void TestMethod(ref List<string> strArg)
	{
	  strArg = new List<string>();
	}

		public void TestMethodNullable (ref int? x) {
			if (x != null)
				x ++;
			else
				x = 0;
		}
    }
}
