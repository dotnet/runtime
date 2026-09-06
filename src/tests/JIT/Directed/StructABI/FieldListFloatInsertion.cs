// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class FieldListFloatInsertion
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static FloatPair ReturnPair(float x, float y)
    {
        // X64-LINUX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        // X64-OSX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        return new FloatPair { X = x, Y = y };
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float PassPair(float x, float y)
    {
        // X64-LINUX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        // X64-OSX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        return SumPair(new FloatPair { X = x, Y = y });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float PassPairWithConstant(float y)
    {
        // X64-LINUX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        // X64-OSX-FULL-LINE: {{v?unpcklps}} {{xmm[0-9]+}}, {{xmm[0-9]+}}
        return SumPair(new FloatPair { X = 0.0f, Y = y });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float SumPair(FloatPair value) => value.X + value.Y;

    [Fact]
    public static int TestEntryPoint()
    {
        FloatPair value = ReturnPair(1.0f, 2.0f);
        return (value.X == 1.0f) && (value.Y == 2.0f) && (PassPair(3.0f, 4.0f) == 7.0f) &&
                       (PassPairWithConstant(5.0f) == 5.0f)
                   ? 100
                   : 0;
    }

    private struct FloatPair
    {
        public float X;
        public float Y;
    }
}
