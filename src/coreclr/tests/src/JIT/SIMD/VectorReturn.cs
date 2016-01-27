// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;
    private static Vector2[] s_A;
    private static Vector2 s_p0;
    private static Vector2 s_p1;
    private static Vector2 s_p2;
    private static Vector2 s_p3;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void init()
    {
        s_A = new Vector2[10];
        Random random = new Random(100);
        for (int i = 0; i < 10; i++)
        {
            s_A[i] = new Vector2(random.Next(100));
        }
        s_p0 = new Vector2(random.Next(100));
        s_p1 = new Vector2(random.Next(100));
        s_p2 = new Vector2(random.Next(100));
        s_p3 = new Vector2(random.Next(100));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 F1(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        return (t0 * s_p0) + (t1 * s_p1) + (t2 * s_p2) + (t3 * s_p3);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector2 F2(float u)
    {
        if (u < 0)
            return s_A[0];
        if (u >= 1)
            return s_A[1];
        if (u < 0.1)
            return s_A[2];
        if (u > 0.9)
            return s_A[3];
        return F1(u);
    }

    public static int Main()
    {
        init();
        Vector2 result = F2(0.5F);
        Vector2 expectedResult = F1(0.5F);
        Console.WriteLine("Result is " + result.ToString());
        if (!CheckValue<float>(result.X, expectedResult.X) || !CheckValue<float>(result.Y, expectedResult.Y))
        {
            Console.WriteLine("Expected result is " + expectedResult.ToString());
            Console.WriteLine("FAILED");
            return Fail;
        }
        Console.WriteLine("PASSED");
        return Pass;
    }
}
