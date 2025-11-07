// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace b92714;

using System;
using Xunit;
public struct AA
{
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        bool local3 = false;
        do
        {
            while (local3) { }
        } while (local3);
    }
}
