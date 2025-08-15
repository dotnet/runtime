// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Numerics;

public class Foo
{
    public Vector3 v1;
    public Vector3 v2;
}

public class Runtime_110820
{
    [Fact]
    public static void TestEntryPoint()
    {
        var foo = new Foo();
        foo.v2 = new Vector3(4, 5, 6);
        foo.v1 = new Vector3(1, 2, 3);
        Assert.Equal(new Vector3(1, 2, 3), foo.v1);
        Assert.Equal(new Vector3(4, 5, 6), foo.v2);
    }
}
