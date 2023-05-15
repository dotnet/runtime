// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// The bug captured by this test was an ARM failure where:
// - We have a fairly large frame going into the backend, but still not enough to trigger]
//   the need to reserve REG_OPT_RSVD.
// - The backend (decomposition in particular) generates many new locals, making the
//   frame large enough to require REG_OPT_RSVD.
// - The bug was that the analysis was being done prior to decomposition and lowering,
//   thus missing this case.
// - The fix was to move this analysis just prior to actual register allocation.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class DevDiv_545504
{
    public const int Pass = 100;
    public const int Fail = -1;

    struct Struct_64bytes
    {
        long m_l0;
        long m_l1;
        long m_l2;
        long m_l3;
        long m_l4;
        long m_l5;
        long m_l6;
        long m_l7;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public Struct_64bytes(long l)
        {
            m_l0 = l;
            m_l1 = l + 1;
            m_l2 = l + 2;
            m_l3 = l + 3;
            m_l4 = l + 4;
            m_l5 = l + 5;
            m_l6= l + 6;
            m_l7= l + 7;
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public long Compute()
        {
            long result = ((m_l0 << (int)m_l1) >> (int)(m_l2 / 2)) +
                          ((m_l3 << (int)m_l4) >> (int)(m_l5 / 2)) +
                          m_l6 + m_l7;
            return result;
        }
    }

    struct Struct_512bytes
    {
        Struct_64bytes m_s0;
        Struct_64bytes m_s1;
        Struct_64bytes m_s2;
        Struct_64bytes m_s3;
        Struct_64bytes m_s4;
        Struct_64bytes m_s5;
        Struct_64bytes m_s6;
        Struct_64bytes m_s7;

        [MethodImplAttribute(MethodImplOptions.NoInlining)]
        public Struct_512bytes(long l)
        {
            m_s0 = new Struct_64bytes(l);
            m_s1 = new Struct_64bytes(l + 1);
            m_s2 = new Struct_64bytes(l + 2);
            m_s3 = new Struct_64bytes(l + 3);
            m_s4 = new Struct_64bytes(l + 4);
            m_s5 = new Struct_64bytes(l + 5);
            m_s6 = new Struct_64bytes(l + 6);
            m_s7 = new Struct_64bytes(l + 7);
        }

        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public long Compute()
        {
            long result = ((m_s0.Compute() << (int)m_s1.Compute()) >> (int)(m_s2.Compute() / 2)) +
                          ((m_s3.Compute() << (int)m_s4.Compute()) >> (int)(m_s5.Compute() / 2)) +
                          m_s6.Compute() + m_s7.Compute();
            return result;
        }
    }

    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long test(int count)
    {
        Struct_512bytes s0 = new Struct_512bytes(0);
        Struct_512bytes s1 = new Struct_512bytes(1);
        Struct_512bytes s2 = new Struct_512bytes(2);
        Struct_512bytes s3 = new Struct_512bytes(3);
        Struct_512bytes s4 = new Struct_512bytes(4);
        Struct_512bytes s5 = new Struct_512bytes(5);

        long result = ((s0.Compute() << (int)s1.Compute()) >> (int)(s2.Compute() / 2)) +
                      ((s3.Compute() << (int)s4.Compute()) >> (int)(s5.Compute() / 2));

        Console.WriteLine("Result: " + result);
        return result;
    }
    [Fact]
    public static int TestEntryPoint()
    {
        int result = (int)test(10);
        if (result != 267386880)
        {
            return Fail;
        }
        return Pass;
    }
}
