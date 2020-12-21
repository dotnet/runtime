// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    static void Initialize()
    {
        // Success case with a startup hook that is a private method.
        Console.WriteLine("Hello from startup hook with non-public method!");
    }
}
