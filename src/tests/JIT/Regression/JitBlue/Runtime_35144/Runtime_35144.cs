// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test was the repro case for issue #35144.
// Until interop is supported for vectors, it is difficult to validate
// that the ABI is correctly implemented, but this test is here to enable
// these cases to be manually verified (and diffed).
//
using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

#pragma warning disable 0169 // warning CS0169: The field '{0}' is never used
struct WrappedVector64  { Vector64<byte> _; }
struct WrappedVector128 { Vector128<byte> _; }
struct WrappedVector256 { Vector256<byte> _; }

// Was incorrectly treated as non-HVA: passed in x0, x1.
// Should be recognized as HVA(SIMD8) and passed in d0, d1.
struct S1 { WrappedVector64 x; Vector64<byte> y; }

// Was incorrectly treated as HFA(double): passed in d0, d1.
// Should be passed as non-HFA in x0, x1.
struct S2 { WrappedVector64 x; double y; }

// Incorrectly treated as HVA(simd16): passed in q0, q1, q2.
// Should be passed by reference as non-HFA.
struct S3 { Vector128<byte> x; WrappedVector256 y; }

public static class Runtime_35144
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo<T>(T x, object o)
    {
        if (o.GetType() != typeof(string)) throw new Exception();
        if (((string)o) != "SomeString") throw new Exception();
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static void FooOpt<T>(T x, object o)
    {
        if (o.GetType() != typeof(string)) throw new Exception();
        if (((string)o) != "SomeString") throw new Exception();
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int returnVal = 100;
        try
        {
            Foo(new S1(), "SomeString");
            Foo(new S2(), "SomeString");
            Foo(new S3(), "SomeString");
            FooOpt(new S1(), "SomeString");
            FooOpt(new S2(), "SomeString");
            FooOpt(new S3(), "SomeString");
        }
        catch (Exception e)
        {
            Console.WriteLine("Unexpected exception " + e.Message);
            returnVal = -1;
        }
        return returnVal;
    }
}
