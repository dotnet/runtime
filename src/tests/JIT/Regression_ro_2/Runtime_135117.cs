// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

// Each Store* method stores a struct returned in a single register from a non-inlined call
// through a ref. The store must write exactly the struct's size.
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

    public struct SL
    {
        public long Value;
    }

    public struct SD
    {
        public double Value;
    }

    public struct SV
    {
        public Vector128<int> Value;
    }

    public struct SI
    {
        public Int128 Value;
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
    private static SL MakeSL(long value) => new SL { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SD MakeSD(double value) => new SD { Value = value };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SV MakeSV(int value) => new SV { Value = Vector128.Create(value) };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Int128 MakeInt128(long value) => new Int128((ulong)value, (ulong)value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static SI MakeSI(long value) => new SI { Value = new Int128((ulong)value, (ulong)value) };

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
    private static void StoreSL(ref SL destination, long value) => destination = MakeSL(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreSD(ref SD destination, double value) => destination = MakeSD(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreSV(ref SV destination, int value) => destination = MakeSV(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreInt128(ref Int128 destination, long value) => destination = MakeInt128(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void StoreSI(ref SI destination, long value) => destination = MakeSI(value);

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

        SL[] sl = new SL[3];
        for (int i = 0; i < sl.Length; i++)
        {
            sl[i].Value = -1;
        }
        StoreSL(ref sl[1], 0x1122334455667788);
        Assert.Equal(-1, sl[0].Value);
        Assert.Equal(0x1122334455667788, sl[1].Value);
        Assert.Equal(-1, sl[2].Value);

        SD[] sd = new SD[3];
        for (int i = 0; i < sd.Length; i++)
        {
            sd[i].Value = -1.0;
        }
        StoreSD(ref sd[1], 2.5);
        Assert.Equal(-1.0, sd[0].Value);
        Assert.Equal(2.5, sd[1].Value);
        Assert.Equal(-1.0, sd[2].Value);

        SV[] sv = new SV[3];
        for (int i = 0; i < sv.Length; i++)
        {
            sv[i].Value = Vector128.Create(-1);
        }
        StoreSV(ref sv[1], 7);
        Assert.Equal(Vector128.Create(-1), sv[0].Value);
        Assert.Equal(Vector128.Create(7), sv[1].Value);
        Assert.Equal(Vector128.Create(-1), sv[2].Value);

        Int128[] i128 = { -1, -1, -1 };
        StoreInt128(ref i128[1], 0x1122334455667788);
        Assert.Equal((Int128)(-1), i128[0]);
        Assert.Equal(new Int128(0x1122334455667788, 0x1122334455667788), i128[1]);
        Assert.Equal((Int128)(-1), i128[2]);

        SI[] si = new SI[3];
        for (int i = 0; i < si.Length; i++)
        {
            si[i].Value = -1;
        }
        StoreSI(ref si[1], 0x1122334455667788);
        Assert.Equal((Int128)(-1), si[0].Value);
        Assert.Equal(new Int128(0x1122334455667788, 0x1122334455667788), si[1].Value);
        Assert.Equal((Int128)(-1), si[2].Value);

        object obj = new object();
        SO[] so = new SO[3];
        StoreSO(ref so[1], obj);
        Assert.Null(so[0].Value);
        Assert.Same(obj, so[1].Value);
        Assert.Null(so[2].Value);
    }
}
