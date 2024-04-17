using System;
using System.Runtime.InteropServices;

public unsafe partial class Test
{
    public unsafe static int Main(string[] args)
    {
        ((IntPtr)(delegate* unmanaged<int,int>)&Interop.ManagedFunc).ToString();

        Console.WriteLine($"main: {args.Length}");
        Interop.UnmanagedFunc();
        return 42;
    }
}

file partial class Interop
{
    [UnmanagedCallersOnly(EntryPoint = "ManagedFunc")]
    public static int ManagedFunc(int number)
    {
        // called UnmanagedFunc
        Console.WriteLine($"ManagedFunc({number}) -> 42");
        return 42;
    }

    [DllImport("local", EntryPoint = "UnmanagedFunc")]
    public static extern void UnmanagedFunc(); // calls ManagedFunc
}
