// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_89456
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<ushort> PermuteVar32x16x2Test(Vector512<ushort> left, ushort right)
    {
        var r8w = right;
        var zmm0 = left;
        var zmm1 = Vector512.CreateScalarUnsafe(r8w);
        var zmm2 = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
        return Avx512BW.PermuteVar32x16x2(zmm0, zmm2, zmm1);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (Avx512BW.IsSupported)
        {
            var expected = Vector512.Create((ushort)1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32);
            Vector512<ushort> actual = PermuteVar32x16x2Test(Vector512.Create(ushort.MinValue, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31), 32);
            if (actual != expected)
            {
                return 101;
            }
        }
        return 100;
    }
}
