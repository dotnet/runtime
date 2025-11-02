using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main()
    {
        Console.WriteLine("TestOutput -> Main running");
        return 42;
    }
}

file class Foo
{
    [UnmanagedCallersOnly]
    public unsafe static extern void SomeFunction1(int i);
}
