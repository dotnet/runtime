// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// this is regression test for VSW 451034
// ngening the assembly and running it resulted in AV

using System;
using Xunit;
using TestLibrary;

public class Test_LoadType
{
    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TestEntryPoint()
    {
        S s = CReloc5<char>.s;
            
        Console.WriteLine("PASS");
    }
}
