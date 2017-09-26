// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

public static partial class XmlSerializerTests
{
    //This is a dummy test that runs on all platform. It is to make sure we have at least one test running in UWP platform, which will exclude all other SGENTESTS.
    [Fact]
    public static void AlwaysPassTest()
    {
        Assert.True(true);
    }
}
