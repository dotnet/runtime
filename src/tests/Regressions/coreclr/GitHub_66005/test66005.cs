// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;
using TestLibrary;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test<TException>() where TException : Exception
    {
        try
        {
            throw new InvalidOperationException();
        }
        catch (TException)
        {
            return;
        }
    }

    [ActiveIssue("needs triage", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]
    [Fact]
    public static void TestEntryPoint()
    {
        Test<InvalidOperationException>();
    }
}
