using System;
using System.Runtime.InteropServices;
public class Test
{
    public static int Main(string[] args)
    {
        Console.WriteLine("TestOutput -> Main running");
        if (args.Length > 2)
        {
            // We don't want to run this, because we can't call variadic functions
            Console.WriteLine($"sum_three: {sum_three(7, 14, 21)}");
            Console.WriteLine($"sum_two: {sum_two(3, 6)}");
            Console.WriteLine($"sum_one: {sum_one(5)}");
        }
        return 42;
    }

    [DllImport("variadic", EntryPoint="sum")] public static extern int sum_one(int a);
    [DllImport("variadic", EntryPoint="sum")] public static extern int sum_two(int a, int b);
    [DllImport("variadic", EntryPoint="sum")] public static extern int sum_three(int a, int b, int c);
}
