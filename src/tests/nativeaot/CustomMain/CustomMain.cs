// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Program
{
    // Each of the module initializer, class constructor, and IncrementExitCode
    // should be executed exactly once, causing this to each 100 by program exit.
    static int s_exitCode;

    [ModuleInitializer]
    internal static void InitializeModule()
    {
        s_exitCode += 8;
    }

    static Program()
    {
        s_exitCode += 31;
        // A side-effecting operation to prevent this cctor from being pre-inited at compile time.
        Console.WriteLine("hello from static constructor");
    }

    [UnmanagedCallersOnly(EntryPoint = "IncrementExitCode", CallConvs = new Type[] { typeof(CallConvCdecl) })]
    static void IncrementExitCode(int amount)
    {
        s_exitCode += amount;
    }

    int ExitCode;

    [Fact]
    public static int TestEntryPoint()
    {
        Console.WriteLine("hello from managed main");
        return s_exitCode;
    }
}
