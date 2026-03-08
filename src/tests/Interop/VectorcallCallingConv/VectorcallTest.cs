// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class VectorcallTest
{
    public static bool IsWindowsX64 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X64;
    public static bool IsWindowsX86 => OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86;
    public static bool IsWindowsXArch => IsWindowsX64 || IsWindowsX86;

    [Fact]
    public static void TestIntegerArgs()
    {
        Console.WriteLine($"Running {nameof(TestIntegerArgs)}...");

        const int a = 11;
        const int expected = a * 2;

        int b;
        int result = VectorcallPInvokes.Double_Vectorcall(a, &b);
        Assert.Equal(expected, b);
        Assert.Equal(expected, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestFloatArgs()
    {
        Console.WriteLine($"Running {nameof(TestFloatArgs)}...");

        float result = VectorcallPInvokes.AddFloats_Vectorcall(1.5f, 2.5f);
        Assert.Equal(4.0f, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestDoubleArgs()
    {
        Console.WriteLine($"Running {nameof(TestDoubleArgs)}...");

        double result = VectorcallPInvokes.AddDoubles_Vectorcall(1.5, 2.5);
        Assert.Equal(4.0, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestMixedIntFloat()
    {
        Console.WriteLine($"Running {nameof(TestMixedIntFloat)}...");

        // a=1, b=2.0f, c=3, d=4.0 => 1 + 2 + 3 + 4 = 10.0
        double result = VectorcallPInvokes.MixedIntFloat_Vectorcall(1, 2.0f, 3, 4.0);
        Assert.Equal(10.0, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestSixFloats()
    {
        Console.WriteLine($"Running {nameof(TestSixFloats)}...");

        // 1 + 2 + 3 + 4 + 5 + 6 = 21
        float result = VectorcallPInvokes.SixFloats_Vectorcall(1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f);
        Assert.Equal(21.0f, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestSixDoubles()
    {
        Console.WriteLine($"Running {nameof(TestSixDoubles)}...");

        // 1 + 2 + 3 + 4 + 5 + 6 = 21
        double result = VectorcallPInvokes.SixDoubles_Vectorcall(1.0, 2.0, 3.0, 4.0, 5.0, 6.0);
        Assert.Equal(21.0, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestReturnFloat()
    {
        Console.WriteLine($"Running {nameof(TestReturnFloat)}...");

        float result = VectorcallPInvokes.ReturnFloat_Vectorcall(42);
        Assert.Equal(42.0f, result);

        Console.WriteLine($"  PASSED");
    }

    [Fact]
    public static void TestReturnDouble()
    {
        Console.WriteLine($"Running {nameof(TestReturnDouble)}...");

        double result = VectorcallPInvokes.ReturnDouble_Vectorcall(42);
        Assert.Equal(42.0, result);

        Console.WriteLine($"  PASSED");
    }

    // Managed callback with vectorcall convention - tests reverse P/Invoke
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvVectorcall)])]
    public static double ManagedMixedCallback(int a, float b, int c, double d)
    {
        return (double)a + (double)b + (double)c + d;
    }

    // Test that native code can call managed code using vectorcall convention
    [Fact]
    public static void TestReverseCallback()
    {
        Console.WriteLine($"Running {nameof(TestReverseCallback)}...");

        // Get function pointer to our managed callback
        delegate* unmanaged[Vectorcall]<int, float, int, double, double> callback = &ManagedMixedCallback;

        // Native code calls our managed callback with vectorcall ABI
        // The callback receives (1, 2.0f, 3, 4.0) and should return 1+2+3+4 = 10.0
        double result = VectorcallPInvokes.TestCallback_Vectorcall(callback);

        Assert.Equal(10.0, result);

        Console.WriteLine($"  PASSED");
    }

    // Note: Vector128 tests are in VectorcallVector128Test.cs which uses function pointers
    // with a non-generic Vec128 struct wrapper. Direct P/Invoke with Vector128<T> is not
    // supported because Vector128<T> is a generic type.

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static int TestEntryPoint()
    {
        try
        {
            TestIntegerArgs();
            TestFloatArgs();
            TestDoubleArgs();
            TestMixedIntFloat();
            TestSixFloats();
            TestSixDoubles();
            TestReturnFloat();
            TestReturnDouble();
            TestReverseCallback();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Test Failure: {e}");
            return 101;
        }

        return 100;
    }
}
