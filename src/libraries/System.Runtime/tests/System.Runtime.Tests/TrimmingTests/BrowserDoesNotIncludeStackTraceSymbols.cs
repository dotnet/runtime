// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;


/// <summary>
/// Tests that StackTraceSymbols and dependencies are not included in a trimmed app targeting browser.
/// The idea is that the runtime should not depend on these types when running in a browser with DebugSymbols == false.
/// Motivation: application size.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        var symbolsType = GetTypeByName("StackTraceSymbols");
        if (symbolsType != null)
        {
            Console.WriteLine("Failed StackTraceSymbols");
            return -1;
        }
        return 100;
    }

    // make reflection trimmer incompatible
    static Type GetTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var type = assembly.GetType("System.Diagnostics." + typeName);
            if (type != null)
            {
                return type;
            }
        }
        return null;
    }
}