// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal class StartupHook
{
    public static void Initialize()
    {
        // This startup hook can pass or fail depending on whether the
        // app comes with Newtonsoft.Json.
        Console.WriteLine("Hello from startup hook with dependency!");

        // A small operation involving NewtonSoft.Json to ensure the assembly is loaded properly
        var t = typeof(Newtonsoft.Json.JsonReader);
    }
}
