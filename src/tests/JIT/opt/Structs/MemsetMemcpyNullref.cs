// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class MemsetMemcpyNullref
{
    [Fact]
    public static void MemsetMemcpyThrowNullRefonNull()
    {
        Assert.Throws<NullReferenceException>(() => MemoryInit(null));
        Assert.Throws<NullReferenceException>(() => MemoryCopy(null, null));
        Assert.Throws<NullReferenceException>(() =>
            {
                // Check when only src is null
                HugeStruct hs = default;
                MemoryCopy(&hs, null);
            });
        Assert.Throws<NullReferenceException>(() =>
            {
                // Check when only dst is null
                HugeStruct hs = default;
                MemoryCopy(null, &hs);
            });

        // Check various lengths
        uint[] lengths = [1, 10, 100, 1000, 10000, 100000, 1000000];
        foreach (uint length in lengths)
        {
            Assert.Throws<NullReferenceException>(() => MemoryInitByref(ref Unsafe.NullRef<byte>(), length));
            Assert.Throws<NullReferenceException>(() => MemoryCopyByref(ref Unsafe.NullRef<byte>(), ref Unsafe.NullRef<byte>(), length));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryCopy(HugeStruct* dst, HugeStruct* src) => 
        *dst = *src;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryCopyByref(ref byte dst, ref byte src, uint len) => 
        Unsafe.CopyBlockUnaligned(ref dst, ref src, len);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryInit(HugeStruct* dst) => 
        *dst = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryInitByref(ref byte dst, uint len) => 
        Unsafe.InitBlockUnaligned(ref dst, 42, len);

    private struct HugeStruct
    {
        public fixed byte Data[20_000];
    }
}
