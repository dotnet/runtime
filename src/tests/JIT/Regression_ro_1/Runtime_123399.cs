// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Xunit;

public class Runtime_123399
{
    [ConditionalFact(typeof(Avx512F), nameof(Avx512F.IsSupported))]
    public static void TestEntryPoint()
    {
        Vector512<int> result = AndMaskZero(Vector512<int>.One);
        Assert.Equal(Vector512<int>.Zero, result);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static Vector512<int> AndMaskZero(Vector512<int> v) =>
        // Vector512.Equals(v, v) produces an all-ones mask
        // Vector512.LessThan(v.AsUInt32(), Vector512<uint>.Zero) produces a zero mask
        // (no unsigned integer can be less than zero)
        // The AND of these two masks should produce zero, not all-ones
        Vector512.Equals(v, v) & Vector512.LessThan(v.AsUInt32(), Vector512<uint>.Zero).AsInt32();
}
