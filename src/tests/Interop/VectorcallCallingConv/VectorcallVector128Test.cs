// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using Xunit;

// Disable runtime marshalling to allow blittable types to be passed directly
[assembly: DisableRuntimeMarshalling]

/// <summary>
/// Vector128 tests for vectorcall calling convention.
///
/// The vectorcall ABI specifies that __m128 values should be passed in XMM registers.
/// The VectorcallX64Classifier in the JIT now recognizes SIMD types and passes them
/// in XMM registers instead of by reference.
/// </summary>
public unsafe class VectorcallVector128Test
{
    private const string NativeLib = "VectorcallNative";

    public static bool IsWindowsX64 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;
    public static bool IsWindowsX86 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86;
    public static bool IsWindowsXArch => IsWindowsX64 || IsWindowsX86;

    // Non-generic struct that matches Vector128<float> in memory layout
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    public struct Vec128
    {
        public float X, Y, Z, W;

        public Vec128(float x, float y, float z, float w)
        {
            X = x; Y = y; Z = z; W = w;
        }

        public static implicit operator Vec128(Vector128<float> v) => Unsafe.As<Vector128<float>, Vec128>(ref v);
        public static implicit operator Vector128<float>(Vec128 v) => Unsafe.As<Vec128, Vector128<float>>(ref v);
    }

    // Non-generic struct that matches Vector256<float> in memory layout (32 bytes)
    [StructLayout(LayoutKind.Sequential, Size = 32)]
    public struct Vec256
    {
        public float E0, E1, E2, E3, E4, E5, E6, E7;

