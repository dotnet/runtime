// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// A GC ref pushed onto the wasm operand stack is a copy the GC can't update, so it goes stale
// when a call relocates the referent. Only reproduces under crossgen wasm R2R (TargetOS=browser).

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_131373
{
    public sealed class Box
    {
        public object Slot;
        public int Tag;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        for (int i = 0; i < 400; i++)
        {
            for (int j = 0; j < 32; j++)
            {
                byte[] garbage = new byte[8192];
                GC.KeepAlive(garbage);
            }

            Box box = new Box { Tag = i };
            StoreThroughByref(ref box.Slot, i);
            Assert.NotNull(box.Slot);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreThroughByref(ref object slot, int i)
    {
        slot = Allocate(i);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static object Allocate(int i)
    {
        for (int j = 0; j < 48; j++)
        {
            byte[] garbage = new byte[8192];
            GC.KeepAlive(garbage);
        }

        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        return new string('x', 1 + (i & 7));
    }
}
