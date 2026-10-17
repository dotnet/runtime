// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_135117
{
    public struct S1
    {
        public byte Value;
    }

    public struct S2
    {
        public ushort Value;
    }

    public struct SF
    {
        public float Value;
    }

    public struct SO
    {
        public object Value;
    }

    private sealed class Box
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S1 MakeS1(byte value) => new S1 { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static S2 MakeS2(ushort value) => new S2 { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Half MakeHalf(float value) => (Half)value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SF MakeSF(float value) => new SF { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SO MakeSO(object value) => new SO { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreS1(ref S1 destination, byte value) => destination = MakeS1(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreS2(ref S2 destination, ushort value) => destination = MakeS2(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreHalf(ref Half destination, float value) => destination = MakeHalf(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreSF(ref SF destination, float value) => destination = MakeSF(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreSO(ref SO destination, object value) => destination = MakeSO(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreNewBoxes(SO[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            StoreSO(ref array[i], new Box { Value = i });
        }
    }

    [Fact]
    public static void TestEntryPoint()
    {
        S1[] s1 = new S1[4];
        for (int i = 0; i < s1.Length; i++)
        {
            s1[i].Value = 0xAA;
        }
        StoreS1(ref s1[1], 0x11);
        Assert.Equal(0xAA, s1[0].Value);
        Assert.Equal(0x11, s1[1].Value);
        Assert.Equal(0xAA, s1[2].Value);
        Assert.Equal(0xAA, s1[3].Value);

        S2[] s2 = new S2[4];
        for (int i = 0; i < s2.Length; i++)
        {
            s2[i].Value = 0xAAAA;
        }
        StoreS2(ref s2[1], 0x1111);
        Assert.Equal(0xAAAA, s2[0].Value);
        Assert.Equal(0x1111, s2[1].Value);
        Assert.Equal(0xAAAA, s2[2].Value);

        Half[] halves = { (Half)1f, (Half)2f, (Half)3f };
        StoreHalf(ref halves[0], 4f);
        Assert.Equal((Half)4f, halves[0]);
        Assert.Equal((Half)2f, halves[1]);
        Assert.Equal((Half)3f, halves[2]);

        SF[] sf = new SF[3];
        for (int i = 0; i < sf.Length; i++)
        {
            sf[i].Value = -1f;
        }
        StoreSF(ref sf[1], 2.5f);
        Assert.Equal(-1f, sf[0].Value);
        Assert.Equal(2.5f, sf[1].Value);
        Assert.Equal(-1f, sf[2].Value);

        // The store of a struct wrapping a reference needs a write barrier so that a gen0 GC
        // sees the young object referenced from an older array.
        SO[] so = new SO[16];
        GC.Collect();
        GC.Collect();
        StoreNewBoxes(so);
        for (int i = 0; i < 4; i++)
        {
            GC.Collect(0);
            _ = new byte[1024];
        }
        for (int i = 0; i < so.Length; i++)
        {
            Assert.Equal(i, Assert.IsType<Box>(so[i].Value).Value);
        }
    }
}