        public Vec256(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
        {
            E0 = e0; E1 = e1; E2 = e2; E3 = e3;
            E4 = e4; E5 = e5; E6 = e6; E7 = e7;
        }

        public static implicit operator Vec256(Vector256<float> v) => Unsafe.As<Vector256<float>, Vec256>(ref v);
        public static implicit operator Vector256<float>(Vec256 v) => Unsafe.As<Vec256, Vector256<float>>(ref v);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HVA2_256
    {
        public Vec256 V0;
        public Vec256 V1;
    }

    // Simple test with direct struct creation (no conversion)
    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestSimpleVector128()
    {
        Console.WriteLine($"Running {nameof(TestSimpleVector128)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddVector128_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<Vec128, Vec128, Vec128>)funcPtr;

        // Create Vec128 directly without conversion
        var a = new Vec128(1.0f, 2.0f, 3.0f, 4.0f);
        var b = new Vec128(10.0f, 20.0f, 30.0f, 40.0f);

        Vec128 result = addFunc(a, b);

        Assert.Equal(11.0f, result.X);
        Assert.Equal(22.0f, result.Y);
        Assert.Equal(33.0f, result.Z);
        Assert.Equal(44.0f, result.W);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestAddVector128()
    {
        Console.WriteLine($"Running {nameof(TestAddVector128)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddVector128_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<Vec128, Vec128, Vec128>)funcPtr;

        var a = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        var b = Vector128.Create(10.0f, 20.0f, 30.0f, 40.0f);

        Vector128<float> result = addFunc((Vec128)a, (Vec128)b);

        Assert.Equal(11.0f, result[0]);
        Assert.Equal(22.0f, result[1]);
        Assert.Equal(33.0f, result[2]);
        Assert.Equal(44.0f, result[3]);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMulVector128()
    {
        Console.WriteLine($"Running {nameof(TestMulVector128)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "MulVector128_Vectorcall");

        var mulFunc = (delegate* unmanaged[Vectorcall]<Vec128, Vec128, Vec128>)funcPtr;

        var a = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);
        var b = Vector128.Create(10.0f, 20.0f, 30.0f, 40.0f);

        Vector128<float> result = mulFunc((Vec128)a, (Vec128)b);

        // 1*10=10, 2*20=40, 3*30=90, 4*40=160
        Assert.Equal(10.0f, result[0]);
        Assert.Equal(40.0f, result[1]);
        Assert.Equal(90.0f, result[2]);
        Assert.Equal(160.0f, result[3]);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMixedIntVector128()
    {
        Console.WriteLine($"Running {nameof(TestMixedIntVector128)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "MixedIntVector128_Vectorcall");

        var mixedFunc = (delegate* unmanaged[Vectorcall]<int, Vec128, Vec128>)funcPtr;

        var vec = Vector128.Create(1.0f, 2.0f, 3.0f, 4.0f);

        Vector128<float> result = mixedFunc(10, (Vec128)vec);

        Assert.Equal(11.0f, result[0]);
        Assert.Equal(12.0f, result[1]);
        Assert.Equal(13.0f, result[2]);
        Assert.Equal(14.0f, result[3]);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestSixVector128s()
    {
        Console.WriteLine($"Running {nameof(TestSixVector128s)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "SixVector128s_Vectorcall");

        var sixFunc = (delegate* unmanaged[Vectorcall]<Vec128, Vec128, Vec128, Vec128, Vec128, Vec128, Vec128>)funcPtr;

        var a = Vector128.Create(1.0f);
        var b = Vector128.Create(2.0f);
        var c = Vector128.Create(3.0f);
        var d = Vector128.Create(4.0f);
        var e = Vector128.Create(5.0f);
        var f = Vector128.Create(6.0f);

        Vector128<float> result = sixFunc((Vec128)a, (Vec128)b, (Vec128)c, (Vec128)d, (Vec128)e, (Vec128)f);

        Assert.Equal(21.0f, result[0]);
        Assert.Equal(21.0f, result[1]);
        Assert.Equal(21.0f, result[2]);
        Assert.Equal(21.0f, result[3]);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    // HVA (Homogeneous Vector Aggregate) test structs
    [StructLayout(LayoutKind.Sequential)]
    public struct HVA2
    {
        public Vec128 V0;
        public Vec128 V1;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HVA3
    {
        public Vec128 V0;
        public Vec128 V1;
        public Vec128 V2;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HVA4
    {
        public Vec128 V0;
        public Vec128 V1;
        public Vec128 V2;
        public Vec128 V3;
    }

    // HVA (Homogeneous Vector Aggregate) tests
    // Vectorcall HVA returns in XMM0-XMM3, supporting structs of 2-4 vectors.

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA2()
    {
        Console.WriteLine($"Running {nameof(TestHVA2)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddHVA2_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<HVA2, HVA2, HVA2>)funcPtr;

        var a = new HVA2
        {
            V0 = new Vec128(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vec128(5.0f, 6.0f, 7.0f, 8.0f)
        };
        var b = new HVA2
        {
            V0 = new Vec128(10.0f, 20.0f, 30.0f, 40.0f),
            V1 = new Vec128(50.0f, 60.0f, 70.0f, 80.0f)
        };

        HVA2 result = addFunc(a, b);

        // V0: (1+10, 2+20, 3+30, 4+40) = (11, 22, 33, 44)
        Assert.Equal(11.0f, result.V0.X);
        Assert.Equal(22.0f, result.V0.Y);
        Assert.Equal(33.0f, result.V0.Z);
        Assert.Equal(44.0f, result.V0.W);

        // V1: (5+50, 6+60, 7+70, 8+80) = (55, 66, 77, 88)
        Assert.Equal(55.0f, result.V1.X);
        Assert.Equal(66.0f, result.V1.Y);
        Assert.Equal(77.0f, result.V1.Z);
        Assert.Equal(88.0f, result.V1.W);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMulHVA2()
    {
        Console.WriteLine($"Running {nameof(TestMulHVA2)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "MulHVA2_Vectorcall");

        var mulFunc = (delegate* unmanaged[Vectorcall]<HVA2, HVA2, HVA2>)funcPtr;

        var a = new HVA2
        {
            V0 = new Vec128(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vec128(5.0f, 6.0f, 7.0f, 8.0f)
        };
        var b = new HVA2
        {
            V0 = new Vec128(10.0f, 10.0f, 10.0f, 10.0f),
            V1 = new Vec128(2.0f, 2.0f, 2.0f, 2.0f)
        };

        HVA2 result = mulFunc(a, b);

        // V0: (1*10, 2*10, 3*10, 4*10) = (10, 20, 30, 40)
        Assert.Equal(10.0f, result.V0.X);
        Assert.Equal(20.0f, result.V0.Y);
        Assert.Equal(30.0f, result.V0.Z);
        Assert.Equal(40.0f, result.V0.W);

        // V1: (5*2, 6*2, 7*2, 8*2) = (10, 12, 14, 16)
        Assert.Equal(10.0f, result.V1.X);
        Assert.Equal(12.0f, result.V1.Y);
        Assert.Equal(14.0f, result.V1.Z);
        Assert.Equal(16.0f, result.V1.W);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA3()
    {
        Console.WriteLine($"Running {nameof(TestHVA3)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddHVA3_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<HVA3, HVA3>)funcPtr;

        var a = new HVA3
        {
            V0 = new Vec128(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vec128(10.0f, 20.0f, 30.0f, 40.0f),
            V2 = new Vec128(100.0f, 200.0f, 300.0f, 400.0f)
        };

        HVA3 result = addFunc(a);

        // AddHVA3_Vectorcall adds 1.0f to each element
        // V0: (1+1, 2+1, 3+1, 4+1) = (2, 3, 4, 5)
        Assert.Equal(2.0f, result.V0.X);
        Assert.Equal(3.0f, result.V0.Y);
        Assert.Equal(4.0f, result.V0.Z);
        Assert.Equal(5.0f, result.V0.W);

        // V1: (10+1, 20+1, 30+1, 40+1) = (11, 21, 31, 41)
        Assert.Equal(11.0f, result.V1.X);
        Assert.Equal(21.0f, result.V1.Y);
        Assert.Equal(31.0f, result.V1.Z);
        Assert.Equal(41.0f, result.V1.W);

        // V2: (100+1, 200+1, 300+1, 400+1) = (101, 201, 301, 401)
        Assert.Equal(101.0f, result.V2.X);
        Assert.Equal(201.0f, result.V2.Y);
        Assert.Equal(301.0f, result.V2.Z);
        Assert.Equal(401.0f, result.V2.W);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestHVA4()
    {
        Console.WriteLine($"Running {nameof(TestHVA4)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddHVA4_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<HVA4, HVA4>)funcPtr;

        var a = new HVA4
        {
            V0 = new Vec128(1.0f, 1.0f, 1.0f, 1.0f),
            V1 = new Vec128(2.0f, 2.0f, 2.0f, 2.0f),
            V2 = new Vec128(3.0f, 3.0f, 3.0f, 3.0f),
            V3 = new Vec128(4.0f, 4.0f, 4.0f, 4.0f)
        };

        HVA4 result = addFunc(a);

        // AddHVA4_Vectorcall adds 10.0f to each element
        // V0: 1+10 = 11
        // V1: 2+10 = 12
        // V2: 3+10 = 13
        // V3: 4+10 = 14
        Assert.Equal(11.0f, result.V0.X);
        Assert.Equal(12.0f, result.V1.X);
        Assert.Equal(13.0f, result.V2.X);
        Assert.Equal(14.0f, result.V3.X);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestMixedHVA2Int()
    {
        // Test exercises mixed int + HVA argument passing.
        // Per vectorcall ABI: integers use positional allocation, HVAs use unused XMM registers.
        // The int goes to RCX (position 0), HVA goes to XMM0-1 (first unused registers).
        Console.WriteLine($"Running {nameof(TestMixedHVA2Int)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "MixedHVA2Int_Vectorcall");

        var mixedFunc = (delegate* unmanaged[Vectorcall]<int, HVA2, Vec128>)funcPtr;

        var hva = new HVA2
        {
            V0 = new Vec128(1.0f, 2.0f, 3.0f, 4.0f),
            V1 = new Vec128(10.0f, 20.0f, 30.0f, 40.0f)
        };

        Vec128 result = mixedFunc(100, hva);

        // Expected: result = (V0 + V1) + scalar = (1+10, 2+20, 3+30, 4+40) + 100 = (111, 122, 133, 144)
        Console.WriteLine($"  Result: X={result.X}, Y={result.Y}, Z={result.Z}, W={result.W}");

        Assert.Equal(111.0f, result.X);
        Assert.Equal(122.0f, result.Y);
        Assert.Equal(133.0f, result.Z);
        Assert.Equal(144.0f, result.W);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64))]
    public static void TestDiscontiguousHVA()
    {
        // Test exercises discontiguous HVA allocation (Example 4 from Microsoft docs).
        // Per vectorcall ABI: regular vectors use positional allocation, HVAs use unused registers.
        // DiscontiguousHVA_Vectorcall(int a, float b, HVA4 c, __m128 d, int e):
        // - a in RCX (position 0)
        // - b in XMM1 (position 1) - regular vector, positional
        // - d in XMM3 (position 3) - regular vector, positional  
        // - e on stack (position 4 > 3 for integers)
        // - c (HVA4) in [XMM0, XMM2, XMM4, XMM5] - discontiguous unused registers
        Console.WriteLine($"Running {nameof(TestDiscontiguousHVA)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "DiscontiguousHVA_Vectorcall");

        var discontFunc = (delegate* unmanaged[Vectorcall]<int, float, HVA4, Vec128, int, float>)funcPtr;

        var hva4 = new HVA4
        {
            V0 = new Vec128(1.0f, 0, 0, 0),
            V1 = new Vec128(10.0f, 0, 0, 0),
            V2 = new Vec128(100.0f, 0, 0, 0),
            V3 = new Vec128(1000.0f, 0, 0, 0)
        };

        var d = new Vec128(10000.0f, 0, 0, 0);

        float result = discontFunc(1, 2.0f, hva4, d, 5);

        // Expected: 1 + 2 + 5 + 1 + 10 + 100 + 1000 + 10000 = 11119
        Console.WriteLine($"  Result: {result}");

        Assert.Equal(11119.0f, result);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    // Vector256 tests - require AVX support
    // Field-type inspection now distinguishes:
    // - Vec256 (8 float fields) -> single YMM register
    // - HVA2 (2 Vec128 fields) -> 2 XMM registers
    public static bool IsWindowsX64WithAvx => IsWindowsX64 && System.Runtime.Intrinsics.X86.Avx.IsSupported;

    [ConditionalFact(nameof(IsWindowsX64WithAvx))]
    public static void TestAddVector256()
    {
        Console.WriteLine($"Running {nameof(TestAddVector256)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "AddVector256_Vectorcall");

        var addFunc = (delegate* unmanaged[Vectorcall]<Vec256, Vec256, Vec256>)funcPtr;

        var a = new Vec256(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);
        var b = new Vec256(10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f, 70.0f, 80.0f);

        Vec256 result = addFunc(a, b);

        Console.WriteLine($"  Result: E0={result.E0}, E1={result.E1}, E2={result.E2}, E3={result.E3}, E4={result.E4}, E5={result.E5}, E6={result.E6}, E7={result.E7}");

        Assert.Equal(11.0f, result.E0);
        Assert.Equal(22.0f, result.E1);
        Assert.Equal(33.0f, result.E2);
        Assert.Equal(44.0f, result.E3);
        Assert.Equal(55.0f, result.E4);
        Assert.Equal(66.0f, result.E5);
        Assert.Equal(77.0f, result.E6);
        Assert.Equal(88.0f, result.E7);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64WithAvx))]
    public static void TestMulVector256()
    {
        Console.WriteLine($"Running {nameof(TestMulVector256)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "MulVector256_Vectorcall");

        var mulFunc = (delegate* unmanaged[Vectorcall]<Vec256, Vec256, Vec256>)funcPtr;

        var a = new Vec256(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);
        var b = new Vec256(10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f);

        Vec256 result = mulFunc(a, b);

        Console.WriteLine($"  Result: E0={result.E0}, E1={result.E1}, E2={result.E2}, E3={result.E3}, E4={result.E4}, E5={result.E5}, E6={result.E6}, E7={result.E7}");

        // 1*10=10, 2*10=20, 3*10=30, 4*10=40, 5*10=50, 6*10=60, 7*10=70, 8*10=80
        Assert.Equal(10.0f, result.E0);
        Assert.Equal(20.0f, result.E1);
        Assert.Equal(30.0f, result.E2);
        Assert.Equal(40.0f, result.E3);
        Assert.Equal(50.0f, result.E4);
        Assert.Equal(60.0f, result.E5);
        Assert.Equal(70.0f, result.E6);
        Assert.Equal(80.0f, result.E7);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64WithAvx))]
    public static void TestVector256MixedInt()
    {
        Console.WriteLine($"Running {nameof(TestVector256MixedInt)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "Vector256MixedInt_Vectorcall");

        var mixedFunc = (delegate* unmanaged[Vectorcall]<int, Vec256, Vec256>)funcPtr;

        var v = new Vec256(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f);

        Vec256 result = mixedFunc(10, v);

        Console.WriteLine($"  Result: E0={result.E0}, E1={result.E1}, E2={result.E2}, E3={result.E3}, E4={result.E4}, E5={result.E5}, E6={result.E6}, E7={result.E7}");

        Assert.Equal(11.0f, result.E0);
        Assert.Equal(12.0f, result.E1);
        Assert.Equal(13.0f, result.E2);
        Assert.Equal(14.0f, result.E3);
        Assert.Equal(15.0f, result.E4);
        Assert.Equal(16.0f, result.E5);
        Assert.Equal(17.0f, result.E6);
        Assert.Equal(18.0f, result.E7);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [ConditionalFact(nameof(IsWindowsX64WithAvx))]
    public static void TestHVA2_256()
    {
        Console.WriteLine($"Running {nameof(TestHVA2_256)}...");

        IntPtr lib = NativeLibrary.Load(NativeLib);
        IntPtr funcPtr = NativeLibrary.GetExport(lib, "HVA2_256_Vectorcall");

        var func = (delegate* unmanaged[Vectorcall]<HVA2_256, Vec256>)funcPtr;

        var hva = new HVA2_256
        {
            V0 = new Vec256(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f),
            V1 = new Vec256(10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f, 70.0f, 80.0f),
        };

        Vec256 result = func(hva);

        Assert.Equal(11.0f, result.E0);
        Assert.Equal(22.0f, result.E1);
        Assert.Equal(33.0f, result.E2);
        Assert.Equal(44.0f, result.E3);
        Assert.Equal(55.0f, result.E4);
        Assert.Equal(66.0f, result.E5);
        Assert.Equal(77.0f, result.E6);
        Assert.Equal(88.0f, result.E7);

        NativeLibrary.Free(lib);
        Console.WriteLine($"  PASSED");
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        int failures = 0;

        if (IsWindowsX64)
        {
            try { TestSimpleVector128(); } catch (Exception e) { Console.WriteLine($"TestSimpleVector128 FAILED: {e.Message}"); failures++; }
            try { TestAddVector128(); } catch (Exception e) { Console.WriteLine($"TestAddVector128 FAILED: {e.Message}"); failures++; }
            try { TestMulVector128(); } catch (Exception e) { Console.WriteLine($"TestMulVector128 FAILED: {e.Message}"); failures++; }
            try { TestMixedIntVector128(); } catch (Exception e) { Console.WriteLine($"TestMixedIntVector128 FAILED: {e.Message}"); failures++; }
            try { TestSixVector128s(); } catch (Exception e) { Console.WriteLine($"TestSixVector128s FAILED: {e.Message}"); failures++; }
            try { TestHVA2(); } catch (Exception e) { Console.WriteLine($"TestHVA2 FAILED: {e.Message}"); failures++; }
            try { TestMulHVA2(); } catch (Exception e) { Console.WriteLine($"TestMulHVA2 FAILED: {e.Message}"); failures++; }
            try { TestHVA3(); } catch (Exception e) { Console.WriteLine($"TestHVA3 FAILED: {e.Message}"); failures++; }
            try { TestHVA4(); } catch (Exception e) { Console.WriteLine($"TestHVA4 FAILED: {e.Message}"); failures++; }
            try { TestMixedHVA2Int(); } catch (Exception e) { Console.WriteLine($"TestMixedHVA2Int FAILED: {e.Message}"); failures++; }
            try { TestDiscontiguousHVA(); } catch (Exception e) { Console.WriteLine($"TestDiscontiguousHVA FAILED: {e.Message}"); failures++; }
            // Vector256 tests - require AVX support
            if (System.Runtime.Intrinsics.X86.Avx.IsSupported)
            {
                try { TestAddVector256(); } catch (Exception e) { Console.WriteLine($"TestAddVector256 FAILED: {e.Message}"); failures++; }
                try { TestMulVector256(); } catch (Exception e) { Console.WriteLine($"TestMulVector256 FAILED: {e.Message}"); failures++; }
                try { TestVector256MixedInt(); } catch (Exception e) { Console.WriteLine($"TestVector256MixedInt FAILED: {e.Message}"); failures++; }
                try { TestHVA2_256(); } catch (Exception e) { Console.WriteLine($"TestHVA2_256 FAILED: {e.Message}"); failures++; }
            }
            else
            {
                Console.WriteLine("Vector256 tests skipped - AVX not supported");
            }
        }
        else
        {
            Console.WriteLine("Vector128 vectorcall tests skipped - not running on Windows x64");
        }

        return failures == 0 ? 100 : 101;
    }
}
