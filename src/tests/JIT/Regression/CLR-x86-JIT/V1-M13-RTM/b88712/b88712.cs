// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b88712;

using System;
using Xunit;

public struct AA
{
    public static void Static5()
    {
        float a = 125.0f;
        a += (a *= 60.0f);
    }
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        Static5();
    }
}
