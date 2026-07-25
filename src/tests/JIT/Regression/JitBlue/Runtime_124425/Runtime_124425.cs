// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_124425
{
    [Fact]
    public static void TestEntryPoint()
    {
        Vector2? v1 = null;
        Assert.Throws<NullReferenceException>(() => CastToVector2(v1));
        Vector3? v2 = null;
        Assert.Throws<NullReferenceException>(() => CastToVector3(v2));
        Vector4? v3 = null;
        Assert.Throws<NullReferenceException>(() => CastToVector4(v3));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CastToVector2<T>(T? value)
    {
        var a = (Vector2)(object)value!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CastToVector3<T>(T? value)
    {
        var a = (Vector3)(object)value!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void CastToVector4<T>(T? value)
    {
        var a = (Vector4)(object)value!;
    }
}