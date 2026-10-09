// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// On a Checked runtime under the CoreCLR interpreter, this test asserted Thread::IsObjRefValid in OBJECTREF::operator->
// at an interpreted virtual call: CreateCustomAttributeInstance registers its argument storage, a local on the interpreter
// stack, for GC reporting, and GCFrame::Remove used to record those slots as unprotected object references when the
// storage was unregistered. A later virtual call whose 'this' argument landed on one of those addresses then tripped
// the check. Reaching the assert depends on the stack layout: it reproduced on windows-x64 under DOTNET_InterpMode 2
// and 3 and passes elsewhere, so the test is process-isolated to keep the layout it was measured with.

[AttributeUsage(AttributeTargets.Class)]
sealed class FourArgsAttribute : Attribute
{
    public FourArgsAttribute(int a, int b, int c, int d) { }
}

[FourArgs(1, 2, 3, 4)]
sealed class Decorated { }

abstract class Base { public abstract int Get(); }
sealed class Derived : Base { public override int Get() => 1; }

public class Runtime_135535
{
    static Base s_target = new Derived();

    [Fact]
    public static void TestEntryPoint()
    {
        // Warm up the paths used below, to reduce later prestub activity (which flushes the debug table).
        typeof(Decorated).GetCustomAttributes(false);
        GC.Collect();
        for (int d = 0; d < 4; d++)
            Walk(1, 1, 1, d);

        // 1. CreateCustomAttributeInstance registers its argument storage for GC reporting and unregisters it.
        typeof(Decorated).GetCustomAttributes(false);
        // 2. A possible GC point marks the recorded addresses (other transitions can do this too).
        GC.Collect();
        // 3. Virtual calls whose 'this' argument lands on the interpreter stack at many addresses above this frame:
        //    the recursion depths vary the frame the call is made from, the leaf variants vary the slot within it.
        for (int a = 0; a < 200; a++)
            for (int b = 0; b < 4; b++)
                for (int c = 0; c < 4; c++)
                    for (int d = 0; d < 4; d++)
                        Walk(a, b, c, d);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Walk(int a, int b, int c, int d) => a > 0 ? Walk(a - 1, b, c, d) : Walk2(b, c, d);

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Walk2(int b, int c, int d)
    {
        long x = b * 3L, y = c * 5L;
        return b > 0 ? Walk2(b - 1, c, d) + (int)(x - y) : Walk3(c, d) + (int)(y - x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Walk3(int c, int d)
    {
        Guid g = new Guid(c, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10);
        if (c > 0)
            return Walk3(c - 1, d) + g.GetHashCode() * 0;
        switch (d)
        {
            case 0: return Leaf0();
            case 1: return Leaf1();
            case 2: return Leaf2();
            default: return Leaf3();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Leaf0() => s_target.Get();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Leaf1()
    {
        long x = 1;
        return s_target.Get() + (int)x;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Leaf2()
    {
        long x = 1, y = 2;
        return s_target.Get() + (int)(x + y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Leaf3()
    {
        long x = 1, y = 2, z = 3;
        return s_target.Get() + (int)(x + y + z);
    }
}
