using System;
using System.Runtime.InteropServices;
using System.Security;

public class MarshalTest
{
    [SecuritySafeCritical]
	public unsafe static int Main()
	{
		int[] i = new int[2] {100, 99};
		int   j = 0;

		TestLibrary.TestFramework.BeginTestCase("Marshal.ReadInt32()");

		TestLibrary.TestFramework.BeginScenario("Marshal.ReadInt32(int[]&)");
		fixed (int* ip = &i[0])
		{
			j = Marshal.ReadInt32( new IntPtr(ip) );

			Console.WriteLine("j = {0} i = {1}", j, i[0]);
		}

		TestLibrary.TestFramework.EndTestCase();

		if (j == i[0])
		{
			TestLibrary.TestFramework.LogInformation("PASS");
			return 100;
		}
		else
		{
			TestLibrary.TestFramework.LogInformation("FAIL");
			return 0;
		}
	}
}
