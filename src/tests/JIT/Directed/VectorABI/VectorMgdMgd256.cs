// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Xunit;

// Test passing and returning HVA (homogeneous vector aggregate) structs containing
// Vector256 and Vector512 elements.
//
// On System V x64:
//   - A single Vector256/512 is passed in a single YMM/ZMM register via handleAsSingleSimd.
//   - HVA structs containing multiple vectors are larger than 16 bytes and are passed on the stack
//     per the System V ABI (structs > 2 eightbytes go on the stack).
//   - This test verifies that both single-vector and multi-vector HVAs are handled correctly,
//     and that values are not corrupted during argument/return value passing.

public static class VectorMgdMgd256
{
    private const int PASS = 100;
    private const int FAIL = 0;

    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    static Random random = new Random(Seed);

    static bool isPassing = true;

    static void Check(string msg, bool condition)
    {
        if (!condition)
        {
            Console.WriteLine($"FAIL: {msg}");
            isPassing = false;
        }
    }

    // ======== HVA structs with Vector256 ========

    public struct HVA256_01 { public Vector256<int> v0; }
    public struct HVA256_02 { public Vector256<int> v0; public Vector256<int> v1; }
    public struct HVA256_03 { public Vector256<int> v0; public Vector256<int> v1; public Vector256<int> v2; }

    // ======== HVA structs with Vector512 ========

    public struct HVA512_01 { public Vector512<int> v0; }
    public struct HVA512_02 { public Vector512<int> v0; public Vector512<int> v1; }

