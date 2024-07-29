// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

// This test ensures that we pass by-value Vector3 arguments correctly, especially on x86.
// On x86, we must ensure that we properly treat an outgoing Vector3 as a 12-byte value
// when pushing it onto the stack.

public static class CoreFX15913
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static float Sum(float v3, Vector3 v)
    {
        return v3 + v.X + v.Y + v.Z;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        float f = 4.0f;
        return Sum(f, Vector3.Zero) == f ? 100 : 0;
    }
}
