using System;
using System.Reflection;

public class Repro
{

	public static int Main()
	{
		try
		{
#if DESKTOP
			Assembly a = Assembly.Load("system, processorArchitecture=somebadvalue");
#else 
            AssemblyName an = new AssemblyName("system, processorArchitecture=somebadvalue");
#endif
		}
		catch(System.IO.FileLoadException e)
		{
			if(e.ToString().ToUpper().IndexOf("UNKNOWN ERROR") == -1)
			{
				//we didn't get "Unknown error" in the exception text
				Console.WriteLine("Pass");
				return 100;
			} 
			else
			{
				Console.WriteLine("Wrong exception text: " + e.ToString());
				Console.WriteLine("FAIL");
				return 101;
			}
		}
		Console.WriteLine("Didn't catch FileLoadException. FAIL");
		return 99;
	}
}
	