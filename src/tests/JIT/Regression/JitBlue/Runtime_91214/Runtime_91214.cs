// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Found by Antigen

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Numerics;
using Xunit;

public class Runtime_91214
{
    [Fact]
    public static void TestEntryPoint()
    {
        Method0();
    }

    public struct S
    {
        public Vector3 v3;
        public bool b;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static S Method2()
    {
        return default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Method0()
    {
        S s = Method2();
        Log(null, s.v3);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Log(object a, object b) { }
}