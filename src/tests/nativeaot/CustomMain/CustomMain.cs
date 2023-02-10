// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

class Program
{
    static int s_exitCode;

    [ModuleInitializer]
    internal static void InitializeModule()
    {
        s_exitCode += 50;
    }

    [UnmanagedCallersOnly(EntryPoint = "IncrementExitCode", CallConvs = new Type[] { typeof(CallConvCdecl) })]
    static void IncrementExitCode(int amount)
    {
        s_exitCode += amount;
    }

    int ExitCode;

    static int Main(string[] args)
    {
        Console.WriteLine("hello from managed code");
        return s_exitCode;
    }
}
