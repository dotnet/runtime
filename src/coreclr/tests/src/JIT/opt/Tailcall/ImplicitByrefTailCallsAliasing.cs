// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

public struct S
{
    public long x;
    public long y;
}

// Tail calls with implicit byref parameters as arguments.
//
// We need to ensure that we don't introduce aliased
// implicit byref parameters by optimizing away copies.

public class ImplicitByrefTailCalls
{
    public static void Z() {}

    // Will return different answers if x and y refer to the same struct.
    public static long Alias(S x, S y)
    {
        Z(); Z(); Z(); Z();
        y.x++;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.x++;
            result += x.x + x.y;
        }
        return result;
    }

    // Will return different answers if y refers to some part of x.
    public static unsafe long Alias2(S x, long* y)
    {
        Z(); Z(); Z(); Z();
        *y += 1;
        long result = 0;
        for (int i = 0; i < 100; i++)
        {
            x.x++;
            result += x.x + x.y;
        }
        return result;
    }

    // A must copy params locally when calling Alias
    // and so can't tail call
    public static long A(S x)
    {
        Z(); Z(); Z(); Z();
        return Alias(x, x);
    } 

    // B must copy params locally when calling Alias2
    // and so can't tail call
    public static unsafe long B(S x)
    {
        Z(); Z(); Z(); Z();
        return Alias2(x, &x.y);
    } 

    public static int Main()
    {
        S s = new S();
        s.x = 1;
        s.y = 2;
        long ra = A(s);
        long rb = B(s);

        Console.WriteLine($"{ra},{rb}");
        
        return (ra == 5350) && (rb == 5350) ? 100 : -1;
    }
}
