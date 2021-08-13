// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Numerics;
using System.Runtime.CompilerServices;

internal partial class VectorTest
{
    private const int Pass = 100;
    private const int Fail = -1;

    private static Vector2[] s_v2_array;
    private static Vector2 s_v2_0;
    private static Vector2 s_v2_1;
    private static Vector2 s_v2_2;
    private static Vector2 s_v2_3;

    private static Vector3[] s_v3_array;
    private static Vector3 s_v3_0;
    private static Vector3 s_v3_1;
    private static Vector3 s_v3_2;
    private static Vector3 s_v3_3;

    private static Vector4[] s_v4_array;
    private static Vector4 s_v4_0;
    private static Vector4 s_v4_1;
    private static Vector4 s_v4_2;
    private static Vector4 s_v4_3;

    private const int DefaultSeed = 20010415;
    private static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static void init()
    {
        Random random = new Random(Seed);

        s_v2_array = new Vector2[10];
        for (int i = 0; i < 10; i++)
        {
            s_v2_array[i] = new Vector2(random.Next(100));
        }
        s_v2_0 = new Vector2(random.Next(100));
        s_v2_1 = new Vector2(random.Next(100));
        s_v2_2 = new Vector2(random.Next(100));
        s_v2_3 = new Vector2(random.Next(100));

        s_v3_array = new Vector3[10];
        for (int i = 0; i < 10; i++)
        {
            s_v3_array[i] = new Vector3(random.Next(100));
        }
        s_v3_0 = new Vector3(random.Next(100));
        s_v3_1 = new Vector3(random.Next(100));
        s_v3_2 = new Vector3(random.Next(100));
        s_v3_3 = new Vector3(random.Next(100));

        s_v4_array = new Vector4[10];
        for (int i = 0; i < 10; i++)
        {
            s_v4_array[i] = new Vector4(random.Next(100));
        }
        s_v4_0 = new Vector4(random.Next(100));
        s_v4_1 = new Vector4(random.Next(100));
        s_v4_2 = new Vector4(random.Next(100));
        s_v4_3 = new Vector4(random.Next(100));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 F1_v2(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        return (t0 * s_v2_0) + (t1 * s_v2_1) + (t2 * s_v2_2) + (t3 * s_v2_3);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector2 F2_v2(float u)
    {
        if (u < 0)
            return s_v2_array[0];
        if (u >= 1)
            return s_v2_array[1];
        if (u < 0.1)
            return s_v2_array[2];
        if (u > 0.9)
            return s_v2_array[3];
        return F1_v2(u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 F1_v3(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        return (t0 * s_v3_0) + (t1 * s_v3_1) + (t2 * s_v3_2) + (t3 * s_v3_3);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector3 F2_v3(float u)
    {
        if (u < 0)
            return s_v3_array[0];
        if (u >= 1)
            return s_v3_array[1];
        if (u < 0.1)
            return s_v3_array[2];
        if (u > 0.9)
            return s_v3_array[3];
        return F1_v3(u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector4 F1_v4(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        return (t0 * s_v4_0) + (t1 * s_v4_1) + (t2 * s_v4_2) + (t3 * s_v4_3);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector4 F2_v4(float u)
    {
        if (u < 0)
            return s_v4_array[0];
        if (u >= 1)
            return s_v4_array[1];
        if (u < 0.1)
            return s_v4_array[2];
        if (u > 0.9)
            return s_v4_array[3];
        return F1_v4(u);
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

        Vector2 result_v2 = F2_v2(0.5F);
        Vector2 expectedResult_v2 = F1_v2(0.5F);
        Console.WriteLine("Result is " + result_v2.ToString());
        if (!CheckValue<float>(result_v2.X, expectedResult_v2.X) || !CheckValue<float>(result_v2.Y, expectedResult_v2.Y))
        {
            Console.WriteLine("Expected result is " + expectedResult_v2.ToString());
            Console.WriteLine("Vector2 test FAILED");
            return Fail;
        }

        Vector3 result_v3 = F2_v3(0.6F);
        Vector3 expectedResult_v3 = F1_v3(0.6F);
        Console.WriteLine("Result is " + result_v3.ToString());
        if (!CheckValue<float>(result_v3.X, expectedResult_v3.X) ||
            !CheckValue<float>(result_v3.Y, expectedResult_v3.Y) ||
            !CheckValue<float>(result_v3.Z, expectedResult_v3.Z))
        {
            Console.WriteLine("Expected result is " + expectedResult_v3.ToString());
            Console.WriteLine("Vector3 test FAILED");
            return Fail;
        }

        Vector4 result_v4 = F2_v4(0.7F);
        Vector4 expectedResult_v4 = F1_v4(0.7F);
        Console.WriteLine("Result is " + result_v4.ToString());
        if (!CheckValue<float>(result_v4.X, expectedResult_v4.X) ||
            !CheckValue<float>(result_v4.Y, expectedResult_v4.Y) ||
            !CheckValue<float>(result_v4.Z, expectedResult_v4.Z) ||
            !CheckValue<float>(result_v4.W, expectedResult_v4.W))
        {
            Console.WriteLine("Expected result is " + expectedResult_v4.ToString());
            Console.WriteLine("Vector4 test FAILED");
            return Fail;
        }

        if (VectorTReturnTest() != Pass)
        {
            Console.WriteLine("VectorTReturnTest FAILED");
            return Fail;
        }

        if (Vector3ReturnTest() != Pass)
        {
            Console.WriteLine("Vector3ReturnTest FAILED");
            return Fail;
        }

        Console.WriteLine("PASSED");
        return Pass;
    }
}
