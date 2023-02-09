using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    static readonly Program Instance = new();

    [UnmanagedCallersOnly(EntryPoint = "SetExitCodeInManagedSide", CallConvs = new Type[] { typeof(CallConvCdecl) })]
    static void SetExitCode(int exitCode)
    {
        Instance.ExitCode = exitCode;
    }

    int ExitCode;

    static int Main(string[] args)
    {
        Console.WriteLine("hello from managed code");
        return Instance.ExitCode;
    }
}
