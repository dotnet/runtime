// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

// Disable runtime marshalling to allow blittable types to be passed directly
[assembly: DisableRuntimeMarshalling]

/// <summary>
/// Tests for vectorcall calling convention with SIMD vector types.
///
/// Uses System.Numerics.Vector4 (non-generic, 16 bytes, same layout as __m128)
/// in function pointer signatures. The JIT recognizes Vector4 as an intrinsic SIMD
/// type and passes/returns it in XMM registers under vectorcall, matching the native
/// __m128 ABI.
///
/// Note: Vector128&lt;T&gt; (generic) cannot be used in unmanaged function pointer
/// signatures due to the runtime's generic marshalling restriction.
/// </summary>
public unsafe class VectorcallVector128Test
{
    private const string NativeLib = "VectorcallNative";

    public static bool IsWindowsX64 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;
    public static bool IsWindowsX86 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86;
    public static bool IsWindowsXArch => IsWindowsX64 || IsWindowsX86;
    public static bool IsWindowsX64WithAvx => IsWindowsX64 && Avx.IsSupported;

    // HVA (Homogeneous Vector Aggregate) structs with Vector4 fields.
    // The JIT detects Vector4 fields as SIMD-compatible for vectorcall HVA classification.
    [StructLayout(LayoutKind.Sequential)]
    public struct HVA2
    {
        public Vector4 V0;
        public Vector4 V1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HVA3
    {
        public Vector4 V0;
        public Vector4 V1;
        public Vector4 V2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HVA4
    {
        public Vector4 V0;
        public Vector4 V1;
        public Vector4 V2;
        public Vector4 V3;
    }

    // ========================================================================
    // Vector4 tests — Vector4 is 16 bytes (same as __m128), passed in XMM
    // ========================================================================

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestAddVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "AddVector128_Vectorcall");

        var a = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var b = new Vector4(10.0f, 20.0f, 30.0f, 40.0f);
        Vector4 result = fn(a, b);

        Assert.Equal(11.0f, result.X);
        Assert.Equal(22.0f, result.Y);
        Assert.Equal(33.0f, result.Z);
        Assert.Equal(44.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMulVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "MulVector128_Vectorcall");

        var a = new Vector4(2.0f, 3.0f, 4.0f, 5.0f);
        var b = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);
        Vector4 result = fn(a, b);

