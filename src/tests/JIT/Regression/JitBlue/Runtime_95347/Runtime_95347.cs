// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Runtime_95347
{
    [Fact]
    public static int Test()
    {
        object? n = "abcd";
        var s = (n ?? false).ToString();
        return (s == "abcd") ? 100 : -1;
    }
}
