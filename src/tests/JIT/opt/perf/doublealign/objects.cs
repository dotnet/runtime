// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CMyException : System.Exception
{
}

public class Class1
{
    public Class1(double d1, byte b, double d2, Class1 c, double d3)
    {
        m_d1 = d1; m_b = b; m_d2 = d2; m_d3 = d3;
        _pLink = c;
    }
    public double m_d1;
    public byte m_b;
    public double m_d2;
#pragma warning disable 0414
    private Class1 _pLink;
#pragma warning restore 0414
    public double m_d3;
}

public class CTest
{
    private static int s_alignedCount = 0;
    private static int s_unalignedCount = 0;

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void UseShort(short x)
    {
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void UseByte(byte x)
    {
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe bool CheckDoubleAlignment(double* p)
    {
        bool aligned = (((int)p % sizeof(double)) == 0);

        if (aligned)
            s_alignedCount++;
        else
            s_unalignedCount++;

        return aligned;
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void TestObjects(short a, double b, byte c, double d)
    {
        short i16;
        byte i8;
        int i;
        Class1 prev = null;

        for (i = 0; i < 100; i++)
        {
            int beforeCount = GC.CollectionCount(0);
            Class1 c1 = new Class1(b, c++, d, prev, b + d);

            fixed (double* p1 = &c1.m_d1, p2 = &c1.m_d2, p3 = &c1.m_d3)
            {
                bool aligned = true;
                if (!CheckDoubleAlignment(p1)) aligned = false;
                if (!CheckDoubleAlignment(p2)) aligned = false;
                if (!CheckDoubleAlignment(p3)) aligned = false;

                if (!aligned)
                {
                    int afterCount = GC.CollectionCount(0);
                    Console.Write("Unaligned access.");
                    Console.Write(" beforeCount=" + beforeCount);
                    Console.Write(" afterCount=" + afterCount);
                    Console.WriteLine();
                }
            }

            i16 = (short)(c1.m_d1 + a);
            i8 = (byte)(c1.m_d2 + c1.m_d3 + c);

            UseShort(i16);
            UseByte(i8);
            prev = c1;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        TestObjects(1, 2, 3, 4);

        float maxUnalignmentsAllowed = (float)(s_unalignedCount + s_alignedCount) * 0.02f;

        Console.WriteLine("MaxUnalignmentsAllowed (2%): {0} UnalignedCount: {1} AlignedCount: {2}",
            maxUnalignmentsAllowed, s_unalignedCount, s_alignedCount);

        if (s_unalignedCount > maxUnalignmentsAllowed)
        {
            Console.WriteLine("!!!!!!!!! TEST FAILED !!!!!!!");
            return 101;
        }
        else
        {
            Console.WriteLine("!!!!!!!!! TEST PASSED !!!!!!!");
            return 100;
        }
    }
}
