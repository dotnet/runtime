// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Reflection;

/// <summary>
/// Ensures setting NullabilityInfoContextSupport = false causes NullabilityInfoContext.Create to throw
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        MethodInfo testMethod = typeof(TestType).GetMethod("TestMethod")!;
        NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();
        try
        {
            nullabilityContext.Create(testMethod.ReturnParameter);
            return -1;
        }
        catch (InvalidOperationException)
        {
            return 100;
        }
    }
}

class TestType
{
    public static string? TestMethod()
    {
        return null;
    }
}
