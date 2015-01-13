// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using Xunit;

public static class Stack_Contains_obj
{
    [Fact]
    public static void ContainsStraightForwardTests()
    {
        Stack stk = new Stack();

        for (int i = 0; i < 100; i++)
        {
            stk.Push(i);
        }

        Assert.Equal(stk.Count, 100);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(stk.Contains(i));
        }

        Assert.False(stk.Contains(150));
        Assert.False(stk.Contains("Hello World"));
        Assert.False(stk.Contains(null));

        stk.Push(null);
        Assert.True(stk.Contains(null));
    }

    [Fact]
    public static void ContainsStraightForwardApiSynchronizedTests()
    {
        Stack stk = new Stack();
        stk = Stack.Synchronized(stk);

        for (int i = 0; i < 100; i++)
        {
            stk.Push(i);
        }

        Assert.Equal(stk.Count, 100);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(stk.Contains(i));
        }

        Assert.False(stk.Contains(150));
        Assert.False(stk.Contains("Hello World"));
        Assert.False(stk.Contains(null));

        stk.Push(null);
        Assert.True(stk.Contains(null));
    }
}
