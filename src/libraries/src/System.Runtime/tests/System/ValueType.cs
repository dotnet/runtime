// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

public static class ValueTypeTests
{
    [Fact]
    public static void TestToString()
    {
        object obj = new S();
        Assert.Equal(obj.ToString(), obj.GetType().ToString());
        Assert.Equal("ValueTypeTests+S", obj.ToString());
    }

    public struct S
    {
        public int x;
        public int y;
    }
}
