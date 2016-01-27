// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
