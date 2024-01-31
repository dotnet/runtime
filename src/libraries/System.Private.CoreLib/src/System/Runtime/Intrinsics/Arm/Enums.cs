// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Intrinsics.Arm
{
    public enum SveMaskPattern : byte
    {
        LargestPowerOf2 = 0,      // The largest power of 2.
        VectorCount1 = 1,         // 1 element.
        VectorCount2 = 2,         // 2 elements.
        VectorCount3 = 3,         // 3 elements.
        VectorCount4 = 4,         // 4 elements.
        VectorCount5 = 5,         // 5 elements.
        VectorCount6 = 6,         // 6 elements.
        VectorCount7 = 7,         // 7 elements.
        VectorCount8 = 8,         // 8 elements.
        VectorCount16 = 9,        // 16 elements.
        VectorCount32 = 10,       // 32 elements.
        VectorCount64 = 11,       // 64 elements.
        VectorCount128 = 12,      // 128 elements.
        VectorCount256 = 13,      // 256 elements.
        LargestMultipleOf4 = 29,  // The largest multiple of 4.
        LargestMultipleOf3 = 30,  // The largest multiple of 3.
        All  = 31                 // All available (implicitly a multiple of two).
    }
}
