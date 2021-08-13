// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

public class RotationDecompositionRemovingSrcTooEarly
{
    public static int Main()
    {
        if (AddRotateLeft33(1UL << 30) != 1)
        {
            return 33;
        }

        if (AddRotateLeft31(1UL << 30) != 1UL << 62)
        {
            return 31;
        }

        return 100;
    }

    // Rotation decomposition was removing the GT_LONG source too early
    // which triggered an assert in LIR::Use::ReplaceWithLclVar that the
    // user must be in the supplied range. The method below exists solely
    // to confirm that the compilation passes without asserts.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddRotateLeft33(ulong a)
    {
        return (a + a) << 33 | (a + a) >> 31;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong AddRotateLeft31(ulong a)
    {
        return (a + a) << 31 | (a + a) >> 33;
    }
}
