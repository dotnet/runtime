// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace System.Security.Cryptography.Tests;

public sealed class CngPropertyTests
{
    [Fact]
    public void TestConstructorSpan()
    {
        var name = "dotnet-test";
        ReadOnlySpan<byte> value = new byte[12];
        value[5] = 1;
        value[6] = 2;
        value[7] = 3;

        _ = new CngProperty(name, value, CngPropertyOptions.CustomProperty);
    }

    [Fact]
    public void TestConstructorSpan_NameNull()
    {
        var name = "dotnet-test";
        var value = new byte[12];
        value[5] = 1;
        value[6] = 2;
        value[7] = 3;

        Assert.Throws<ArgumentNullException>(() =>
        {
            ReadOnlySpan<byte> span = value;
            _ = new CngProperty(name, span, CngPropertyOptions.CustomProperty);
        })
    }
}
