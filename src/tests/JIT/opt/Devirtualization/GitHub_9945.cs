// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Base
{
    public int value;
    public void B0() { value += 12; }
    public virtual void B1() { value += 33; }
}

// Ensure that D1 and D2 have enough virtuals that the slot number for
// the virtual M1 is greater than any virtual's slot number in B.

public class D1 : Base
{
    public virtual void MA() { }
    public virtual void MB() { }
    public virtual void MC() { }
    public virtual void MD() { }

    public virtual void M1() { value += 44; }
}

public class D2 : Base
{
    public virtual void MA() { }
    public virtual void MB() { }
    public virtual void MC() { }
    public virtual void MD() { }

    public virtual void M1() { value += 55; }
}

// Aggressive use of 'dup' here by CSC will confuse the jit, and it
// may substitute 'b' for uses of d1 and d2. This is not
// value-incorrect but loses type information.
//
// This loss of type information subsequently triggers an assert in
// devirtualzation because b does not have M1 as virtual method.

public class Test
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestEntry(bool arg)
    {
        Base b;

        if (arg)
        {
            D1 d1 = new D1();
            b = d1;
            d1.B1();
            d1.M1();
        }
        else
        {
            D2 d2 = new D2();
            b = d2;
            d2.B1();
            d2.M1();
        }

        b.B0();
        return b.value;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return TestEntry(false);
    }
}
