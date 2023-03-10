// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;
public class IntrinsicTest
{
    private static int s_counter;
    private int _instanceCounter;
    private static long s_counter64;
    private long _instanceCounter64;
    private static int s_id_counter;
    private int _id_instanceCounter;
    private static long s_id_counter64;
    private long _id_instanceCounter64;
    private static long s_temp = 1111;

    private static long s_idmp = 1111;
    private static long s_idjunk = 0;
    [MethodImpl(MethodImplOptions.NoInlining)]

    public int GetValue() { s_temp++; return (int)0x1ceddeed; }

    [MethodImpl(MethodImplOptions.NoInlining)]

    public int id_GetValue() { s_idmp++; return (int)0x1ceddeed; }

    public static bool MainTest()
    {
        int te0 = 3245, te1 = 4325, te2 = 4325, te3 = 2134;
        long te064 = 454562, te164 = 345653, te264 = 345653, te364 = 345564;
        int dummy = 4355;
        long dummy64 = 656342;
        int id0 = 3245, id1 = 4325, id2 = 4325, id3 = 2134;
        long id064 = 454562, id164 = 345653, id264 = 345653, id364 = 345564;
        int idummy = 4355;
        long idummy64 = 656342;
        bool fail = false;
        IntrinsicTest Dummy = new IntrinsicTest();
        te0 = Interlocked.CompareExchange(ref te0, Dummy.GetValue(), te0);
        id0 = id0; if ((id0 == id0)) s_idjunk = Dummy.id_GetValue(); else s_idjunk = Dummy.id_GetValue();
        Console.WriteLine("------------------------------------CMPXCHG  0 3 0 0");
        if (te0 != id0) { fail = true; Console.WriteLine("te0 check failed {0} {1}", te0, id0); }
        if (te1 != id1) { fail = true; Console.WriteLine("te1 check failed {0} {1}", te1, id1); }
        if (te2 != id2) { fail = true; Console.WriteLine("te2 check failed {0} {1}", te2, id2); }
        if (te3 != id3) { fail = true; Console.WriteLine("te3 check failed {0} {1}", te3, id3); }
        if (te064 != id064) { fail = true; Console.WriteLine("te064 check failed {0} {1}", te064, id064); }
        if (te164 != id164) { fail = true; Console.WriteLine("te164 check failed {0} {1}", te164, id164); }
        if (te264 != id264) { fail = true; Console.WriteLine("te264 check failed {0} {1}", te264, id264); }
        if (te364 != id364) { fail = true; Console.WriteLine("te364 check failed {0} {1}", te364, id364); }
        if (dummy != idummy) { fail = true; Console.WriteLine("dummy check failed {0} {1}", dummy, idummy); }
        if (dummy64 != idummy64) { fail = true; Console.WriteLine("dummy64 check failed {0} {1}", dummy64, idummy64); }

        if (s_counter != s_id_counter) { Console.WriteLine("counter mismatch {0} {1}", s_counter, s_id_counter); fail = true; }
        if (s_counter64 != s_id_counter64) { Console.WriteLine("counter64 mismatch {0} {1}", s_counter64, s_id_counter64); fail = true; }
        if (Dummy._instanceCounter != Dummy._id_instanceCounter) { Console.WriteLine("instanceCounter mismatch {0} {1}", Dummy._instanceCounter, Dummy._id_instanceCounter); fail = true; }
        if (Dummy._instanceCounter64 != Dummy._id_instanceCounter64) { Console.WriteLine("instanceCounter64 mismatch {0} {1}", Dummy._instanceCounter64, Dummy._id_instanceCounter64); fail = true; }
        if (s_temp != s_idmp) { Console.WriteLine("temp mismatch {0} {1}", s_temp, s_idmp); fail = true; }
        return fail;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        if (MainTest())
        {
            Console.WriteLine("Test Failed");
            return 101;
        }
        else
        {
            Console.WriteLine("Test Passed");
            return 100;
        }
    }
}

