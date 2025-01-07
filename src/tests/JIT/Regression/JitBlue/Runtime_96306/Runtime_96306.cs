// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_96306
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Foo(new Point2D { V = new Vector2(101, -1) }, 100);
    }

    // 'a' is passed in rcx but homed into xmm1 after promotion.
    // 'scale' is passed in xmm1 but spilled because of the call to Bar.
    // We must take care that we spill 'scale' before we home 'a'.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(Point2D a, float scale)
    {
        Bar();
        return ReturnValue(scale);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReturnValue(float value) => (int)value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Bar() { }

    private struct Point2D
    {
        public Vector2 V;
    }
}

