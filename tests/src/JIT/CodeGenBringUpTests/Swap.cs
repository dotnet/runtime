// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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
