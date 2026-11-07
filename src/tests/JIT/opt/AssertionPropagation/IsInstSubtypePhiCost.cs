// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for dotnet/runtime#133857: assertion propagation walking PHI
// definitions for an `isinst`/`chkcast` had no memoisation, so the cost was
// O(fan-in ^ depth). Nine 8-way switches merging into one object, then a single
// `if (o is A a)`, took 10.9 s and 629 MB of JIT arena. With the memo it is ~1 ms
// and ~57 MB.
//
// Two different things are checked:
//
//   * NineMerges is the cost test. Its time bound is the only part of this file
//     that fails against the unfixed JIT -- without it the test still passes,
//     it just takes ~11 s and silently eats CI memory. The bound is deliberately
//     loose (the fixed JIT is ~1 ms), so a slow machine, a Checked JIT or a
//     stress mode cannot trip it.
//   * LoopCarried / LoopCarriedWithSwitches guard the cyclic-PHI path: a
//     loop-carried PHI closes a cycle, the recursion budget fires, and the same
//     key legitimately has different answers at different remaining depths. They
//     also carry a null edge and a non-A edge into the cast, because a proof that
//     leaked across contexts is only observable that way: the fold deletes the
//     isinst together with its null test, so the null edge throws and the B edge
//     reads B.Y where A.X belongs. They cannot detect a *lost* fold -- the cast
//     succeeds either way, so the value is equal.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Xunit;

public class IsInstSubtypePhiCost
{
    public class A
    {
        public int X;
    }

    // A field at the same offset as A.X but holding a different value, so a fold that
    // wrongly treats a B as an A adds 1234 where the correct answer adds 0.
    public class B
    {
        public int Y = 1234;
    }

