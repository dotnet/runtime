using System;
using System.Runtime.InteropServices;

public class DumpTest
{
	/* this should call HexDumpA with ANSI encoded string */
	[DllImport("libtest", CharSet=CharSet.Ansi)]
	private static extern int HexDump (string data);

	/* this should call HexDump default version with Unicode string */
	[DllImport("libtest", EntryPoint="HexDump", CharSet=CharSet.Unicode)]
	private static extern int HexDump2(string data);

	/* this should call HexDump1W with unicode encoding */
	[DllImport("libtest", CharSet=CharSet.Unicode)]
	private static extern int HexDump1(string data);

	public static int Main()
	{
		int res;
		
		res = HexDump ("First test");
		Console.WriteLine (res);
		if (res != 100769)
			return 1;

		res = HexDump2 ("First test");
		Console.WriteLine (res);
		if (res != 404)
			return 2;

		res = HexDump1 ("First test");
		Console.WriteLine (res);
		if (res != 1000404)
			return 3;

		return 0;		
	}
}

