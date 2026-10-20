// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

[StructLayout(LayoutKind.Explicit)]
struct State
{
    [FieldOffset(8)]
    public ulong Coefficient;

    [FieldOffset(16)]
    public ulong Other;
}

public class Runtime_135844
{
    [ConditionalFact(typeof(Avx512F), nameof(Avx512F.IsSupported))]
    public static void TestEntryPoint()
    {
        State state = new() { Coefficient = 3, Other = 29 };
        Vector512<uint> unsignedInput = Vector512.Create(2u);
        Vector512<int> signedInput = Vector512.Create(2);

        Vector512<ulong> unsignedResult = MultiplyUnsigned(ref state, unsignedInput);
        Vector512<long> signedResult = MultiplySigned(ref state, signedInput);

        Assert.Equal(Vector512.Create(6ul), unsignedResult);
        Assert.Equal(Vector512.Create(6L), signedResult);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<ulong> MultiplyUnsigned(ref State state, Vector512<uint> input) =>
        Avx512F.Multiply(input, Vector512.Create(state.Coefficient).AsUInt32());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector512<long> MultiplySigned(ref State state, Vector512<int> input) =>
        Avx512F.Multiply(input, Vector512.Create(unchecked((long)state.Coefficient)).AsInt32());
}
