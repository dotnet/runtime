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
    public static void print(int a, int b)
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


    public static int Main()
    {
        int s = Gcd(36, 81);
        Console.WriteLine("GCD is " + s);
        if (s != 9) return Fail;
        return Pass;        
    }
}
