using System;
using System.Runtime.CompilerServices;

public class TlsClass<T> {
	[ThreadStatic]
	public static string staticTls;
}

public class AccessClass<T> {

	[MethodImpl(MethodImplOptions.NoInlining)]
	public string GetStatic ()
	{
		// Get field offset through rgctx_fetch of a class not yet initialized
		return TlsClass<T>.staticTls;
	}
}

public class Program {

	public static int Main (string[] args)
	{
		AccessClass<string> ac1 = new AccessClass<string> ();
		if (ac1.GetStatic () != null)
			return 1;

		return 0;
	}

}
