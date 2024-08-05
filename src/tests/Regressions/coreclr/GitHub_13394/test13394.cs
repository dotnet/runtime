// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using Xunit;

public struct MyValueType
{
    object o;
}

public abstract class Test_test13394
{
    public abstract void M(MyValueType v);

    [Fact]
    public static void TestEntryPoint()
    {
        new Concrete().M(default);
    }
}

class Concrete : Test_test13394
{
    public override void M(MyValueType v)
    {
        new Vector<double>().ToString();
    }
}
