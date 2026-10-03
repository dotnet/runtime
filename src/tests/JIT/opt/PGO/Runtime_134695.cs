// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Dynamic PGO records a value histogram for SpanHelpers.Memmove and SpanHelpers.SequenceEqual
// calls. That schema entry is 8 byte aligned even on 32 bit targets, where the compressed schema
// region that precedes the counters is only 4 byte aligned. Reading the counters back for the
// tier-1 rejit used to rebase the schema offsets onto the counter region, which shifted the value
// histogram and every entry after it, and read past the end of the counters.
//
// The probes below differ in block count so the schema region length - and therefore its alignment
// relative to 8 - differs between them. They need an optimized instrumented tier (see the csproj)
// so that SpanHelpers.Memmove and SpanHelpers.SequenceEqual are inlined into them and get value
// histograms; by default only hot R2R code gets that tier.
//
// DOTNET_TieredPGO=1

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class Runtime_134695
{
    private interface IOp
    {
        int Apply(int x);
    }

    private sealed class AddOp : IOp
    {
        public int Apply(int x) => x + 1;
    }

    // Each probe makes an interface call (handle histogram) followed by SequenceEqual and CopyTo
    // (value histograms), then a chain of branches whose length varies per method.

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe1(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe2(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe3(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe4(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        if (x > 3) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe5(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        if (x > 3) r++;
        if (x > 4) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe6(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        if (x > 3) r++;
        if (x > 4) r++;
        if (x > 5) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe7(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        if (x > 3) r++;
        if (x > 4) r++;
        if (x > 5) r++;
        if (x > 6) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Probe8(IOp op, ReadOnlySpan<byte> a, ReadOnlySpan<byte> b, Span<byte> dst, int x)
    {
        int r = op.Apply(x);
        if (a.SequenceEqual(b)) r += 100;
        a.CopyTo(dst);
        r += dst[0];
        if (x > 0) r++;
        if (x > 1) r++;
        if (x > 2) r++;
        if (x > 3) r++;
        if (x > 4) r++;
        if (x > 5) r++;
        if (x > 6) r++;
        if (x > 7) r++;
        return r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckAll(IOp op, byte[] a, byte[] b, byte[] dst)
    {
        // op.Apply(100) is 101, SequenceEqual adds 100, dst[0] adds 7, and ProbeN adds N.
        Assert.Equal(209, Probe1(op, a, b, dst, 100));
        Assert.Equal(210, Probe2(op, a, b, dst, 100));
        Assert.Equal(211, Probe3(op, a, b, dst, 100));
        Assert.Equal(212, Probe4(op, a, b, dst, 100));
        Assert.Equal(213, Probe5(op, a, b, dst, 100));
        Assert.Equal(214, Probe6(op, a, b, dst, 100));
        Assert.Equal(215, Probe7(op, a, b, dst, 100));
        Assert.Equal(216, Probe8(op, a, b, dst, 100));
    }

    [Fact]
    public static void TestEntryPoint()
    {
        byte[] a = new byte[64];
        Array.Fill(a, (byte)7);
        byte[] b = (byte[])a.Clone();
        byte[] dst = new byte[64];
        IOp op = new AddOp();

        // Tier-0 instrumented code collects the histograms; the background worker then rejits at
        // tier-1 and reads them back. Give that transition time to happen.
        for (int i = 0; i < 200; i++)
        {
            CheckAll(op, a, b, dst);
            if ((i % 20) == 0)
            {
                Thread.Sleep(15);
            }
        }

        Thread.Sleep(200);

        // Now running the tier-1 code that consumed the profile data.
        for (int i = 0; i < 200; i++)
        {
            CheckAll(op, a, b, dst);
        }
    }
}
