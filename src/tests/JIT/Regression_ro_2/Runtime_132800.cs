// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using Xunit;

// Test that the implicit broadcast from Vector.Dot gets lowered properly

public class Runtime_132800
{
    [ConditionalFact(typeof(AdvSimd), nameof(AdvSimd.IsSupported))]
    public static int TestEntryPoint()
    {
        int result = Vector128.IndexOf(
            AdvSimd.SubtractRoundedHighNarrowingUpper(Vector64<uint>.AllBitsSet,
                                                      Vector128<ulong>.AllBitsSet,
                                                      Vector128<ulong>.AllBitsSet),
            Vector.Dot(Vector<uint>.One, Vector<uint>.One));

        if (result != -1)
        {
            return 101;
        }

        if (!BroadcastDot(Vector64<byte>.One, Vector64<byte>.One).Equals(Vector64.Create((byte)8)))
        {
            return 102;
        }

        if (!BroadcastDot(Vector128<byte>.One, Vector128<byte>.One).Equals(Vector128.Create((byte)16)))
        {
            return 103;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector64<byte> BroadcastDot(Vector64<byte> left, Vector64<byte> right) =>
        Vector64.Create(Vector64.Dot(left, right));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<byte> BroadcastDot(Vector128<byte> left, Vector128<byte> right) =>
        Vector128.Create(Vector128.Dot(left, right));
}
