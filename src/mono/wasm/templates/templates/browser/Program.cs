using System;
using System.Runtime.CompilerServices;

Console.WriteLine ("Hello, Console!");

public class MyClass {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string CallMeFromJS()
    {
        return "Hello, World!";
    }
}
