// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    private static int CallCount { get; set; }

    private static void Initialize()
    {
        CallCount++;
        Console.WriteLine($"-- Hello from startup hook with non-public method! Call count: {CallCount}");
    }
}
