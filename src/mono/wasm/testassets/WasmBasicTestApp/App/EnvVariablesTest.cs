// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;

public partial class EnvVariablesTest
{
    [JSExport]
    public static int DumpVariables()
    {
        // enumerate all environment variables
        foreach (string key in Environment.GetEnvironmentVariables().Keys)
        {
            Console.WriteLine($"{key}={Environment.GetEnvironmentVariable(key)}");
        }

        return 42;
    }
}
