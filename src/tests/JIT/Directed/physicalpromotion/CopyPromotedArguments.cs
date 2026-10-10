// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class CopyPromotedArguments
{
    private static long s_consumed;

    [Fact]
    public static void TestEntryPoint()
    {
        foreach (long value in new[] { 0L, 1L, -1L, long.MinValue, long.MaxValue })
        {
            CopyArgumentIsolation(value);
            CopyArgumentAcrossException(value);
            Assert.Equal(value + 1 + (int)(value + 1), CleanSource(value));
        }
    }

    [StructLayout(LayoutKind.Explicit, Size = 24)]
    private struct S
    {
        [FieldOffset(0)] public long Wide;
        [FieldOffset(0)] public int Narrow;
        [FieldOffset(8)] public long Other;
        [FieldOffset(16)] public long Last;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S Create(long value) => new S { Wide = value, Other = value + 1, Last = value + 2 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Observe(long value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(S value)
    {
        Assert.Equal(value.Other + 1, value.Last);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CheckFields(S value, long wide, long other, long last)
    {
        Assert.Equal(wide, value.Wide);
        Assert.Equal(other, value.Other);
        Assert.Equal(last, value.Last);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeAndModify(S value)
    {
        s_consumed = value.Wide;
        value.Wide = ~value.Wide;
        value.Other = -7;
        CheckFields(value, ~s_consumed, -7, s_consumed + 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyArgumentIsolation(long value)
    {
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        ConsumeAndModify(src);
        Assert.Equal(value + 1, s_consumed);
        CheckFields(src, value + 1, value + 1, value + 2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ConsumeThenThrow(S value)
    {
        ConsumeAndModify(value);
        throw new System.InvalidOperationException();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CopyArgumentAcrossException(long value)
    {
        S src = Create(value);
        try
        {
            src.Wide++;
            Observe(src.Wide);
            Observe(src.Wide);
            Observe(src.Wide);
            ConsumeThenThrow(src);
        }
        catch (System.InvalidOperationException)
        {
            CheckFields(src, value + 1, value + 1, value + 2);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long CleanSource(long value)
    {
        // X64-WINDOWS: call {{.*}}CopyPromotedArguments:Create
        // Build the outgoing argument from the replacement, without synchronizing
        // source storage and immediately loading across that narrow store.
        // X64-WINDOWS: mov [[VALUE:r[a-z0-9]+]], qword ptr [rsp+[[SOURCE:0x[0-9A-Fa-f]+]]]
        // X64-WINDOWS-NOT: mov qword ptr [rsp+[[SOURCE]]], [[VALUE]]
        // X64-WINDOWS: call {{.*}}CopyPromotedArguments:Consume
        S src = Create(value);
        src.Wide++;
        Observe(src.Wide);
        Observe(src.Wide);
        Observe(src.Wide);
        Consume(src); // The outgoing copy can take Wide directly from its replacement.
        S dst = src;
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Observe(dst.Narrow);
        Consume(src);
        return src.Wide + dst.Narrow;
    }
}