    // ======== Single Vector256/512 argument tests ========

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> PassSingle256(Vector256<int> a) => a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> Add256(Vector256<int> a, Vector256<int> b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> PassMany256(Vector256<int> a, Vector256<int> b, Vector256<int> c, Vector256<int> d)
        => a + b + c + d;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector256<int> Mixed256(int x, Vector256<int> v, long y)
        => v + Vector256.Create(x + (int)y);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> PassSingle512(Vector512<int> a) => a;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> Add512(Vector512<int> a, Vector512<int> b) => a + b;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> Mixed512(int x, Vector512<int> v, long y)
        => v + Vector512.Create(x + (int)y);

    // ======== HVA argument tests (passed on stack on SysV x64) ========

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA256_01 PassHVA256_01(HVA256_01 h) => h;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA256_02 PassHVA256_02(HVA256_02 h) => h;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA256_03 PassHVA256_03(HVA256_03 h) => h;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA512_01 PassHVA512_01(HVA512_01 h) => h;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA512_02 PassHVA512_02(HVA512_02 h) => h;

    // ======== HVA return tests ========

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA256_01 ReturnHVA256_01(Vector256<int> v0) => new HVA256_01 { v0 = v0 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA256_02 ReturnHVA256_02(Vector256<int> v0, Vector256<int> v1) => new HVA256_02 { v0 = v0, v1 = v1 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    static HVA512_01 ReturnHVA512_01(Vector512<int> v0) => new HVA512_01 { v0 = v0 };

    // ======== Mixed HVA + scalar argument tests ========

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int HVA256WithScalars(int a, HVA256_01 h, int b)
        => a + b + h.v0.GetElement(0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int HVA512WithScalars(long a, HVA512_01 h, long b)
        => (int)(a + b) + h.v0.GetElement(0);

    // ======== Reflection tests (forces real calling convention) ========

    static void TestReflection256()
    {
        var v = Vector256.Create(42);
        var h01 = new HVA256_01 { v0 = v };

        var method = typeof(VectorMgdMgd256).GetMethod(nameof(PassHVA256_01),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (HVA256_01)method.Invoke(null, new object[] { h01 });
        Check("Reflection PassHVA256_01.v0", result.v0 == v);

        var h02 = new HVA256_02 { v0 = Vector256.Create(10), v1 = Vector256.Create(20) };
        var method2 = typeof(VectorMgdMgd256).GetMethod(nameof(PassHVA256_02),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result2 = (HVA256_02)method2.Invoke(null, new object[] { h02 });
        Check("Reflection PassHVA256_02.v0", result2.v0 == Vector256.Create(10));
        Check("Reflection PassHVA256_02.v1", result2.v1 == Vector256.Create(20));
    }

    static void TestReflection512()
    {
        var v = Vector512.Create(42);
        var h01 = new HVA512_01 { v0 = v };

        var method = typeof(VectorMgdMgd256).GetMethod(nameof(PassHVA512_01),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var result = (HVA512_01)method.Invoke(null, new object[] { h01 });
        Check("Reflection PassHVA512_01.v0", result.v0 == v);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine($"Vector256<int>.Count = {Vector256<int>.Count}");
        Console.WriteLine($"Vector512<int>.Count = {Vector512<int>.Count}");

        if (Avx.IsSupported)
        {
            // ---- Single Vector256 tests ----
            Console.WriteLine("=== Single Vector256 tests ===");

            var v256 = Vector256.Create(1, 2, 3, 4, 5, 6, 7, 8);
            Check("PassSingle256", PassSingle256(v256) == v256);

            Check("Add256", Add256(Vector256.Create(1), Vector256.Create(2)) == Vector256.Create(3));

            Check("PassMany256", PassMany256(
                Vector256.Create(1), Vector256.Create(2), Vector256.Create(3), Vector256.Create(4)) == Vector256.Create(10));

            Check("Mixed256", Mixed256(3, Vector256.Create(10), 7L) == Vector256.Create(20));

            // ---- HVA256 argument tests ----
            Console.WriteLine("=== HVA256 argument tests ===");

            var hva256_01 = new HVA256_01 { v0 = Vector256.Create(random.Next(100)) };
            var r01 = PassHVA256_01(hva256_01);
            Check("PassHVA256_01.v0", r01.v0 == hva256_01.v0);

            var hva256_02 = new HVA256_02 { v0 = Vector256.Create(random.Next(100)), v1 = Vector256.Create(random.Next(100)) };
            var r02 = PassHVA256_02(hva256_02);
            Check("PassHVA256_02.v0", r02.v0 == hva256_02.v0);
            Check("PassHVA256_02.v1", r02.v1 == hva256_02.v1);

            var hva256_03 = new HVA256_03
            {
                v0 = Vector256.Create(random.Next(100)),
                v1 = Vector256.Create(random.Next(100)),
                v2 = Vector256.Create(random.Next(100))
            };
            var r03 = PassHVA256_03(hva256_03);
            Check("PassHVA256_03.v0", r03.v0 == hva256_03.v0);
            Check("PassHVA256_03.v1", r03.v1 == hva256_03.v1);
            Check("PassHVA256_03.v2", r03.v2 == hva256_03.v2);

            // ---- HVA256 return tests ----
            Console.WriteLine("=== HVA return tests ===");

            var retH01 = ReturnHVA256_01(Vector256.Create(77));
            Check("ReturnHVA256_01.v0", retH01.v0 == Vector256.Create(77));

            var retH02 = ReturnHVA256_02(Vector256.Create(10), Vector256.Create(20));
            Check("ReturnHVA256_02.v0", retH02.v0 == Vector256.Create(10));
            Check("ReturnHVA256_02.v1", retH02.v1 == Vector256.Create(20));

            // ---- Mixed scalar + HVA256 tests ----
            Console.WriteLine("=== Mixed scalar + HVA tests ===");

            var hMixed = new HVA256_01 { v0 = Vector256.Create(100) };
            Check("HVA256WithScalars", HVA256WithScalars(1, hMixed, 2) == 103);

            // ---- Reflection256 tests ----
            Console.WriteLine("=== Reflection tests ===");

            TestReflection256();
        }
        else
        {
            Console.WriteLine("=== Skipping Vector256 tests: AVX not supported ===");
        }

        if (Avx512F.IsSupported)
        {
            // ---- Single Vector512 tests ----
            Console.WriteLine("=== Single Vector512 tests ===");

            var v512 = Vector512.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            Check("PassSingle512", PassSingle512(v512) == v512);

            Check("Add512", Add512(Vector512.Create(1), Vector512.Create(2)) == Vector512.Create(3));

            Check("Mixed512", Mixed512(3, Vector512.Create(10), 7L) == Vector512.Create(20));

            // ---- HVA512 argument tests ----
            Console.WriteLine("=== HVA512 argument tests ===");

            var hva512_01 = new HVA512_01 { v0 = Vector512.Create(random.Next(100)) };
            var r512_01 = PassHVA512_01(hva512_01);
            Check("PassHVA512_01.v0", r512_01.v0 == hva512_01.v0);

            var hva512_02 = new HVA512_02 { v0 = Vector512.Create(random.Next(100)), v1 = Vector512.Create(random.Next(100)) };
            var r512_02 = PassHVA512_02(hva512_02);
            Check("PassHVA512_02.v0", r512_02.v0 == hva512_02.v0);
            Check("PassHVA512_02.v1", r512_02.v1 == hva512_02.v1);

            // ---- HVA512 return tests ----
            var retH512 = ReturnHVA512_01(Vector512.Create(99));
            Check("ReturnHVA512_01.v0", retH512.v0 == Vector512.Create(99));

            // ---- Mixed scalar + HVA512 tests ----
            var hMixed512 = new HVA512_01 { v0 = Vector512.Create(200) };
            Check("HVA512WithScalars", HVA512WithScalars(1L, hMixed512, 2L) == 203);

            // ---- Reflection512 tests ----
            TestReflection512();
        }
        else
        {
            Console.WriteLine("=== Skipping Vector512 tests: AVX-512 not supported ===");
        }

        Console.WriteLine(isPassing ? "Test Passed" : "Test FAILED");

        return isPassing ? PASS : FAIL;
    }
}
