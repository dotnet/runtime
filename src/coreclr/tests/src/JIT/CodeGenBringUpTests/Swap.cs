// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//


using System;
using System.Runtime.CompilerServices;
public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void Swap(ref int a, ref int b)
    {
      int t = a;
      a = b;
      b = t;
    }


    public static int Main()
    {
        int a = 10, b= 20;
        Console.WriteLine("Before swap: " + a + "," + b);
        Swap(ref a, ref b);
        Console.WriteLine("After swap: " + a + "," + b);
        if (a==20 && b== 10) return Pass;
        return Fail;        
    }
}