        Assert.Equal(20.0f, result.X);
        Assert.Equal(30.0f, result.Y);
        Assert.Equal(40.0f, result.Z);
        Assert.Equal(50.0f, result.W);
        NativeLibrary.Free(lib);
    }

    // ========================================================================
    // Vector4 edge cases
    // ========================================================================

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestIdentityVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "IdentityVector128_Vectorcall");

        var input = new Vector4(42.0f, -1.0f, 0.0f, float.MaxValue);
        Vector4 result = fn(input);

        Assert.Equal(42.0f, result.X);
        Assert.Equal(-1.0f, result.Y);
        Assert.Equal(0.0f, result.Z);
        Assert.Equal(float.MaxValue, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestNegateVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "NegateVector128_Vectorcall");

        var input = new Vector4(1.0f, -2.0f, 3.0f, -4.0f);
        Vector4 result = fn(input);

        Assert.Equal(-1.0f, result.X);
        Assert.Equal(2.0f, result.Y);
        Assert.Equal(-3.0f, result.Z);
        Assert.Equal(4.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHsumVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, float>)
            NativeLibrary.GetExport(lib, "HsumVector128_Vectorcall");

        var input = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        float result = fn(input);

        Assert.Equal(10.0f, result);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestConstReturnVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4>)
            NativeLibrary.GetExport(lib, "ConstVector128_Vectorcall");

        Vector4 result = fn();

        Assert.Equal(1.0f, result.X);
        Assert.Equal(2.0f, result.Y);
        Assert.Equal(3.0f, result.Z);
        Assert.Equal(4.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestScaleVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<float, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "ScaleVector128_Vectorcall");

        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        Vector4 result = fn(10.0f, v);

        Assert.Equal(10.0f, result.X);
        Assert.Equal(20.0f, result.Y);
        Assert.Equal(30.0f, result.Z);
        Assert.Equal(40.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestFmaVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "FmaVector128_Vectorcall");

        var a = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var b = new Vector4(9.0f, 8.0f, 7.0f, 6.0f);
        var c = new Vector4(2.0f, 2.0f, 2.0f, 2.0f);
        // (a + b) * c = (10, 10, 10, 10) * (2, 2, 2, 2) = (20, 20, 20, 20)
        Vector4 result = fn(a, b, c);

        Assert.Equal(20.0f, result.X);
        Assert.Equal(20.0f, result.Y);
        Assert.Equal(20.0f, result.Z);
        Assert.Equal(20.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMixedIntVector128()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<int, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "MixedIntVector128_Vectorcall");

        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        Vector4 result = fn(10, v);

        Assert.Equal(11.0f, result.X);
        Assert.Equal(12.0f, result.Y);
        Assert.Equal(13.0f, result.Z);
        Assert.Equal(14.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestSixVector128s()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4, Vector4,
            Vector4, Vector4, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "SixVector128s_Vectorcall");

        Vector4 result = fn(
            new Vector4(1.0f), new Vector4(2.0f), new Vector4(3.0f),
            new Vector4(4.0f), new Vector4(5.0f), new Vector4(6.0f));

        Assert.Equal(21.0f, result.X);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestManyIntsOneVector()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<int, int, int, int, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "ManyIntsOneVector_Vectorcall");

        var v = new Vector4(100.0f, 200.0f, 300.0f, 400.0f);
        // a+b+c+d = 1+2+3+4 = 10, added to each element
        Vector4 result = fn(1, 2, 3, 4, v);

        Assert.Equal(110.0f, result.X);
        Assert.Equal(210.0f, result.Y);
        Assert.Equal(310.0f, result.Z);
        Assert.Equal(410.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestChainedVectorcallResults()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var addFn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "AddVector128_Vectorcall");
        var negateFn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "NegateVector128_Vectorcall");

        var a = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var b = new Vector4(10.0f, 20.0f, 30.0f, 40.0f);

        // Chain: negate(add(a, b)) = -(11, 22, 33, 44) = (-11, -22, -33, -44)
        Vector4 result = negateFn(addFn(a, b));

        Assert.Equal(-11.0f, result.X);
        Assert.Equal(-22.0f, result.Y);
        Assert.Equal(-33.0f, result.Z);
        Assert.Equal(-44.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestSpecialFloatValues()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<Vector4, Vector4>)
            NativeLibrary.GetExport(lib, "IdentityVector128_Vectorcall");

        var input = new Vector4(float.NaN, float.PositiveInfinity, float.NegativeInfinity, float.Epsilon);
        Vector4 result = fn(input);

        Assert.True(float.IsNaN(result.X));
        Assert.Equal(float.PositiveInfinity, result.Y);
        Assert.Equal(float.NegativeInfinity, result.Z);
        Assert.Equal(float.Epsilon, result.W);
        NativeLibrary.Free(lib);
    }

    // ========================================================================
    // HVA (Homogeneous Vector Aggregate) tests — structs with Vector4 fields
    // ========================================================================

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA2()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<HVA2, HVA2, HVA2>)
            NativeLibrary.GetExport(lib, "AddHVA2_Vectorcall");

        var a = new HVA2
        {
            V0 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vector4(5.0f, 6.0f, 7.0f, 8.0f)
        };
        var b = new HVA2
        {
            V0 = new Vector4(10.0f, 20.0f, 30.0f, 40.0f),
            V1 = new Vector4(50.0f, 60.0f, 70.0f, 80.0f)
        };

        HVA2 result = fn(a, b);

        Assert.Equal(11.0f, result.V0.X);
        Assert.Equal(22.0f, result.V0.Y);
        Assert.Equal(33.0f, result.V0.Z);
        Assert.Equal(44.0f, result.V0.W);
        Assert.Equal(55.0f, result.V1.X);
        Assert.Equal(66.0f, result.V1.Y);
        Assert.Equal(77.0f, result.V1.Z);
        Assert.Equal(88.0f, result.V1.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA3()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<HVA3, HVA3>)
            NativeLibrary.GetExport(lib, "AddHVA3_Vectorcall");

        var a = new HVA3
        {
            V0 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vector4(10.0f, 20.0f, 30.0f, 40.0f),
            V2 = new Vector4(100.0f, 200.0f, 300.0f, 400.0f)
        };

        HVA3 result = fn(a);

        Assert.Equal(2.0f, result.V0.X);
        Assert.Equal(11.0f, result.V1.X);
        Assert.Equal(101.0f, result.V2.X);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA4()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<HVA4, HVA4>)
            NativeLibrary.GetExport(lib, "AddHVA4_Vectorcall");

        var a = new HVA4
        {
            V0 = new Vector4(1.0f), V1 = new Vector4(2.0f),
            V2 = new Vector4(3.0f), V3 = new Vector4(4.0f)
        };

        HVA4 result = fn(a);

        Assert.Equal(11.0f, result.V0.X);
        Assert.Equal(12.0f, result.V1.X);
        Assert.Equal(13.0f, result.V2.X);
        Assert.Equal(14.0f, result.V3.X);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMixedHVA2Int()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<int, HVA2, Vector4>)
            NativeLibrary.GetExport(lib, "MixedHVA2Int_Vectorcall");

        var hva = new HVA2
        {
            V0 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vector4(10.0f, 20.0f, 30.0f, 40.0f)
        };

        Vector4 result = fn(100, hva);

        Assert.Equal(111.0f, result.X);
        Assert.Equal(122.0f, result.Y);
        Assert.Equal(133.0f, result.Z);
        Assert.Equal(144.0f, result.W);
        NativeLibrary.Free(lib);
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestDiscontiguousHVA()
    {
        IntPtr lib = NativeLibrary.Load(NativeLib);
        var fn = (delegate* unmanaged[Vectorcall]<int, float, HVA4, Vector4, int, float>)
            NativeLibrary.GetExport(lib, "DiscontiguousHVA_Vectorcall");

        var hva4 = new HVA4
        {
            V0 = new Vector4(1.0f, 0, 0, 0),
            V1 = new Vector4(10.0f, 0, 0, 0),
            V2 = new Vector4(100.0f, 0, 0, 0),
            V3 = new Vector4(1000.0f, 0, 0, 0)
        };

        float result = fn(1, 2.0f, hva4, new Vector4(10000.0f, 0, 0, 0), 5);

        // 1 + 2 + 5 + 1 + 10 + 100 + 1000 + 10000 = 11119
        Assert.Equal(11119.0f, result);
        NativeLibrary.Free(lib);
    }

    // ========================================================================
    // TestEntryPoint — runs all tests
    // ========================================================================

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        int failures = 0;

        if (IsWindowsX64)
        {
            // Vector4 basic tests (16 bytes, same as __m128)
            try { TestAddVector128(); } catch (Exception e) { Console.WriteLine($"TestAddVector128 FAILED: {e.Message}"); failures++; }
            try { TestMulVector128(); } catch (Exception e) { Console.WriteLine($"TestMulVector128 FAILED: {e.Message}"); failures++; }
            try { TestMixedIntVector128(); } catch (Exception e) { Console.WriteLine($"TestMixedIntVector128 FAILED: {e.Message}"); failures++; }
            try { TestSixVector128s(); } catch (Exception e) { Console.WriteLine($"TestSixVector128s FAILED: {e.Message}"); failures++; }

            // Vector4 edge cases
            try { TestIdentityVector128(); } catch (Exception e) { Console.WriteLine($"TestIdentityVector128 FAILED: {e.Message}"); failures++; }
            try { TestNegateVector128(); } catch (Exception e) { Console.WriteLine($"TestNegateVector128 FAILED: {e.Message}"); failures++; }
            try { TestHsumVector128(); } catch (Exception e) { Console.WriteLine($"TestHsumVector128 FAILED: {e.Message}"); failures++; }
            try { TestConstReturnVector128(); } catch (Exception e) { Console.WriteLine($"TestConstReturnVector128 FAILED: {e.Message}"); failures++; }
            try { TestScaleVector128(); } catch (Exception e) { Console.WriteLine($"TestScaleVector128 FAILED: {e.Message}"); failures++; }
            try { TestFmaVector128(); } catch (Exception e) { Console.WriteLine($"TestFmaVector128 FAILED: {e.Message}"); failures++; }
            try { TestManyIntsOneVector(); } catch (Exception e) { Console.WriteLine($"TestManyIntsOneVector FAILED: {e.Message}"); failures++; }
            try { TestChainedVectorcallResults(); } catch (Exception e) { Console.WriteLine($"TestChainedVectorcallResults FAILED: {e.Message}"); failures++; }
            try { TestSpecialFloatValues(); } catch (Exception e) { Console.WriteLine($"TestSpecialFloatValues FAILED: {e.Message}"); failures++; }

            // HVA tests
            try { TestHVA2(); } catch (Exception e) { Console.WriteLine($"TestHVA2 FAILED: {e.Message}"); failures++; }
            try { TestHVA3(); } catch (Exception e) { Console.WriteLine($"TestHVA3 FAILED: {e.Message}"); failures++; }
            try { TestHVA4(); } catch (Exception e) { Console.WriteLine($"TestHVA4 FAILED: {e.Message}"); failures++; }
            try { TestMixedHVA2Int(); } catch (Exception e) { Console.WriteLine($"TestMixedHVA2Int FAILED: {e.Message}"); failures++; }
            try { TestDiscontiguousHVA(); } catch (Exception e) { Console.WriteLine($"TestDiscontiguousHVA FAILED: {e.Message}"); failures++; }
        }
        else
        {
            Console.WriteLine("Vectorcall SIMD tests skipped - not running on Windows x64");
        }

        return failures == 0 ? 100 : 101;
    }
}
