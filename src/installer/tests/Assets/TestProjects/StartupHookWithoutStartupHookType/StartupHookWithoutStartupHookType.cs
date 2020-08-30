// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHookWrongType
{
    public static void Initialize()
    {
        // This hook should not be called because it doesn't have the
        // correct type name (StartupHook). Instead, the startup hook
        // provider code should throw an exception.
        Console.WriteLine("Hello from startup hook!");
    }
}
