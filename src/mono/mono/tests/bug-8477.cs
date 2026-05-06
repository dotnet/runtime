// This test is meant to make sure Mono doesn't fail when invalid COM invocations are present but never reached.
// See https://bugzilla.xamarin.com/show_bug.cgi?id=8477 for details.

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[ComImport, Guid("06A82D35-8946-4E2E-AE71-DADDE8341F5D")]
class COMponent
{
    [MethodImpl(MethodImplOptions.InternalCall)] public static extern void InCOMplete1 ();
    [MethodImpl(MethodImplOptions.InternalCall)] public extern void InCOMplete2 ();
}

class Test
{
    static void COMmunicate (COMponent c)
    {
	if (c != null)
	    c.InCOMplete2 ();
    }

    static int Main()
    {
	// Check #1: An invocation of a ComImport class method w/o a corresponding interface method must lead to an exception.
	try
	{
	    COMponent.InCOMplete1();
	    // No exception has been thrown, something is wrong.
	    return 1;
	}
	catch (InvalidOperationException)
	{
	    // An exception has been thrown and caught correctly.
	}

	// Check #2: Same as #1, but the method is not executed (i.e. it's located in a "cold" basic block). No exception should be thrown.
	COMmunicate (null);

	return 0;
    }
}
