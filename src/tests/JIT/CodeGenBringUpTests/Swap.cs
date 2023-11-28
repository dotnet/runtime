// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;
public class BringUpTest_Swap
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    internal static void Swap(ref int a, ref int b)
    {
      int t = a;
      a = b;
      b = t;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        int a = 10, b= 20;
        Console.WriteLine("Before swap: " + a + "," + b);
        Swap(ref a, ref b);
        Console.WriteLine("After swap: " + a + "," + b);
        if (a==20 && b== 10) return Pass;
        return Fail;        
    }
}
