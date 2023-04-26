// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_11343
{
    const int Passed = 100;
    const int Failed = 0;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Test()
    {
        string s = null;
        // Should throw NullReferenceException even if the result is not used
        int unused = s.Length;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        try
        {
            Test();
            return Failed;
        }
        catch (NullReferenceException)
        {
            return Passed;
        }
    }
}
