// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// There was an issue with register->stack copy for multi-reg return of a struct with small types
// on arm/arm64.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;

public struct S
{
    public object o;
    public byte b;
}

public class Runtime_46240
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void TestS(S s)
    {
        return;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S GetS()
    {
        return new S();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestS()
    {
        // Use all available registers to push some locals on stack
        // and force multi-reg->stack copies with small types
        // that require different encoding on arm32/arm64.
        S s1 = GetS();
        S s2 = GetS();
        S s3 = GetS();
        S s4 = GetS();
        S s5 = GetS();
        S s6 = GetS();
        S s7 = GetS();
        S s8 = GetS();
        S s9 = GetS();
        S s10 = GetS();

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

    public static int Main(string[] args)
    {
        if (TestS() != 100)
        {
            return 101;
        }
        return 100;
    }
}