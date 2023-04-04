// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    private static int CallCount { get; set; }

    public static void Initialize()
    {
        // Normal success case with a simple startup hook.
        Initialize(123);
    }

    public static void Initialize(int input)
    {
        CallCount++;
        Console.WriteLine($"-- Hello from startup hook with overload! Call count: {CallCount}");
    }
}
