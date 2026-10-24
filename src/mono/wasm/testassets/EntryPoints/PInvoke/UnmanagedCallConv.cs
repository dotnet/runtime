using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

public class Test
{
    public static int Main(string[] args)
    {
        // The P/Invokes only have to be kept and AOT compiled: there is no library to call.
        if (args.Length == 42)
        {
            Cdecl();
            CdeclSuppressGCTransition();
            NoCallConvs();
        }
        return 42;
    }

    [DllImport("nativelib")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl) })]
    internal static extern int Cdecl();

    [DllImport("nativelib")]
    [UnmanagedCallConv(CallConvs = new[] { typeof(CallConvCdecl), typeof(CallConvSuppressGCTransition) })]
    internal static extern int CdeclSuppressGCTransition();

    [DllImport("nativelib")]
    [UnmanagedCallConv]
    internal static extern int NoCallConvs();
}
