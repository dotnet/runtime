// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b91859;

using System;
using Xunit;
public class AA
{
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        bool b = false;
        b = (b ? (object)b : (object)new AA()) ==
            (b ? new AA() : (b ? new AA() : null));
    }
}
