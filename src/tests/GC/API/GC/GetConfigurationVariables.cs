// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/* 
 * Tests ReadConfigurationVariables() 
 */

using System;
public class Test_GetConfigurationVariables
{
    public static int Main()
    {
        // A simple smoke test to ensure there are no exceptions.
        var configurations = GC.GetConfigurationVariables();
        Console.WriteLine($"Configuration Count: {configurations.Count}");
        return 100;
    }
}
