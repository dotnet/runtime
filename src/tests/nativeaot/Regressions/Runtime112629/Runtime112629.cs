// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias AssemblyWithDot;
extern alias AssemblyWithUnderscore;

class Program
{
    public static int Main()
    {
        if (new AssemblyWithDot::TestNamespace.TestType().Value != "A.B")
            return 101;

        if (new AssemblyWithUnderscore::TestNamespace.TestType().Value != "A_B")
            return 102;

        return 100;
    }
}
