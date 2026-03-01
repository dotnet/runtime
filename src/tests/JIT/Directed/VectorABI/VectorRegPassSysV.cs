// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

// Test that Vector128, Vector256, and Vector512 are correctly passed as arguments
// and returned from methods on System V x64 (Linux), verifying the single-register
// SIMD passing path in the JIT's SysVX64Classifier.
//
// Vector128 (16B) -> XMM register
// Vector256 (32B) -> YMM register (requires AVX)
// Vector512 (64B) -> ZMM register (requires AVX-512)

public static class VectorRegPassSysV
{
    private const int PASS = 100;
    private const int FAIL = 0;

    // --- Vector128 tests ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> AddVec128(Vector128<int> a, Vector128<int> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> PassManyVec128(Vector128<int> a, Vector128<int> b, Vector128<int> c,
                                         Vector128<int> d, Vector128<int> e, int scalar)
    {
        return a + b + c + d + e + Vector128.Create(scalar);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> MixedArgsVec128(int x, Vector128<float> v, long y)
    {
        return v + Vector128.Create((float)(x + y));
    }

    // --- Vector256 tests (require AVX) ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> AddVec256(Vector256<int> a, Vector256<int> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> PassManyVec256(Vector256<int> a, Vector256<int> b, Vector256<int> c,
                                         Vector256<int> d, int scalar)
    {
        return a + b + c + d + Vector256.Create(scalar);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<float> MixedArgsVec256(int x, Vector256<float> v, long y)
    {
        return v + Vector256.Create((float)(x + y));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> ReturnVec256(int value)
    {
        return Vector256.Create(value);
    }

    // --- Vector512 tests (require AVX-512) ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> AddVec512(Vector512<int> a, Vector512<int> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> ReturnVec512(int value)
    {
        return Vector512.Create(value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<float> MixedArgsVec512(int x, Vector512<float> v, long y)
    {
        return v + Vector512.Create((float)(x + y));
    }

    // --- Vector64 tests ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<int> AddVec64(Vector64<int> a, Vector64<int> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector64<float> ReturnVec64(float a, float b)
    {
        return Vector64.Create(a, b);
    }

    // --- Chained return tests (return of one call feeds into the next) ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> ChainVec128(Vector128<float> a, Vector128<float> b,
                                        Vector128<float> c, Vector128<float> d)
    {
        return AddVec128F(AddVec128F(a, b), AddVec128F(c, d));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<float> AddVec128F(Vector128<float> a, Vector128<float> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<float> ChainVec256(Vector256<float> a, Vector256<float> b,
                                        Vector256<float> c, Vector256<float> d)
    {
        return AddVec256F(AddVec256F(a, b), AddVec256F(c, d));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<float> AddVec256F(Vector256<float> a, Vector256<float> b)
    {
        return a + b;
    }

    // --- Vector128<double> return tests ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<double> AddVec128D(Vector128<double> a, Vector128<double> b)
    {
        return a + b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<double> AddVec256D(Vector256<double> a, Vector256<double> b)
    {
        return a + b;
    }

    // --- Multi-size mixing: different vector widths in one call ---

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector128<int> MixedSizes(Vector128<int> a, Vector256<int> b, int scalar)
    {
        return a + b.GetLower() + Vector128.Create(scalar);
    }

    // --- Return into struct field ---

    struct VectorPair128
    {
        public Vector128<int> Lo;
        public Vector128<int> Hi;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static VectorPair128 ReturnPair128(Vector128<int> a, Vector128<int> b)
    {
        VectorPair128 result;
        result.Lo = AddVec128(a, Vector128.Create(1));
        result.Hi = AddVec128(b, Vector128.Create(2));
        return result;
    }

    // --- Test helpers ---

    static bool Check<T>(T actual, T expected, string testName) where T : IEquatable<T>
    {
        if (!actual.Equals(expected))
        {
            Console.WriteLine($"FAIL: {testName}: expected {expected}, got {actual}");
            return false;
        }
        return true;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = true;

        // --- Vector64 tests ---
        Console.WriteLine("=== Vector64 tests ===");

        pass &= Check(AddVec64(Vector64.Create(1, 2), Vector64.Create(10, 20)),
            Vector64.Create(11, 22), "AddVec64");

        pass &= Check(ReturnVec64(3.0f, 4.0f), Vector64.Create(3.0f, 4.0f), "ReturnVec64");

        // --- Vector128 tests (always available on x64) ---
        Console.WriteLine("=== Vector128 tests ===");

        var v128a = Vector128.Create(1, 2, 3, 4);
        var v128b = Vector128.Create(10, 20, 30, 40);
        pass &= Check(AddVec128(v128a, v128b), Vector128.Create(11, 22, 33, 44), "AddVec128");

        pass &= Check(
            PassManyVec128(
                Vector128.Create(1), Vector128.Create(2), Vector128.Create(3),
                Vector128.Create(4), Vector128.Create(5), 100),
            Vector128.Create(115),
            "PassManyVec128");

        pass &= Check(MixedArgsVec128(3, Vector128.Create(10.0f), 7L),
            Vector128.Create(20.0f), "MixedArgsVec128");

        // --- Vector128<double> tests ---
        pass &= Check(AddVec128D(Vector128.Create(1.5, 2.5), Vector128.Create(3.0, 4.0)),
            Vector128.Create(4.5, 6.5), "AddVec128D");

        // --- Vector128 chained return tests ---
        // ChainVec128: (1+2) + (0.5+0.1) = 3.6
        pass &= Check(
            ChainVec128(Vector128.Create(1.0f), Vector128.Create(2.0f),
                        Vector128.Create(0.5f), Vector128.Create(0.1f)),
            Vector128.Create(3.6f), "ChainVec128");

        // --- Vector256 tests ---
        Console.WriteLine("=== Vector256 tests ===");
        Console.WriteLine($"  Avx.IsSupported = {Avx.IsSupported}");

        if (Avx.IsSupported)
        {
            var v256a = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
            var v256b = Vector256.Create(10, 20, 30, 40, 50, 60, 70, 80);
            pass &= Check(AddVec256(v256a, v256b),
                Vector256.Create(11, 22, 33, 44, 55, 66, 77, 88), "AddVec256");

            pass &= Check(
                PassManyVec256(
                    Vector256.Create(1), Vector256.Create(2), Vector256.Create(3),
                    Vector256.Create(4), 100),
                Vector256.Create(110),
                "PassManyVec256");

            pass &= Check(MixedArgsVec256(3, Vector256.Create(10.0f), 7L),
                Vector256.Create(20.0f), "MixedArgsVec256");

            pass &= Check(ReturnVec256(42), Vector256.Create(42), "ReturnVec256");

            // --- Vector256<double> tests ---
            pass &= Check(AddVec256D(Vector256.Create(1.0), Vector256.Create(2.0)),
                Vector256.Create(3.0), "AddVec256D");

            // --- Vector256 chained return tests ---
            pass &= Check(
                ChainVec256(Vector256.Create(1.0f), Vector256.Create(2.0f),
                            Vector256.Create(0.5f), Vector256.Create(0.1f)),
                Vector256.Create(3.6f), "ChainVec256");
        }
        else
        {
            Console.WriteLine("  Skipping Vector256 tests because AVX is not supported.");
        }

        // --- Vector512 tests ---
        Console.WriteLine("=== Vector512 tests ===");
        Console.WriteLine($"  Avx512F.IsSupported = {Avx512F.IsSupported}");

        if (Avx512F.IsSupported)
        {
            var v512a = Vector512.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            var v512b = Vector512.Create(10, 20, 30, 40, 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160);
            pass &= Check(AddVec512(v512a, v512b),
                Vector512.Create(11, 22, 33, 44, 55, 66, 77, 88, 99, 110, 121, 132, 143, 154, 165, 176),
                "AddVec512");

            pass &= Check(ReturnVec512(99), Vector512.Create(99), "ReturnVec512");

            pass &= Check(MixedArgsVec512(3, Vector512.Create(10.0f), 7L),
                Vector512.Create(20.0f), "MixedArgsVec512");
        }
        else
        {
            Console.WriteLine("  Skipping Vector512 tests because AVX-512 is not supported.");
        }

        // --- Multi-size mixing tests ---
        Console.WriteLine("=== Multi-size mixing tests ===");

        if (Avx.IsSupported)
        {
            pass &= Check(
                MixedSizes(Vector128.Create(1), Vector256.Create(10), 5),
                Vector128.Create(16), "MixedSizes_128_256");
        }
        else
        {
            Console.WriteLine("  Skipping multi-size mixing tests because AVX is not supported.");
        }

        // --- Return into struct field tests ---
        Console.WriteLine("=== Return into struct tests ===");

        var pair = ReturnPair128(Vector128.Create(10), Vector128.Create(20));
        pass &= Check(pair.Lo, Vector128.Create(11), "ReturnPair128.Lo");
        pass &= Check(pair.Hi, Vector128.Create(22), "ReturnPair128.Hi");

        Console.WriteLine(pass ? "PASS" : "FAIL");
        return pass ? PASS : FAIL;
    }
}
