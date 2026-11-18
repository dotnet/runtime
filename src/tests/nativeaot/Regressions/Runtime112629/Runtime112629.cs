// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias AssemblyWithDot;
extern alias AssemblyWithUnderscore;

using Xunit;

class Program
{
    public static int Main()
    {
        Assert.Equal("A.B", new AssemblyWithDot::TestNamespace.TestType().Value);
        Assert.Equal("A_B", new AssemblyWithUnderscore::TestNamespace.TestType().Value);
        return 100;
    }
}
