// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b89506;

using System;
using Xunit;
public class AA
{
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        Main1();
    }

    internal static void Main1()
    {
        (new float[1, 1, 1, 1])[0, 0, 0, 0] -= (new float[1, 1])[0, 0];
    }
}
