using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main()
    {
        Console.WriteLine("TestOutput -> Main running");
        return 42;
    }

    [DllImport("variadic", EntryPoint="sum")]
    public unsafe static extern int using_sum_one(delegate* unmanaged<char*, IntPtr, void> callback);

    [DllImport("variadic", EntryPoint="sum")]
    public static extern int sum_one(int a, int b);
}
