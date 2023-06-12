// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Getter and Setter, simple, compiled debug+, both should NOT be inlined.

using System;
using Xunit;
public class A
{
    private int _prop;
    public int prop
    {
        get { return _prop; }
        set { _prop = value; }
    }
}
public class debug
{
    [Fact]
    public static int TestEntryPoint()
    {
        A a = new A();
        a.prop = 100;
        int retval = a.prop;
        return retval;
    }
}
