// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_119674
{
    private struct StructWithReference
    {
        public object Reference;
        public int Value;
    }

    private static class MovableStatic
    {
        public static StructWithReference Value;
    }

    private static object[] s_fragmentation;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe bool CheckMovableStatic()
    {
        MovableStatic.Value.Reference = s_fragmentation;
        MovableStatic.Value.Value = 42;

        fixed (int* value = &MovableStatic.Value.Value)
        {
            return PointerSurvivesCompactingGc(value);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static unsafe bool PointerSurvivesCompactingGc(int* value)
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        fixed (int* current = &MovableStatic.Value.Value)
        {
            return value == current && *value == 42;
        }
    }

    private static void PrepareFragmentation()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);

        object[] objects = new object[1024];
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i] = new byte[128];
        }

        for (int i = 0; i < objects.Length; i += 2)
        {
            objects[i] = null;
        }

        s_fragmentation = objects;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        PrepareFragmentation();

        Assert.True(CheckMovableStatic());
    }
}
