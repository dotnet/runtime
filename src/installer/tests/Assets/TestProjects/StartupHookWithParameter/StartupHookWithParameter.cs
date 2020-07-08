// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    public static void Initialize(int input)
    {
        // This hook should not be called because it takes a
        // parameter. Instead, the startup hook provider code should
        // throw an exception.
        Console.WriteLine("Hello from startup hook taking int! Input: " + input);
    }
}
