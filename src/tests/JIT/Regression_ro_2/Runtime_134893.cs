// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Threading;
using Xunit;

public class Runtime_134893
{
    [StructLayout(LayoutKind.Explicit)]
    private struct OverlappingLocals
    {
        [FieldOffset(0)]
        public Vector256<byte> Vector;

        [FieldOffset(16)]
        public long Index;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct OverlappingShortLocals
    {
        [FieldOffset(0)]
        public Vector256<short> Vector;

        [FieldOffset(16)]
        public long Index;
    }

    [Fact]
    [SkipOnMono("CoreCLR JIT regression test")]
    public static void TestEntryPoint()
    {
        Assert.Equal(Vector128.Create(0, 1, 0, 0), WithElement(Vector128<int>.Zero, 1));
        Assert.Equal(Vector256.Create((byte)16), Shuffle(Vector256.Create((byte)16)));
        Assert.Equal(Vector256.Create((short)8), Shuffle(Vector256.Create((short)8)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector128<int> WithElement(Vector128<int> vector, int index) =>
        vector.WithElement(index, Interlocked.Exchange(ref index, 100));

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<byte> Shuffle(Vector256<byte> vector)
    {
        OverlappingLocals locals = default;
        locals.Vector = vector;
        return Vector256.Shuffle(
            locals.Vector, Vector256.Create((byte)Interlocked.Exchange(ref locals.Index, -1)));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static Vector256<short> Shuffle(Vector256<short> vector)
    {
        OverlappingShortLocals locals = default;
        locals.Vector = vector;
        return Vector256.Shuffle(
            locals.Vector, Vector256.Create((short)Interlocked.Exchange(ref locals.Index, -1)));
    }
}
