// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_89456
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Reinterp128(Vector128<float> v)
    {
        return Unsafe.As<Vector128<float>, Vector3>(ref v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Reinterp256(Vector256<float> v)
    {
        return Unsafe.As<Vector256<float>, Vector3>(ref v);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector3 Reinterp512(Vector512<float> v)
    {
        return Unsafe.As<Vector512<float>, Vector3>(ref v);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Reinterp128(default);
        Reinterp256(default);
        Reinterp512(default);
    }
}
