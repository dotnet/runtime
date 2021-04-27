// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;

struct MyValueType
{
    object o;
}

abstract class Test
{
    public abstract void M(MyValueType v);

    static int Main()
    {
        new Concrete().M(default);
        return 100;
    }
}

class Concrete : Test
{
    public override void M(MyValueType v)
    {
        new Vector<double>().ToString();
    }
}
