// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// There was an issue with register->stack copy for multi-reg return of a struct with small types
// on arm/arm64.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using Xunit;

public struct S<T>
{
    public object o;
    public T t;
}

public class Tester<T>
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestS(S<T> s)
    {
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S<T> GetS()
    {
        return new S<T>();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestS()
    {
        // Use all available registers to push some locals on stack
        // and force multi-reg->stack copies with small types
        // that require different encoding on arm32/arm64.
        S<T> s1 = GetS();
        S<T> s2 = GetS();
        S<T> s3 = GetS();
        S<T> s4 = GetS();
        S<T> s5 = GetS();
        S<T> s6 = GetS();
        S<T> s7 = GetS();
        S<T> s8 = GetS();
        S<T> s9 = GetS();
        S<T> s10 = GetS();

        // Keep all variable alive so we can't reuse registers.
        TestS(s10);
        TestS(s9);
        TestS(s8);
        TestS(s7);
        TestS(s6);
        TestS(s5);
        TestS(s4);
        TestS(s3);
        TestS(s2);
        TestS(s1);

        return 100;
    }
}

public class Runtime_46240
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (Tester<byte>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<sbyte>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<ushort>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<short>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<uint>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<int>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<ulong>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<long>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<float>.TestS() != 100)
        {
            return 101;
        }
        if (Tester<double>.TestS() != 100)
        {
            return 101;
        }
        return 100;
    }
}
