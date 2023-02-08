// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Xunit;

// Test case where a type-equvalent delegate is assigned

public class X
{
    static int F() => 3;

    [Fact]
    public static int TestEntryPoint()
    {
        XD x = F;
        XD y = Lib.GetDelegate();
        return x() + y() + 64;
    }
    
}
