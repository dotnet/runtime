// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        Vector3 v3 = Test1(new Vector4(1, 2, 3, 4));
        if (v3.X != 1 || v3.Y != 2 || v3.Z != 3)
        {
            return 42;
        }

        Vector4 v4 = Test2(new Vector4(1, 2, 3, 4), new Vector4(2, 1, 4, 3));
        if (v4.X != 3 || v4.Y != 6 || v4.Z != 21 || v4.W != 0)
        {
            return 43;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static Vector3 Test1(Vector4 value)
    {
        return Unsafe.As<Vector4, Vector3>(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static Vector4 Test2(Vector4 a, Vector4 b)
    {
        Vector4 c = a + b;
        Vector3 d = Unsafe.As<Vector4, Vector3>(ref c);
        return a * new Vector4(d, 0);
    }
}
