// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Runtime_133581
{
    [StructLayout(LayoutKind.Explicit, Size = 65600)]
    private struct Big
    {
        [FieldOffset(0)]
        public int Head;

        [FieldOffset(65584)]
        public long Tail;

        [FieldOffset(65592)]
        public object Object;
    }

    private class Holder
    {
        public Big Value;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Holder holder = new Holder();
        CopyToField(holder);
        Assert.Equal(42, holder.Value.Head);
        Assert.Equal(123456789L, holder.Value.Tail);
        Assert.Equal("payload", holder.Value.Object);

        Big destination = default;
        CopyToRef(ref destination);
        Assert.Equal(42, destination.Head);
        Assert.Equal(123456789L, destination.Tail);
        Assert.Equal("payload", destination.Object);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CopyToField(Holder holder)
    {
        Big value = default;
        value.Head = 42;
        value.Tail = 123456789;
        value.Object = "payload";
        holder.Value = value;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void CopyToRef(ref Big destination)
    {
        Big value = default;
        value.Head = 42;
        value.Tail = 123456789;
        value.Object = "payload";
        destination = value;
    }
}
