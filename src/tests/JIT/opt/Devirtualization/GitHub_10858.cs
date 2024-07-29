// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

class B 
{
    public virtual string F() { return "B"; }
}

sealed class D : B
{
    public override string F() { return "D"; }
}

sealed class E : B
{
    public override string F() { return "E"; }
}

public class X
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Test(bool arg)
    {
        // When optimizing IL, CSC will leave the newobj's on the stack
        // across the branches to the common def point.
        B b1 = arg ? (B)new E() : (B)new D();
        B b2 = null;

        // Conditional flow here to forces b1 to a local instead of
        // remaining on the stack. So we have a single def point with
        // two reaching values.
        if (arg)
        {
            b2 = new D();
        }
        else
        {
            b2 = new E();
        }

        // We should not be able to devirtualize either call.
        return b2.F()[0] + b1.F()[0] - 37;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Test(false);
    }
}
