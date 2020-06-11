// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // Normal success case with a simple startup hook.
        Console.WriteLine("Hello from startup hook!");
    }
}
