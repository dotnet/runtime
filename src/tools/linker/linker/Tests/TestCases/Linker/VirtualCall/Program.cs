using System;

namespace TestCases.Linker.VirtualCall
{
	public class Program
	{
		[Mark]
		public static int Test ()
		{
			Program p = new Program ();
			return p.Run ();
		}

		int Run ()
		{
			Library lib = new PowerFulLibrary ();
			return lib.Shebang ();
		}
	}
}