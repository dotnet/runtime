// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Gcd
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void print(int a, int b)
    {
         Console.WriteLine("GCD: " + a + "," + b);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static int Gcd(int a, int b)
    {
        print(a, b);
        int result;
        if (b == 0) 
          result = a;
        else if (a < b) 
          result = Gcd(b, a);
        else
          result = Gcd(b, a%b);

        return result;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        int s = Gcd(36, 81);
        Console.WriteLine("GCD is " + s);
        if (s != 9) return Fail;
        return Pass;        
    }
}
