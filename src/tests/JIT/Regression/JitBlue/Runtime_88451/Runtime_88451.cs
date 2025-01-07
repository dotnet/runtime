// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_88451
{
    [Fact]
    public static int TestEntryPoint()
    {
        return Math.Tanh(double.NegativeInfinity) == -1 ? 100 : 101;
    }
}
