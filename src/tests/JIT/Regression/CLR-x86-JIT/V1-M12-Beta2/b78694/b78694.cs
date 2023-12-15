// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Foo
{
    [Fact]
    static public void TestEntryPoint()
    {
        double inf = Double.PositiveInfinity;
        System.Console.WriteLine(System.Math.Atan2(inf, inf));
        System.Console.WriteLine(System.Math.Atan2(inf, -inf));
        System.Console.WriteLine(System.Math.Atan2(-inf, inf));
        System.Console.WriteLine(System.Math.Atan2(-inf, -inf));
    }
}
