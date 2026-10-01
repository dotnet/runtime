// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_131640;

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// On wasm32 these are 12, 20 and 28 bytes: sizes the interpreter-to-R2R thunk used to zero-pad
// out to the next multiple of 8, writing past the end of the caller's return buffer.
public struct Db12
{
    public byte[] Data;
    public int Length;
    public bool Flag;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Db12 CreateRented(int n)
    {
        Db12 d = default;
        d.Data = ArrayPool<byte>.Shared.Rent(n < 16 ? 16 : n);
        d.Flag = true;
        return d;
    }

    public void Dispose() => Return(ref Data);

    internal static void Return(ref byte[] data)
    {
        byte[] d = data;
        data = null;
        if (d is not null)
        {
            ArrayPool<byte>.Shared.Return(d);
        }
    }
}

public struct Db20
{
    public byte[] Data;
    public int Length;
    public int A;
    public int B;
    public int C;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Db20 CreateRented(int n)
    {
        Db20 d = default;
        d.Data = ArrayPool<byte>.Shared.Rent(n < 16 ? 16 : n);
        d.C = 1;
        return d;
    }

    public void Dispose() => Db12.Return(ref Data);
}

public struct Db28
{
    public byte[] Data;
    public int Length;
    public int A;
    public int B;
    public int C;
    public int D;
    public int E;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static Db28 CreateRented(int n)
    {
        Db28 d = default;
        d.Data = ArrayPool<byte>.Shared.Rent(n < 16 ? 16 : n);
        d.E = 1;
        return d;
    }

    public void Dispose() => Db12.Return(ref Data);
}

public struct RowStack
{
    public byte[] Buf;
    public int Len;
    public int Idx;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public RowStack(int n)
    {
        Buf = ArrayPool<byte>.Shared.Rent(n);
        Len = n;
        Idx = n;
    }

    public void Dispose() => Db12.Return(ref Buf);
}

public class Runtime_131640
{
    // Two byref-like values are kept live across the struct-returning call so the repro does
    // not depend on a single frame slot landing above that call's return buffer.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Sum(ReadOnlySpan<byte> span, ReadOnlySpan<byte> other, ref RowStack stack)
    {
        Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(span)));
        Assert.False(Unsafe.IsNullRef(ref MemoryMarshal.GetReference(other)));

        int total = 0;
        for (int i = 0; i < span.Length; i++)
        {
            total += span[i];
        }

        stack.Idx = 0;
        return total;
    }

    // The spans are materialized before the struct-returning call and stay live across it.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run12(ReadOnlyMemory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;
        ReadOnlySpan<byte> other = bytes.Span;
        Db12 db = Db12.CreateRented(bytes.Length);
        RowStack stack = new RowStack(512);
        try
        {
            return Sum(span, other, ref stack);
        }
        finally
        {
            stack.Dispose();
            db.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run20(ReadOnlyMemory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;
        ReadOnlySpan<byte> other = bytes.Span;
        Db20 db = Db20.CreateRented(bytes.Length);
        RowStack stack = new RowStack(512);
        try
        {
            return Sum(span, other, ref stack);
        }
        finally
        {
            stack.Dispose();
            db.Dispose();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Run28(ReadOnlyMemory<byte> bytes)
    {
        ReadOnlySpan<byte> span = bytes.Span;
        ReadOnlySpan<byte> other = bytes.Span;
        Db28 db = Db28.CreateRented(bytes.Length);
        RowStack stack = new RowStack(512);
        try
        {
            return Sum(span, other, ref stack);
        }
        finally
        {
            stack.Dispose();
            db.Dispose();
        }
    }

    [Theory]
    [InlineData(12)]
    [InlineData(20)]
    [InlineData(28)]
    public static void StructReturnDoesNotOverflowTheCallersReturnBuffer(int wasm32StructSize)
    {
        ReadOnlyMemory<byte> data = new byte[] { 1, 2, 3, 4, 5 };
        int actual = wasm32StructSize switch
        {
            12 => Run12(data),
            20 => Run20(data),
            _ => Run28(data),
        };

        Assert.Equal(15, actual);
    }
}
