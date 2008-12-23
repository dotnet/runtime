using System;
using System.Runtime.InteropServices;

namespace TestApp
{
	public delegate char MyDelegate(int x);

    class Driver
    {
		static char Test (int x) { return (char)x; }

		static int Main()
		{
			MyDelegate m = Driver.Test;
			Marshal.GetFunctionPointerForDelegate (m);
			return 0;
		}
	}
}
