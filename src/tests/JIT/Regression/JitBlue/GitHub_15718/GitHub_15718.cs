// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        var map = new Dictionary<string, bool?> { { "foo", true } };
        return (Test(map) == true) ? 100 : 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool? Test(Dictionary<string, bool?> map)
    {
        return map["foo"];
    }
}
