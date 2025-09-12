using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: DisableRuntimeMarshalling]
public class Test
{
    public static int Main()
    {
        var x = new S { Value = 5 };

        Console.WriteLine("TestOutput -> Main running " + x.Value);
        return 42;
    }

    [StructLayout(LayoutKind.Auto)]
    public struct S { public int Value; public float Value2; }

    [UnmanagedCallersOnly]
    public static void M(S myStruct) { }
}
