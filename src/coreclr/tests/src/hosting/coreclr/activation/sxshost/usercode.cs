using System;
using System.Reflection;

#if _NONENGLISHCULTURE_
[assembly:AssemblyCultureAttribute("ja-jp")]
#endif

[assembly: AssemblyVersion("5.6.7.8")]
public class EventSink
{
	static public int Click(int x, int y) 
	{
		return (x+y);
	}

	public int Click2(int x, int y) 
	{
		return (x+y);
	}


	public static int Main()
	{
		return 100;
	}
}

