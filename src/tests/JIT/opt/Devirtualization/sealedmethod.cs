// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Base
{
    public virtual int GetValue(int value)
    {
        return 0x33;
    }
}

public class Derived : Base
{
    public sealed override int GetValue(int value)
    {
        return value;
    }
}

public class F
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int TestSealedMethodInlining(Derived obj)
    {
        return obj.GetValue(3);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Derived d = new Derived();
        int v = TestSealedMethodInlining(d);
        return (v == 3 ? 100 : -1);
    }
}
