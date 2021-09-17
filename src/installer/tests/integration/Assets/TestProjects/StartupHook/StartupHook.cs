// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // Normal success case with a simple startup hook.
        Console.WriteLine("Hello from startup hook!");
    }
}
