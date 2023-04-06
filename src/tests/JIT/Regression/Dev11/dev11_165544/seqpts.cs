// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

/* 
 * Ensure that for start of sequence points that are also start of NOGC interrupt regions, an interruptible NOP is placed in front for the sequence point.
 *
 * The test does not check for the above in the JIT tree but only functional correctness.
 */
// csc /o- /debug+
using System;
using Xunit;

struct BigCopy
{
    public long l1, l2, l3;
    public object gc;
    public override string ToString()
    {
        return string.Format("l1 = {0}, l2 = {1}, l3 = {2}", l1, l2, l3);
    }
    public static BigCopy operator +(BigCopy c, long l)
    {
        c.l1 += l;
        c.l2 += l;
        c.l3 += l;
        return c;
    }
}

public static class Repro
{
    [Fact]
    public static int TestEntryPoint()
    {
        BigCopy b1, b2, b3;
        b1.gc = "me";
        b1.l1 = 1;
        b1.l2 = 2;
        b1.l3 = 3;
        b2 = b1;
        b2 += 3;
        b3 = b2;
        b3 += 3;
        Console.WriteLine("b1 = {0}", b1);
        Console.WriteLine("b2 = {0}", b2);
        Console.WriteLine("b3 = {0}", b3);
        if (b1.l1 == 1 && b1.l2 == 2 && b1.l3 == 3 &&
           b2.l1 == 4 && b2.l2 == 5 && b2.l3 == 6 &&
           b3.l1 == 7 && b3.l2 == 8 && b3.l3 == 9) return 100;
        return 101;
    }
}
