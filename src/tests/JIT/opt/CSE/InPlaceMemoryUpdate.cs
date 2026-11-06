// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// CSE should not replace the load in "*p = *p op x" when that would prevent
// the memory-operand form of 'op'.
public class InPlaceMemoryUpdate
{
    private class Holder
    {
        public int Flags;
        public long Count;
    }

    [Fact]
    public static void UpdatesMemory()
    {
        Holder holder = new Holder { Flags = 0x13, Count = 10 };
        Assert.Equal(0x13, OrField(holder));
        Assert.Equal(0x53, holder.Flags);
        Assert.Equal(10, AddField(holder, 5));
        Assert.Equal(15, holder.Count);
        Assert.Equal(0x53, OrFieldCommuted(holder, 0x100));
        Assert.Equal(0x153, holder.Flags);
        Assert.Equal(15, CheckedAddField(holder, 2));
        Assert.Equal(17, holder.Count);
        Assert.Throws<OverflowException>(() => CheckedAddField(holder, long.MaxValue));
        Assert.Equal(17, holder.Count);

        int value = 0x13;
        Assert.Equal(0x13, AndRef(ref value));
        Assert.Equal(0x03, value);
        Assert.Equal(0x03, NotRef(ref value));
        Assert.Equal(~0x03, value);
        Assert.Equal(~0x03, NegRef(ref value));
        Assert.Equal(0x04, value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int OrField(Holder holder)
    {
        // X64: or dword ptr [{{[a-z0-9]+}}+{{[0-9A-Fa-fx]+}}], 64
        int old = holder.Flags;
        holder.Flags |= 0x40;
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long AddField(Holder holder, long x)
    {
        // X64: add qword ptr [{{[a-z0-9]+}}+{{[0-9A-Fa-fx]+}}], {{[a-z0-9]+}}
        long old = holder.Count;
        holder.Count += x;
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int OrFieldCommuted(Holder holder, int x)
    {
        // X64: or dword ptr [{{[a-z0-9]+}}+{{[0-9A-Fa-fx]+}}], {{[a-z0-9]+}}
        int old = holder.Flags;
        holder.Flags = x | holder.Flags;
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CheckedAddField(Holder holder, long x)
    {
        // Overflow-checked ops have no memory-operand form, so the load stays CSE'd.
        // X64: mov [[OLD:[a-z0-9]+]], qword ptr [{{[a-z0-9]+}}+{{[0-9A-Fa-fx]+}}]
        // X64-NOT: qword ptr [{{[a-z0-9]+}}+{{[0-9A-Fa-fx]+}}]
        // X64: jo
        long old = holder.Count;
        holder.Count = checked(holder.Count + x);
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int AndRef(ref int value)
    {
        // X64: and dword ptr [{{[a-z0-9]+}}], -17
        int old = value;
        value &= ~0x10;
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NotRef(ref int value)
    {
        // X64: not dword ptr [{{[a-z0-9]+}}]
        int old = value;
        value = ~value;
        return old;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int NegRef(ref int value)
    {
        // X64: neg dword ptr [{{[a-z0-9]+}}]
        int old = value;
        value = -value;
        return old;
    }
}
