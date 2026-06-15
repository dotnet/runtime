using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

public class Test
{
    public static int Main(string[] args)
    {
        var s = new STGMEDIUM();
        ReleaseStgMedium(ref s);
        return 42;
    }

    [DllImport("ole32.dll")]
    internal static extern void ReleaseStgMedium(ref STGMEDIUM medium);
}
