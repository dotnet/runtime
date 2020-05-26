using System.Runtime.CompilerServices;

#if IVT_INCLUDED
[assembly: InternalsVisibleTo ("library")]
#endif

class Internals
{
	internal static void CallStatic ()
	{
	}
}