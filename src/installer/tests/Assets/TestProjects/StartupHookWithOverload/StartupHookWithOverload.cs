// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // Success case with a startup hook that contains multiple
        // Initialize methods. This is used to check that the startup
        // hook provider doesn't get confused by the presence of an
        // extra Initialize method with an incorrect signature.
        Initialize(123);
    }

    public static void Initialize(int input)
    {
        Console.WriteLine("Hello from startup hook with overload! Input: " + input);
    }
}
