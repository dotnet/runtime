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
	[DllImport ("libtest")]				
	public static extern int GetVersionEx ([In, Out] OSVersionInfo osvi);

	[DllImport ("libtest", EntryPoint="GetVersionEx" )] 
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

		return 0;
	}
}



