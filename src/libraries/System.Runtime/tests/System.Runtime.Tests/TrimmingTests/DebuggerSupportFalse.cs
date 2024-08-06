// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Reflection;

/// <summary>
/// Ensures setting DebuggerSupport = false causes parameter names to be removed
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        // Ensure the method is kept
        MethodWithParameter ("");

        // Get parameter name via trim-incompatible reflection
        var parameterName = reflectedType.GetMethod("MethodWithParameter")!.GetParameters()[0].Name;

        // Parameter name should be removed
        if (!string.IsNullOrEmpty(parameterName))
            return -1;

        return 100;
    }

    static Type reflectedType = typeof(Program);

    public static void MethodWithParameter(string parameter) {}
}
