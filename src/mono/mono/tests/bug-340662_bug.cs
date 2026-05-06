using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;


class Program
{
	[DllImport("foo.dll", CallingConvention=CallingConvention.Winapi)]
	public static extern void pf1(string format, __arglist);

	[DllImport("foo.dll", CallingConvention=CallingConvention.Cdecl)]
	public static extern void pf2(string format, __arglist);
 
	[DllImport("foo.dll", CallingConvention=CallingConvention.StdCall)]
	public static extern void pf3(string format, __arglist);

	[DllImport("foo.dll", CallingConvention=CallingConvention.ThisCall)]
	public static extern void pf4(string format, __arglist);

	[DllImport("foo.dll", CallingConvention=CallingConvention.FastCall)]
	public static extern void pf5(string format, __arglist);

	[DllImport("foo.dll", CallingConvention=CallingConvention.StdCall)]
	public static extern void mixed1(string format);

	static int Main()
	{
		for (int i = 1; i < 6; ++i) {
			if (typeof (Program).GetMethod ("pf"+i).CallingConvention != CallingConventions.VarArgs) {
				Console.WriteLine ("pf{0} {1} != VarArg", i, typeof (Program).GetMethod ("pf"+i).CallingConvention);
				return 1;
			}
		}

		if (typeof (Program).GetMethod ("mixed1").CallingConvention != CallingConventions.Standard) {
			Console.WriteLine ("mixed1 {0} != Standard", typeof (Program).GetMethod ("mixed1").CallingConvention);
			return 1;
		}

        Console.WriteLine ("OK");
        return 0;
    }
}
