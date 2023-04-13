// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public struct S
{
    public int i;
    public int j;
}

public class Test_GitHub_27027
{
    public S s;

    [Fact]
    public static int TestEntryPoint()
    {
        // Test that the correct exception is thrown from Run.
        // The bug was that the exceptions were reordered and DivideByZeroException
        // was thrown instead of NullReferenceException.
        try {
            Run(null, 0);
        }
        catch (System.NullReferenceException)
        {
            return 100;
        }

        return -1;
    }
 
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Run(Test_GitHub_27027 test, int j)
    {
        int k = test.s.i + 1/j;
        return k;
    }
}
