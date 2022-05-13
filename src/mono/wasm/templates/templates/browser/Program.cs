using System;
using System.Runtime.CompilerServices;

Console.WriteLine ("Hello, Browser!");

public class MyClass {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string CallMeFromJS()
    {
        return "Hello, World!";
    }
}
