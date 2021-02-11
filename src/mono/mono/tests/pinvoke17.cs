using System;
using System.Runtime.InteropServices;

[StructLayout (LayoutKind.Sequential)]   
public class OSVersionInfo 
{			
	public int a;
	public int b; 
}

[StructLayout (LayoutKind.Sequential)]  
public struct OSVersionInfo2 
{
	public int a;
	public int b; 
}


public class LibWrap 
{
	[DllImport ("libtest", EntryPoint="MyGetVersionEx")]				
	public static extern int GetVersionEx ([In, Out] OSVersionInfo osvi);

    [DllImport ("libtest")]
	public static extern int BugGetVersionEx (int a, int b, int c, int d, int e, int f, int g, int h, [In, Out] OSVersionInfo osvi);
    
	[DllImport ("libtest", EntryPoint="MyGetVersionEx")] 
	public static extern int GetVersionEx2 (ref OSVersionInfo2 osvi);  
}

public class Test
{
	public static int Main()
	{
		Console.WriteLine( "\nPassing OSVersionInfo as class" );

		OSVersionInfo osvi = new OSVersionInfo();
		osvi.a = 1;
		osvi.b = 2;
		
		if (LibWrap.GetVersionEx (osvi) != 5)
			return 1;

		if (osvi.a != 2)
			return 2;
		
		if (osvi.b != 3)
			return 3;
		
		Console.WriteLine( "A: {0}", osvi.a);
		Console.WriteLine( "B: {0}", osvi.b);
		
		Console.WriteLine( "\nPassing OSVersionInfo as struct" );
		
		OSVersionInfo2 osvi2 = new OSVersionInfo2();
		osvi2.a = 1;
		osvi2.b = 2;

		if (LibWrap.GetVersionEx2 (ref osvi2) != 5)
			return 4;
		
		if (osvi2.a != 2)
			return 5;
		
		if (osvi2.b != 3)
			return 6;
		
		Console.WriteLine( "A: {0}", osvi2.a);
		Console.WriteLine( "B: {0}", osvi2.b);

		Console.WriteLine ("Testing with extra parameters at the beginning");

		OSVersionInfo osvi3 = new OSVersionInfo();
		osvi3.a = 1;
		osvi3.b = 2;
		
		if (LibWrap.BugGetVersionEx (10, 10, 10, 10, 20, 20, 20, 20, osvi3) != 5)
			return 7;

		if (osvi3.a != 2)
			return 8;
		
		if (osvi3.b != 3)
			return 9;
		
		Console.WriteLine( "A: {0}", osvi.a);
		Console.WriteLine( "B: {0}", osvi.b);
		
		Console.WriteLine( "\nPassing OSVersionInfo as struct" );
		
		return 0;
	}
}



