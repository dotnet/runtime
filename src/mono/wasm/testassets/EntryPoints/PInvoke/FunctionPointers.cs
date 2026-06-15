using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main()
    {
        Console.WriteLine("TestOutput -> Main running");
        return 42;
    }

    [DllImport("someting")]
    public unsafe static extern void SomeFunction1(delegate* unmanaged<int> callback);
}
