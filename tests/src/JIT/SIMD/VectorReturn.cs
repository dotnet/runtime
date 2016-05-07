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

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector<T> VectorOne<T>() where T: struct
    {
        return Vector<T>.One;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector<T> VectorPlusOne<T>(Vector<T> v1) where T : struct
    {
        Vector<T> v2 = VectorOne<T>();
        return v1 + v2;
    }

    public static int VectorTReturnTest()
    {
        Vector<float> v1 = new Vector<float>(2.0f);
        Vector<float> result1 = VectorPlusOne<float>(v1);
        for (int i=0; i < Vector<float>.Count; ++i)
        {
            if (!CheckValue<float>(result1[i], 3.0f))
            {
                Console.WriteLine("Expected result is " + 3.0f);
                Console.WriteLine("Instead got " + result1[i]);
                Console.WriteLine("FAILED");
                return Fail;
            }
        }

        Vector<int> v2 = new Vector<int>(5);
        Vector<int> result2 = VectorPlusOne<int>(v2);
        for (int i = 0; i < Vector<int>.Count; ++i)
        {
            if (!CheckValue<int>(result2[i], 6))
            {
                Console.WriteLine("Expected result is " + 6);
                Console.WriteLine("Instead got " + result2[i]);
                Console.WriteLine("FAILED");
                return Fail;
            }
        }

        return Pass;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector3 GetVector3One()
    {
        return new Vector3(1.0f);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector3 GetVector3PlusOne(Vector3 v1)
    {
        Vector3 v2 = GetVector3One();
        return v1 + v2;
    }

    public static int Vector3ReturnTest()
    {
        Vector3 v1 = new Vector3(3.0f, 4.0f, 5.0f);
        Vector3 result = GetVector3PlusOne(v1);

        if (!CheckValue<float>(result.X, 4.0f) ||
            !CheckValue<float>(result.Y, 5.0f) ||
            !CheckValue<float>(result.Z, 6.0f))
        {
            Console.WriteLine("Vector3ReturnTest did not return expected value");
            return Fail;
        }

        return Pass;
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

        if (VectorTReturnTest() != Pass)
        {
            Console.WriteLine("FAILED");
            return Fail;
        }

        if (Vector3ReturnTest() != Pass)
        {
            Console.WriteLine("FAILED");
            return Fail;
        }

        Console.WriteLine("PASSED");
        return Pass;
    }
}