    // AggressiveOptimization on every reproducer is load-bearing: the walk only
    // runs with full optimizations, and the default tiered first call would
    // compile this as Tier0 and skip assertion propagation entirely.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int NineMerges(int x)
    {
        object o = new A();
        int acc = 0;
        switch ((x >> 0) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 1;
                break;
            case 2:
                acc += 2;
                break;
            case 3:
                acc += 3;
                break;
            case 4:
                acc += 4;
                break;
            case 5:
                acc += 5;
                break;
            case 6:
                acc += 6;
                break;
            case 7:
                acc += 7;
                break;
        }
        switch ((x >> 3) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 2;
                break;
            case 2:
                acc += 3;
                break;
            case 3:
                acc += 4;
                break;
            case 4:
                acc += 5;
                break;
            case 5:
                acc += 6;
                break;
            case 6:
                acc += 7;
                break;
            case 7:
                acc += 8;
                break;
        }
        switch ((x >> 6) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 3;
                break;
            case 2:
                acc += 4;
                break;
            case 3:
                acc += 5;
                break;
            case 4:
                acc += 6;
                break;
            case 5:
                acc += 7;
                break;
            case 6:
                acc += 8;
                break;
            case 7:
                acc += 9;
                break;
        }
        switch ((x >> 9) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 4;
                break;
            case 2:
                acc += 5;
                break;
            case 3:
                acc += 6;
                break;
            case 4:
                acc += 7;
                break;
            case 5:
                acc += 8;
                break;
            case 6:
                acc += 9;
                break;
            case 7:
                acc += 10;
                break;
        }
        switch ((x >> 12) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 5;
                break;
            case 2:
                acc += 6;
                break;
            case 3:
                acc += 7;
                break;
            case 4:
                acc += 8;
                break;
            case 5:
                acc += 9;
                break;
            case 6:
                acc += 10;
                break;
            case 7:
                acc += 11;
                break;
        }
        switch ((x >> 15) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 6;
                break;
            case 2:
                acc += 7;
                break;
            case 3:
                acc += 8;
                break;
            case 4:
                acc += 9;
                break;
            case 5:
                acc += 10;
                break;
            case 6:
                acc += 11;
                break;
            case 7:
                acc += 12;
                break;
        }
        switch ((x >> 18) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 7;
                break;
            case 2:
                acc += 8;
                break;
            case 3:
                acc += 9;
                break;
            case 4:
                acc += 10;
                break;
            case 5:
                acc += 11;
                break;
            case 6:
                acc += 12;
                break;
            case 7:
                acc += 13;
                break;
        }
        switch ((x >> 21) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 8;
                break;
            case 2:
                acc += 9;
                break;
            case 3:
                acc += 10;
                break;
            case 4:
                acc += 11;
                break;
            case 5:
                acc += 12;
                break;
            case 6:
                acc += 13;
                break;
            case 7:
                acc += 14;
                break;
        }
        switch ((x >> 24) & 7)
        {
            case 0:
                o = new A();
                break;
            case 1:
                acc += 9;
                break;
            case 2:
                acc += 10;
                break;
            case 3:
                acc += 11;
                break;
            case 4:
                acc += 12;
                break;
            case 5:
                acc += 13;
                break;
            case 6:
                acc += 14;
                break;
            case 7:
                acc += 15;
                break;
        }
        if (o is A a)
        {
            acc += a.X;
        }
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LoopCarried(int x, int n)
    {
        object o = new A();
        int acc = 0;
        for (int i = 0; i < n; i++)
        {
            if ((x & 1) == 0)
            {
                o = new A();
            }
            acc += i;
        }

        // A null and a B both reach the cast below, so the loop-carried value is not
        // provably an A. A fold that ignored that would delete the isinst and with it the
        // null test: the null edge dereferences null, and the B edge reads B.Y (1234)
        // where A.X (0) belongs. Correct behaviour is the same sum either way.
        if ((x & 0x100) != 0)
        {
            o = null;
        }
        else if ((x & 0x200) != 0)
        {
            o = new B();
        }

        if (o is A a)
        {
            acc += a.X;
        }
        return acc;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int LoopCarriedWithSwitches(int x, int n)
    {
        object o = new A();
        int acc = 0;
        for (int i = 0; i < n; i++)
        {
            switch ((x >> 0) & 7)
            {
                case 0:
                    o = new A();
                    break;
                case 1:
                    acc += 1;
                    break;
                case 2:
                    acc += 2;
                    break;
                case 3:
                    acc += 3;
                    break;
                case 4:
                    acc += 4;
                    break;
                case 5:
                    acc += 5;
                    break;
                case 6:
                    acc += 6;
                    break;
                case 7:
                    acc += 7;
                    break;
            }
            switch ((x >> 3) & 7)
            {
                case 0:
                    o = new A();
                    break;
                case 1:
                    acc += 2;
                    break;
                case 2:
                    acc += 3;
                    break;
                case 3:
                    acc += 4;
                    break;
                case 4:
                    acc += 5;
                    break;
                case 5:
                    acc += 6;
                    break;
                case 6:
                    acc += 7;
                    break;
                case 7:
                    acc += 8;
                    break;
            }
            switch ((x >> 6) & 7)
            {
                case 0:
                    o = new A();
                    break;
                case 1:
                    acc += 3;
                    break;
                case 2:
                    acc += 4;
                    break;
                case 3:
                    acc += 5;
                    break;
                case 4:
                    acc += 6;
                    break;
                case 5:
                    acc += 7;
                    break;
                case 6:
                    acc += 8;
                    break;
                case 7:
                    acc += 9;
                    break;
            }
        }

        // Same shape as LoopCarried: a null edge and a B edge feed the cast, so a fold
        // that ignored them is observable as a null dereference or as 1234 instead of 0.
        if ((x & 0x100) != 0)
        {
            o = null;
        }
        else if ((x & 0x200) != 0)
        {
            o = new B();
        }

        if (o is A a)
        {
            acc += a.X;
        }
        return acc;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        // Timed on the first call, which is the one that JITs the method.
        long start = Stopwatch.GetTimestamp();
        int result = NineMerges(1);
        long ms = (Stopwatch.GetTimestamp() - start) * 1000 / Stopwatch.Frequency;

        if (result != 1)
        {
            return 101; // wrong answer: the cast was folded incorrectly
        }
        if (ms > 2000)
        {
            return 102; // the PHI walk lost its memo and went exponential again
        }
        // LoopCarried's loop assigns o only when bit 0 is clear, so x=0 is what makes the
        // value loop-carried at all; 0x100 and 0x200 then add the poison edges.
        if (LoopCarried(0, 4) != 6)
        {
            return 103;
        }
        if (LoopCarried(0x100, 4) != 6)
        {
            return 105;
        }
        if (LoopCarried(0x200, 4) != 6)
        {
            return 106;
        }
        if (LoopCarriedWithSwitches(1, 4) != 4)
        {
            return 104;
        }
        if (LoopCarriedWithSwitches(0x101, 4) != 28)
        {
            return 107;
        }
        if (LoopCarriedWithSwitches(0x201, 4) != 4)
        {
            return 108;
        }
        return 100;
    }
}
