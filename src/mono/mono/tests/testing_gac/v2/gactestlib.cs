
[assembly: System.Reflection.AssemblyVersion ("1.1.0.0")]

public class X
{
	public static void N1 ()
	{
		
	}

	// In the "v2" version, let's make this method missing.
	// public static void N2 ()
	// {
	// }
}

public class OnlyInV2 {
	public static void M () { }
}
