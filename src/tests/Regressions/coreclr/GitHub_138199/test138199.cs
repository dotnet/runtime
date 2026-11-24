// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

public static class Runtime_138199
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1, CanonicalActivation.Test.Run());
    }
}
