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

        // These APIs are not expected to fail/throw on zero length, even if pointers are not valid
        byte valid = 0;
        MemoryInitByref(ref Unsafe.NullRef<byte>(), 0);
        MemoryCopyByref(ref Unsafe.NullRef<byte>(), ref valid, 0);
        MemoryCopyByref(ref valid, ref Unsafe.NullRef<byte>(), 0);
        MemoryCopyByref(ref Unsafe.NullRef<byte>(), ref Unsafe.NullRef<byte>(), 0);

        byte valid2 = 0;
        MemoryInitByrefZeroLen(ref valid);
        MemoryInitByrefZeroLen(ref Unsafe.NullRef<byte>());
        MemoryCopyByrefZeroLen(ref valid, ref valid2);
        MemoryCopyByrefZeroLen(ref valid, ref Unsafe.NullRef<byte>());
        MemoryCopyByrefZeroLen(ref Unsafe.NullRef<byte>(), ref valid2);
        MemoryCopyByrefZeroLen(ref Unsafe.NullRef<byte>(), ref Unsafe.NullRef<byte>());
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryCopyByrefZeroLen(ref byte dst, ref byte src) => 
        Unsafe.CopyBlockUnaligned(ref dst, ref src, 0);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MemoryInitByrefZeroLen(ref byte dst) => 
        Unsafe.InitBlockUnaligned(ref dst, 42, 0);
}
