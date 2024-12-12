using System;
using System.Runtime.InteropServices;

public unsafe partial class Test
{
    public unsafe static int Main(string[] args)
    {
        ((IntPtr)(delegate* unmanaged<int,int>)&Interop.Managed8\u4F60Func).ToString();
        Console.WriteLine($"main: {args.Length}");
        Interop.UnmanagedFunc();

        return 42;
    }
}

namespace Conflict.A {
    file class Interop {
        [UnmanagedCallersOnly(EntryPoint = "ConflictManagedFunc")]
        public static int Managed8\u4F60Func(int number)
        {
            Console.WriteLine($"Conflict.A.Managed8\u4F60Func({number}) -> {number}");
            return number;
        }
    }
}

file partial class Interop
{
    [UnmanagedCallersOnly(EntryPoint = "ManagedFunc")]
    public static int Managed8\u4F60Func(int number)
    {
        // called from UnmanagedFunc
        Console.WriteLine($"Managed8\u4F60Func({number}) -> 42");
        return 42;
    }

    [DllImport("local", EntryPoint = "UnmanagedFunc")]
    public static extern void UnmanagedFunc(); // calls ManagedFunc
}
