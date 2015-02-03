// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

public static unsafe class ValueTypeTests
{
    [Fact]
    public static void TestToString()
    {
        Object o = new S();
        String s = o.ToString();
        Assert.NotNull(s);
        String s1 = o.GetType().ToString();
        Assert.Equal(s, s1);
        Assert.Equal("ValueTypeTests+S", s);
    }

    public struct S
    {
        public int x;
        public int y;
    }
}

