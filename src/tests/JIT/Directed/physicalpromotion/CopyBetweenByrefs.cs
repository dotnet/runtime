// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class CopyBetweenByrefs
{
    // Five fields prevent old-style promotion, so these copies exercise physical promotion.
    [StructLayout(LayoutKind.Sequential)]
    private struct Raw
    {
        public nint Value, Other, Last, More1, More2;
    }

    [StructLayout(LayoutKind.Sequential)]
    private ref struct Managed
    {
        public ref int Value;
        public nint Other, Last, More1, More2;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        int local = 123;
        Assert.Equal(local, ToManaged((nint)(&local)));
        Assert.Equal(local, ToRaw(ref local));

        int[] values = new[] { 12, 34, 56 };
        fixed (int* pointer = &values[1])
        {
            Assert.Equal(34, ToManaged((nint)pointer));
            Assert.Equal(34, ToRaw(ref values[1]));
            PinnedRoundTrip((nint)pointer);
        }

        SurvivesUnpinning();
        SourceRemainsTracked();
        NullRoundTrip();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Raw CreateRaw(nint pointer) =>
        new Raw { Value = pointer, Other = 41, Last = 42, More1 = 43, More2 = 44 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Managed CreateManaged(ref int value) =>
        new Managed { Value = ref value, Other = 41, Last = 42, More1 = 43, More2 = 44 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(nint value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(ref int value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ToManaged(nint pointer)
    {
        Raw src = CreateRaw(pointer);
        src.Value = pointer;
        Observe(src.Value);
        Observe(src.Value);
        Observe(src.Value);
        Managed dst = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Managed, byte>(ref dst),
                                  ref Unsafe.As<Raw, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        Observe(ref dst.Value);
        Observe(ref dst.Value);
        Observe(ref dst.Value);
        return dst.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ToRaw(ref int value)
    {
        Managed src = CreateManaged(ref value);
        src.Value = ref value;
        Observe(ref src.Value);
        Observe(ref src.Value);
        Observe(ref src.Value);
        Raw dst = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Raw, byte>(ref dst),
                                  ref Unsafe.As<Managed, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        Observe(dst.Value);
        Observe(dst.Value);
        Observe(dst.Value);
        return *(int*)dst.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Collect() => GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int[] CreateArray() => new[] { 12, 34, 56 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SurvivesUnpinning()
    {
        Managed dst = default;
        fixed (int* pointer = &CreateArray()[1])
        {
            Raw src = CreateRaw((nint)pointer);
            src.Value = (nint)pointer;
            Observe(src.Value);
            Observe(src.Value);
            Observe(src.Value);
            Unsafe.CopyBlockUnaligned(ref Unsafe.As<Managed, byte>(ref dst),
                                      ref Unsafe.As<Raw, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        }

        // The destination byref is now the only root for the unpinned array.
        Collect();
        Assert.Equal(34, dst.Value);
        dst.Value = 98;
        Collect();
        Assert.Equal(98, dst.Value);
        CheckOtherFields(dst.Other, dst.Last, dst.More1, dst.More2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SourceRemainsTracked()
    {
        Managed src = CreateManaged(ref CreateArray()[1]);
        Observe(ref src.Value);
        Observe(ref src.Value);
        Observe(ref src.Value);
        Raw dst = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Raw, byte>(ref dst),
                                  ref Unsafe.As<Managed, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        Observe(dst.Value);
        Observe(dst.Value);
        Observe(dst.Value);

        // The raw pointer is not used after collection. The original byref must
        // remain tracked independently and keep the array alive and accessible.
        Collect();
        Assert.Equal(34, src.Value);
        src.Value = 76;
        Collect();
        Assert.Equal(76, src.Value);
        CheckOtherFields(dst.Other, dst.Last, dst.More1, dst.More2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void PinnedRoundTrip(nint pointer)
    {
        Raw src = CreateRaw(pointer);
        Observe(src.Value);
        Observe(src.Value);
        Managed managed = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Managed, byte>(ref managed),
                                  ref Unsafe.As<Raw, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        Collect(); // The caller keeps the raw pointer pinned throughout the round trip.
        Assert.Equal(34, managed.Value);
        Raw dst = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Raw, byte>(ref dst),
                                  ref Unsafe.As<Managed, byte>(ref managed), (uint)Unsafe.SizeOf<Raw>());
        Collect();
        Assert.Equal(pointer, dst.Value);
        Assert.Equal(34, *(int*)dst.Value);
        CheckOtherFields(dst.Other, dst.Last, dst.More1, dst.More2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void NullRoundTrip()
    {
        Raw src = CreateRaw(0);
        Observe(src.Value);
        Observe(src.Value);
        Managed managed = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Managed, byte>(ref managed),
                                  ref Unsafe.As<Raw, byte>(ref src), (uint)Unsafe.SizeOf<Raw>());
        Collect();
        Assert.True(Unsafe.IsNullRef(ref managed.Value));
        Raw dst = default;
        Unsafe.CopyBlockUnaligned(ref Unsafe.As<Raw, byte>(ref dst),
                                  ref Unsafe.As<Managed, byte>(ref managed), (uint)Unsafe.SizeOf<Raw>());
        Assert.Equal((nint)0, dst.Value);
        CheckOtherFields(dst.Other, dst.Last, dst.More1, dst.More2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckOtherFields(nint other, nint last, nint more1, nint more2)
    {
        Assert.Equal((nint)41, other);
        Assert.Equal((nint)42, last);
        Assert.Equal((nint)43, more1);
        Assert.Equal((nint)44, more2);
    }
}
