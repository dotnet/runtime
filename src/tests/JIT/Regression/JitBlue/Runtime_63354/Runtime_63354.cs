// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_63354
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Test1(Vector3 v1, ref Vector3 v2)
    {
        v1.X = 100;
        v2 = v1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Vector3 Test2(Vector3 v)
    {
        for (int i = 0; i < 1; i++)
        {
            var vs = new Vector3[] { v };
            Box(v);
            v.X = 0;
        }
        return v;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Box(object o) {}

    [Fact]
    public static int TestEntryPoint()
    {
        for (int i = 0; i < 1; i++)
        {
            Test2(Vector3.Zero);
        }

        Vector3 v = default;
        Test1(v, ref v);
        return (int)v.X;
    }
}
