using System;
using System.Runtime.InteropServices;

public class Test {

	[DllImport("cygwin1.dll", EntryPoint="puts", CharSet=CharSet.Ansi)]
	public static extern int puts (string name);

	public static int Main () {
		puts ("A simple Test for PInvoke");
		
		if (Math.Cos (Math.PI) != -1)
			return 1;
		if (Math.Acos (1) != 0)
			return 1;
		
		return 0;
	}
}


