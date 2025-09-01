using System;
using System.Runtime.InteropServices;
public unsafe class Test
{
    [UnmanagedCallersOnly(EntryPoint = "MyCallback")]
    public static int MyCallback()
    {
        Console.WriteLine("TestOutput -> WASM Library MyCallback is called");
        return 100;
    }
}
