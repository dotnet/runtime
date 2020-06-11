// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class StartupHook
{
    public void Initialize()
    {
        // This hook should not be called because it's an instance
        // method. Instead, the startup hook provider code should
        // throw an exception.
        Console.WriteLine("Hello from startup hook with instance method!");
    }
}
