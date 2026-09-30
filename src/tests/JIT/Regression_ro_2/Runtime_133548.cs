// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_133548
{
    [ConditionalFact(typeof(Sse41), nameof(Sse41.IsSupported))]
    public static void TestEntryPoint()
    {
        Vector128<byte> right = Vector128.Create(0x01010000u, 0x00000101u, 0x01010000u, 0x00000101u).AsByte();
        Vector128<float> value = Vector128.Create(1.1f);

        Assert.Equal(Vector128.Create(0.0f, 1.1f, 0.0f, 1.1f), Blend(value, Vector128<byte>.Zero, right));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<float> Blend(Vector128<float> value, Vector128<byte> left, Vector128<byte> right) =>
        Sse41.BlendVariable(Vector128<float>.Zero, value, Sse2.CompareEqual(left, right).AsSingle());
}
