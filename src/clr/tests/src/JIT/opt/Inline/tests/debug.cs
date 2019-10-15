// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Getter and Setter, simple, compiled debug+, both should NOT be inlined.

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
internal class debug
{
    public static int Main()
    {
        A a = new A();
        a.prop = 100;
        int retval = a.prop;
        return retval;
    }
}
