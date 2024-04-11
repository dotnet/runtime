// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

class Program
{
    internal static int s_return;

    [DynamicDependency(nameof(StartupHook.Initialize), typeof(StartupHook))]
    static int Main() => s_return;
}

class StartupHook
{
    public static void Initialize()
    {
        Console.WriteLine("Running startup hook");
        Program.s_return = 100;
    }
}
