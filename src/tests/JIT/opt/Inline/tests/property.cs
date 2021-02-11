// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Getter and Setter, simple, both should be inlined.

using System;
public class A
{
    private int _prop;
    public int prop
    {
        get { return _prop; }
        set { _prop = value; }
    }
}
internal class Property
{
    public static int Main()
    {
        A a = new A();
        a.prop = 100;
        int retval = a.prop;
        return retval;
    }
}
