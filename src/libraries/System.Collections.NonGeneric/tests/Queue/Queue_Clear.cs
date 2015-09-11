// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Globalization;
using Xunit;

public class Queue_Clear
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Clear_Empty(int capacity)
    {
        var q = new Queue(capacity);
        Assert.Equal(0, q.Count);
        q.Clear();
        Assert.Equal(0, q.Count);
    }
}
