// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runs four call shapes with DOTNET_ForceRelocs=1. On RISC-V, where W^X is off by default, Checked and Debug
// runtimes then reserve executable memory at scattered addresses, which can put helper and cross-heap targets
// out of auipc + jalr range, so that those calls go through jump stubs.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public static class ForceRelocsJumpStubs
{
    private sealed class Holder
    {
        public object Field;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        var holder = new Holder();
        Assert.Same(StoreNew(holder), holder.Field);
        Assert.Equal("jumpstub", Concat("jump", "stub"));
        Assert.Equal(42, CatchFromThrower());
        Assert.Equal(7, Add(3, 4));
    }

    // Allocation and write-barrier helpers.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object StoreNew(Holder holder) => holder.Field = new object();

    // A call into CoreLib.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string Concat(string a, string b) => string.Concat(a, b);

    // The throw helper, and an unwind back through the caller.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CatchFromThrower()
    {
        try
        {
            Thrower();
            return -1;
        }
        catch (InvalidOperationException)
        {
            return 42;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Thrower() => throw new InvalidOperationException();

    // A plain managed call with a return value.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Add(int a, int b) => a + b;
}
