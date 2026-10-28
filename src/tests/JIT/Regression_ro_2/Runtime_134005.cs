// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// The GC poll that Buffer.BulkMoveWithWriteBarrier requests used to end up in the block that
// terminates an EH region, and "Insert GC Polls" did not know how to handle such blocks.
public class Runtime_134005
{
    private static object[] s_dst;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void PollInFinally()
    {
        try
        {
            Console.Write("");
        }
        finally
        {
            object[] a = new object[4];
            object[] b = new object[4];
            a[0] = a;
            s_dst = b;
            a.AsSpan().CopyTo(b.AsSpan());
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void PollInCatch()
    {
        try
        {
            throw new Exception();
        }
        catch (Exception)
        {
            object[] a = new object[4];
            object[] b = new object[4];
            a[0] = a;
            s_dst = b;
            a.AsSpan().CopyTo(b.AsSpan());
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        PollInFinally();
        Assert.NotNull(s_dst);

        s_dst = null;
        PollInCatch();
        Assert.NotNull(s_dst);
    }
}
