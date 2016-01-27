// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

using Xunit;

public class EmptyTests
{
    [Fact]
    public static void TestEmpty()
    {
        Assert.True(Array.Empty<int>() != null);
        Assert.Equal(0, Array.Empty<int>().Length);
        Assert.Equal(1, Array.Empty<int>().Rank);
        Assert.Equal(Array.Empty<int>(), Array.Empty<int>());

        Assert.True(Array.Empty<object>() != null);
        Assert.Equal(0, Array.Empty<object>().Length);
        Assert.Equal(1, Array.Empty<object>().Rank);
        Assert.Equal(Array.Empty<object>(), Array.Empty<object>());
    }

    [Fact]
    public static void TestTypeStaticFields()
    {
        Assert.NotNull(Type.EmptyTypes);
        Assert.NotNull(Type.Missing);

        // Assert singletons return the same instance
        var et = Type.EmptyTypes;
        Assert.Equal(et, Type.EmptyTypes);
        var ms = Type.Missing;
        Assert.Equal(ms, Type.Missing);
    }
}
