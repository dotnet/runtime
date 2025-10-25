// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


namespace b00735;

using System;
using Xunit;
public struct AA
{
    static void f()
    {
        bool flag = false;
        if (flag)
        {
            while (flag)
            {
                while (flag) { }
            }
        }
        do { } while (flag);
    }
    [OuterLoop]
    [Fact]
    public static void TestEntryPoint()
    {
        f();
    }
}
