// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_134486
{
    [ConditionalFact(typeof(Avx2), nameof(Avx2.IsSupported))]
    public static unsafe void TestGather()
    {
        int* data = stackalloc int[] { 10, 11, 12, 13 };
        Assert.Equal(2, Gather(data, Vector128.Create(1, 0, 2, 0), Vector128.Create(11, 10, 12, 10)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe int Gather(int* data, Vector128<int> indices, Vector128<int> expected)
    {
        Vector128<int> a = Avx2.GatherVector128(data, indices, 4);
        Vector128<int> b = Avx2.GatherVector128(data, indices.AsInt64(), 4);

        if (a == expected)
        {
            return b == expected ? 1 : 2;
        }
        return 3;
    }

    [ConditionalFact(typeof(AdvSimd.Arm64), nameof(AdvSimd.Arm64.IsSupported))]
    public static void TestAddSaturateScalar()
    {
        Assert.True(DistinguishAddSaturateScalar(Vector64.CreateScalar(1), Vector64.CreateScalar(-1)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static bool DistinguishAddSaturateScalar(Vector64<int> left, Vector64<int> right)
    {
        return AdvSimd.Arm64.AddSaturateScalar(left, right) != AdvSimd.Arm64.AddSaturateScalar(left, right.AsUInt32());
    }
}
