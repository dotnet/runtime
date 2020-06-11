// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class StartupHook
{
    static void Initialize()
    {
        // Success case with a startup hook that is a private method.
        Console.WriteLine("Hello from startup hook with non-public method!");
    }
}
