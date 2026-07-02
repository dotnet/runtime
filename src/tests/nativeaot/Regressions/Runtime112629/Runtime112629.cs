// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

extern alias AssemblyWithDot;
extern alias AssemblyWithUnderscore;

class Program
{
    [Fact]
    public static int TestEntryPoint()
    {
        if (new AssemblyWithDot::TestNamespace.TestType().Value != "A.B")
            return 101;

        if (new AssemblyWithUnderscore::TestNamespace.TestType().Value != "A_B")
            return 102;

        return 100;
    }
}
