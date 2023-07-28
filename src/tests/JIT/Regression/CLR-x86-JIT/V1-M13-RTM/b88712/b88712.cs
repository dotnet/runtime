// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public struct AA
{
    public static void Static5()
    {
        float a = 125.0f;
        a += (a *= 60.0f);
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Static5();
        return 100;
    }
}
