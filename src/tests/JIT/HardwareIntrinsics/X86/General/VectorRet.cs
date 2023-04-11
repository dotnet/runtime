// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This test case is ported from S.N.Vector counterpart
// https://github.com/dotnet/runtime/blob/main/src/tests/JIT/SIMD/VectorReturn.cs

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

namespace IntelHardwareIntrinsicTest.General;
public partial class Program
{
    private static Vector128<float>[] s_v128_array;
    private static Vector128<float> s_v128_0;
    private static Vector128<float> s_v128_1;
    private static Vector128<float> s_v128_2;
    private static Vector128<float> s_v128_3;

    private static Vector128<short>[] s_v128i_array;
    private static Vector128<short> s_v128i_0;
    private static Vector128<short> s_v128i_1;
    private static Vector128<short> s_v128i_2;
    private static Vector128<short> s_v128i_3;

    private static Vector256<float>[] s_v256_array;
    private static Vector256<float> s_v256_0;
    private static Vector256<float> s_v256_1;
    private static Vector256<float> s_v256_2;
    private static Vector256<float> s_v256_3;

    private static Vector256<byte>[] s_v256i_array;
    private static Vector256<byte> s_v256i_0;
    private static Vector256<byte> s_v256i_1;
    private static Vector256<byte> s_v256i_2;
    private static Vector256<byte> s_v256i_3;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    static void init()
    {
        Random random = new Random(100);

        if (Sse.IsSupported)
        {
            s_v128_array = new Vector128<float>[10];
            for (int i = 0; i < 10; i++)
            {
                s_v128_array[i] = Vector128.Create((float)random.Next(100));
            }
            s_v128_0 = Vector128.Create((float)random.Next(100));
            s_v128_1 = Vector128.Create((float)random.Next(100));
            s_v128_2 = Vector128.Create((float)random.Next(100));
            s_v128_3 = Vector128.Create((float)random.Next(100));
        }

        if (Sse2.IsSupported)
        {
            s_v128i_array = new Vector128<short>[10];
            for (int i = 0; i < 10; i++)
            {
                s_v128i_array[i] = Vector128.Create((short)random.Next(100));
            }
            s_v128i_0 = Vector128.Create((short)random.Next(100));
            s_v128i_1 = Vector128.Create((short)random.Next(100));
            s_v128i_2 = Vector128.Create((short)random.Next(100));
            s_v128i_3 = Vector128.Create((short)random.Next(100));
        }

        if (Avx.IsSupported)
        {
            s_v256_array = new Vector256<float>[10];
            for (int i = 0; i < 10; i++)
            {
                s_v256_array[i] = Vector256.Create((float)random.Next(100));
            }
            s_v256_0 = Vector256.Create((float)random.Next(100));
            s_v256_1 = Vector256.Create((float)random.Next(100));
            s_v256_2 = Vector256.Create((float)random.Next(100));
            s_v256_3 = Vector256.Create((float)random.Next(100));
        }

        if (Avx2.IsSupported)
        {
            s_v256i_array = new Vector256<byte>[10];
            for (int i = 0; i < 10; i++)
            {
                s_v256i_array[i] = Vector256.Create((byte)random.Next(100));
            }
            s_v256i_0 = Vector256.Create((byte)random.Next(100));
            s_v256i_1 = Vector256.Create((byte)random.Next(100));
            s_v256i_2 = Vector256.Create((byte)random.Next(100));
            s_v256i_3 = Vector256.Create((byte)random.Next(100));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<float> F1_v128(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        Vector128<float> tmp1 = Sse.Add(Sse.Subtract(Vector128.Create(t0), s_v128_0), Sse.Subtract(Vector128.Create(t1), s_v128_1));
        Vector128<float> tmp2 = Sse.Add(Sse.Subtract(Vector128.Create(t2), s_v128_2), Sse.Subtract(Vector128.Create(t3), s_v128_3));
        return Sse.Add(tmp1, tmp2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector128<float> F2_v128(float u)
    {
        if (u < 0)
            return s_v128_array[0];
        if (u >= 1)
            return s_v128_array[1];
        if (u < 0.1)
            return s_v128_array[2];
        if (u > 0.9)
            return s_v128_array[3];
        return F1_v128(u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector128<short> F1_v128i(int t)
    {
        int ti = 1 - t;
        int t0 = ti * ti * ti;
        int t1 = 3 * ti * ti * t;
        int t2 = 3 * ti * t * t;
        int t3 = t * t * t;
        Vector128<short> tmp1 = Sse2.Add(Sse2.Subtract(Vector128.Create((short)t0), s_v128i_0), Sse2.Subtract(Vector128.Create((short)t1), s_v128i_1));
        Vector128<short> tmp2 = Sse2.Add(Sse2.Subtract(Vector128.Create((short)t2), s_v128i_2), Sse2.Subtract(Vector128.Create((short)t3), s_v128i_3));
        return Sse2.Add(tmp1, tmp2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector128<short> F2_v128i(short u)
    {
        if (u < 0)
            return s_v128i_array[0];
        if (u >= 10)
            return s_v128i_array[1];
        if (u < 0.1)
            return s_v128i_array[2];
        if (u > 90)
            return s_v128i_array[3];
        return F1_v128i(u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<float> F1_v256(float t)
    {
        float ti = 1 - t;
        float t0 = ti * ti * ti;
        float t1 = 3 * ti * ti * t;
        float t2 = 3 * ti * t * t;
        float t3 = t * t * t;
        Vector256<float> tmp1 = Avx.Add(Avx.Subtract(Vector256.Create(t0), s_v256_0), Avx.Subtract(Vector256.Create(t1), s_v256_1));
        Vector256<float> tmp2 = Avx.Add(Avx.Subtract(Vector256.Create(t2), s_v256_2), Avx.Subtract(Vector256.Create(t3), s_v256_3));
        return Avx.Add(tmp1, tmp2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector256<float> F2_v256(float u)
    {
        if (u < 0)
            return s_v256_array[0];
        if (u >= 1)
            return s_v256_array[1];
        if (u < 0.1)
            return s_v256_array[2];
        if (u > 0.9)
            return s_v256_array[3];
        return F1_v256(u);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector256<byte> F1_v256i(int t)
    {
        int ti = 1 - t;
        int t0 = ti * ti * ti;
        int t1 = 3 * ti * ti * t;
        int t2 = 3 * ti * t * t;
        int t3 = t * t * t;
        Vector256<byte> tmp1 = Avx2.Add(Avx2.Subtract(Vector256.Create((byte)t0), s_v256i_0), Avx2.Subtract(Vector256.Create((byte)t1), s_v256i_1));
        Vector256<byte> tmp2 = Avx2.Add(Avx2.Subtract(Vector256.Create((byte)t2), s_v256i_2), Avx2.Subtract(Vector256.Create((byte)t3), s_v256i_3));
        return Avx2.Add(tmp1, tmp2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector256<byte> F2_v256i(byte u)
    {
        if (u < 0)
            return s_v256i_array[0];
        if (u >= 10)
            return s_v256i_array[1];
        if (u < 0.1)
            return s_v256i_array[2];
        if (u > 90)
            return s_v256i_array[3];
        return F1_v256i(u);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector128<T> Vector128One<T>() where T : struct
    {
        return CreateVector128(GetValueFromInt<T>(1));
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector256<T> Vector256One<T>() where T : struct
    {
        return CreateVector256(GetValueFromInt<T>(1));
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector128<T> Vector128PlusOne<T>(Vector128<T> v1) where T : struct
    {
        Vector128<T> v2 = Vector128One<T>();
        return Vector128Add<T>(v1, v2);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector256<T> Vector256PlusOne<T>(Vector256<T> v1) where T : struct
    {
        Vector256<T> v2 = Vector256One<T>();
        return Vector256Add<T>(v1, v2);
    }

    public static unsafe int Vector128ReturnTest()
    {
        Vector128<float> v1 = Vector128.Create(2.0f);
        Vector128<float> vres1 = Vector128PlusOne<float>(v1);

        float* result1 = stackalloc float[4];
        Sse.Store(result1, vres1);

        for (int i = 0; i < 4; ++i)
        {
            if (result1[i] != 3.0f)
            {
                Console.WriteLine("Expected result is " + 3.0f);
                Console.WriteLine("Instead got " + result1[i]);
                Console.WriteLine("FAILED");
                return Fail;
            }
        }


        Vector128<int> v2 = Vector128.Create((int)5);
        Vector128<int> vres2 = Vector128PlusOne<int>(v2);

        int* result2 = stackalloc int[4];
        Sse2.Store(result2, vres2);

        for (int i = 0; i < 4; ++i)
        {
            if (result2[i] != 6)
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
    public static Vector128<short> GetVector128Int16One()
    {
        return Vector128.Create((short)1);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector128<short> GetVector128Int16PlusOne(Vector128<short> v1)
    {
        Vector128<short> v2 = GetVector128Int16One();
        return Sse2.Add(v1, v2);
    }

    public static unsafe int Vector128Int16ReturnTest()
    {
        Vector128<short> v1 = Vector128.Create(3, 4, 5, 6, 7, 8, 9, 10);
        Vector128<short> vres = GetVector128Int16PlusOne(v1);

        short* result = stackalloc short[8];
        Sse2.Store(result, vres);

        if ((result[0] != 4) || (result[1] != 5) || (result[2] != 6) || (result[3] != 7) ||
            (result[4] != 8) || (result[5] != 9) || (result[6] != 10) || (result[7] != 11))
        {
            Console.WriteLine("Vector128Int16ReturnTest did not return expected value");
            Console.Write("[ ");
            for (int i = 0; i < 8; i++)
            {
                Console.Write(result[i] + ", ");
            }
            Console.Write("]");
            return Fail;
        }

        return Pass;
    }

    public static unsafe int Vector256ReturnTest()
    {
        Vector256<float> v1 = Vector256.Create(2.0f);
        Vector256<float> vres1 = Vector256PlusOne<float>(v1);

        float* result1 = stackalloc float[8];
        Avx.Store(result1, vres1);

        for (int i = 0; i < 8; ++i)
        {
            if (result1[i] != 3.0f)
            {
                Console.WriteLine("Expected result is " + 3.0f);
                Console.WriteLine("Instead got " + result1[i]);
                Console.WriteLine("FAILED");
                return Fail;
            }
        }


        Vector256<int> v2 = Vector256.Create((int)5);
        Vector256<int> vres2 = Vector256PlusOne<int>(v2);

        int* result2 = stackalloc int[8];
        Avx.Store(result2, vres2);

        for (int i = 0; i < 8; ++i)
        {
            if (result2[i] != 6)
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
    public static Vector256<int> GetVector256Int32One()
    {
        return Vector256.Create(1);
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static Vector256<int> GetVector256Int32PlusOne(Vector256<int> v1)
    {
        Vector256<int> v2 = GetVector256Int32One();
        return Avx2.Add(v1, v2);
    }

    public static unsafe int Vector256Int32ReturnTest()
    {
        Vector256<int> v1 = Vector256.Create(3, 4, 5, 6, 7, 8, 9, 10);
        Vector256<int> vres = GetVector256Int32PlusOne(v1);

        int* result = stackalloc int[8];
        Avx.Store(result, vres);

        if ((result[0] != 4) || (result[1] != 5) || (result[2] != 6) || (result[3] != 7) ||
            (result[4] != 8) || (result[5] != 9) || (result[6] != 10) || (result[7] != 11))
        {
            Console.WriteLine("Vector256Int32ReturnTest did not return expected value");
            Console.Write("[ ");
            for (int i = 0; i < 8; i++)
            {
                Console.Write(result[i] + ", ");
            }
            Console.Write("]");
            return Fail;
        }

        return Pass;
    }

    [Xunit.ActiveIssue("https://github.com/dotnet/runtime/issues/75767", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMonoLLVMAOT))]
    [Fact]
    public static unsafe void VectorRet()
    {
        init();

        if (Sse2.IsSupported)
        {
            Vector128<float> result_v128 = F2_v128(0.5F);
            Vector128<float> expectedResult_v128 = F1_v128(0.5F);

            float* result = stackalloc float[4];
            Sse.Store(result, result_v128);
            float* expResult = stackalloc float[4];
            Sse.Store(expResult, expectedResult_v128);

            for (int i = 0; i < 4; i++)
            {
                if (result[i] != expResult[i])
                {
                    Console.WriteLine("Vector128<float> test FAILED");
                    Console.Write("[ ");
                    for (int j = 0; j < 4; j++)
                    {
                        Console.Write(result[j] + ", ");
                    }
                    Console.WriteLine("]");
                    Assert.Fail("");
                }
            }

            if (Vector128ReturnTest() != Pass)
            {
                Console.WriteLine("Vector128ReturnTest FAILED");
                Assert.Fail("");
            }

            Vector128<short> result_v128i = F2_v128i(6);
            Vector128<short> expectedResult_v128i = F1_v128i(6);

            short* results = stackalloc short[8];
            Sse2.Store(results, result_v128i);
            short* expResults = stackalloc short[8];
            Sse2.Store(expResults, expectedResult_v128i);

            for (int i = 0; i < 8; i++)
            {
                if (results[i] != expResults[i])
                {
                    Console.WriteLine("Vector128<short> test FAILED");
                    Console.Write("[ ");
                    for (int j = 0; j < 8; j++)
                    {
                        Console.Write(results[j] + ", ");
                    }
                    Console.WriteLine("]");
                    Assert.Fail("");
                }
            }

            if (Vector128Int16ReturnTest() != Pass)
            {
                Console.WriteLine("Vector128Int16ReturnTest FAILED");
                Assert.Fail("");
            }
        }

        if (Avx2.IsSupported)
        {
            Vector256<float> result_v256 = F2_v256(0.7F);
            Vector256<float> expectedResult_v256 = F1_v256(0.7F);

            float* result = stackalloc float[8];
            Avx.Store(result, result_v256);
            float* expResult = stackalloc float[8];
            Avx.Store(expResult, expectedResult_v256);

            for (int i = 0; i < 8; i++)
            {
                if (result[i] != expResult[i])
                {
                    Console.WriteLine("Vector256<float> test FAILED");
                    Console.Write("[ ");
                    for (int j = 0; j < 8; j++)
                    {
                        Console.Write(result[j] + ", ");
                    }
                    Console.WriteLine("]");
                    Assert.Fail("");
                }
            }

            if (Vector256ReturnTest() != Pass)
            {
                Console.WriteLine("Vector256ReturnTest FAILED");
                Assert.Fail("");
            }

            Vector256<byte> result_v256i = F2_v256i(7);
            Vector256<byte> expectedResult_v256i = F1_v256i(7);

            byte* resultb = stackalloc byte[32];
            Avx.Store(resultb, result_v256i);
            byte* expResultb = stackalloc byte[32];
            Avx.Store(expResultb, expectedResult_v256i);

            for (int i = 0; i < 32; i++)
            {
                if (resultb[i] != expResultb[i])
                {
                    Console.WriteLine("Vector256<byte> test FAILED");
                    Console.Write("[ ");
                    for (int j = 0; j < 32; j++)
                    {
                        Console.Write(resultb[j] + ", ");
                    }
                    Console.WriteLine("]");
                    Assert.Fail("");
                }
            }

            if (Vector256Int32ReturnTest() != Pass)
            {
                Console.WriteLine("Vector128Int16ReturnTest FAILED");
                Assert.Fail("");
            }
        }

        Console.WriteLine("PASSED");
    }
}
