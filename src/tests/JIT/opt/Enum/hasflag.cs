// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Some simple tests for the Enum.HasFlag optimization.
//
// All the calls to HasFlag below are now optimized to simple compares,
// except for the case in the try/catch.

using System;
using System.Runtime.CompilerServices;
using Xunit;

enum E
{
    ZERO,
    RED,
    BLUE
}

enum F : ulong
{
    ZERO,
    RED =  0x800000000UL,
    BLUE = 0x1000000000UL
}

enum G : byte
{
    ZERO,
    RED  = 6,
    BLUE = 9
}

struct EStruct
{
    public E e;
}

class EClass
{
    public E e;
}

class ShortHolder
{
    public ShortHolder(short s) { v = s; }
    public short v;
}

public class P
{
    static E[] ArrayOfE = { E.RED, E.BLUE };

    static E StaticE = E.BLUE;

    static E GetE()
    {
        return E.RED;
    }

    static E GetESideEffect()
    {
        E e = StaticE;
        switch (e)
        {
            case E.RED:
                StaticE = E.BLUE;
                break;
            case E.BLUE:
                StaticE = E.RED;
                break;
        }
        return e;
    }

    internal static bool ByrefE(ref E e1, E e2)
    {
        return e1.HasFlag(e2);
    }

    internal static bool ByrefF(ref F e1, F e2)
    {
        return e1.HasFlag(e2);
    }

    internal static bool ByrefG(ref G e1, G e2)
    {
        return e1.HasFlag(e2);
    }

    // Example from GitHub 23847
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static bool GitHub23847(E e1, short s)
    {
        return GitHub23847Aux(s).HasFlag(e1);
    }

    // Once this is inlined we end up with a short pre-boxed value
    internal static E GitHub23847Aux(short s)
    {
        ShortHolder h = new ShortHolder(s);
        return (E)h.v;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        E e1 = E.RED;
        E e2 = E.BLUE;

        EClass ec = new EClass();
        ec.e = E.RED;

        EStruct es = new EStruct();
        es.e = E.BLUE;

        bool true0 = E.RED.HasFlag(E.RED);
        bool true1 = e1.HasFlag(GetE());
        bool true2 = ArrayOfE[0].HasFlag(E.RED);
        bool true3 = StaticE.HasFlag(E.BLUE);
        bool true4 = ec.e.HasFlag(e1);
        bool true5 = e2.HasFlag(es.e);
        bool true6 = e1.HasFlag(E.ZERO);
        bool true7 = e2.HasFlag(E.ZERO);

        bool false0 = E.BLUE.HasFlag(E.RED);
        bool false1 = e1.HasFlag(e2);
        bool false2 = GetE().HasFlag(e2);
        bool false3 = E.RED.HasFlag(ArrayOfE[1]);
        bool false4 = E.BLUE.HasFlag(ec.e);
        bool false5 = ByrefE(ref e1, StaticE);

        bool true8 = StaticE.HasFlag(GetESideEffect());
        bool false6 = GetESideEffect().HasFlag(StaticE);

        bool false7 = F.RED.HasFlag(F.BLUE);
        bool false8 = G.RED.HasFlag(G.BLUE);

        F f1 = F.RED;
        bool false9 = ByrefF(ref f1, F.BLUE);

        G g1 = G.RED;
        bool true9 = ByrefG(ref g1, G.RED);

        bool false10 = GitHub23847(E.RED, 0x100);

        bool[] trueResults = {true0, true1, true2, true3, true4, true5, true6, true7, true8, true9};
        bool[] falseResults = {false0, false1, false2, false3, false4, false5, false6, false7, false8, false9, false10};

        bool resultOk = true;

        int i = 0;
        foreach (var b in trueResults)
        {
            i++;
            if (!b) 
            {
                Console.WriteLine("true{0} failed\n", i);
                resultOk = false;
            }
        }

        i = 0;
        foreach (var b in falseResults)
        {
            i++;
            if (b) 
            {
                Console.WriteLine("false{0} failed\n", i);
                resultOk = false;
            }
        }

        // Optimization should bail on this case which causes an exception.
        bool didThrow = false;
        try
        {
            bool badFlag = E.RED.HasFlag((Enum) F.RED);
        }
        catch (ArgumentException)
        {
            didThrow = true;
        }

        if (!didThrow)
        {
            Console.WriteLine("exception case failed\n");
            resultOk = false;
        }

        return resultOk ? 100 : 0;
    }
}
