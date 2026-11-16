// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class Runtime_134604
{
    [Fact]
    public static unsafe void TestEntryPoint()
    {
        if (!AdvSimd.Arm64.IsSupported)
        {
            return;
        }

        byte* table = stackalloc byte[256];
        for (int i = 0; i < 256; i++)
        {
            table[i] = (byte)i;
        }

        uint actual = Run(table, 0xF7F8FBFE);
        Assert.Equal(0xF7F8FC06u, actual);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static unsafe uint Run(byte* table, uint value)
    {
        for (int i = 0; i < 8; i++)
        {
            var (a0, a1, a2, a3) = AdvSimd.Arm64.Load4xVector128(table);
            var (b0, b1, b2, b3) = AdvSimd.Arm64.Load4xVector128(table + 64);
            var (c0, c1, c2, c3) = AdvSimd.Arm64.Load4xVector128(table + 128);
            var (d0, d1, d2, d3) = AdvSimd.Arm64.Load4xVector128(table + 192);
            Vector128<byte> stride = Vector128.Create((byte)64);
            Vector128<byte> index  = Vector128.CreateScalar(value).AsByte();

            Vector128<byte> result = AdvSimd.Arm64.VectorTableLookup((a0, a1, a2, a3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (b0, b1, b2, b3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (c0, c1, c2, c3), index);
            index -= stride;
            result = AdvSimd.Arm64.VectorTableLookupExtension(result, (d0, d1, d2, d3), index);
            value  = result.AsUInt32().ToScalar() + 1;
        }

        return value;
    }
}
