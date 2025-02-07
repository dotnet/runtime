// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Runtime.CompilerServices;
using System.Runtime;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class Test_StackOverflow
{
    const int OsPageSizeInBytes = 4096; // 4KB per page

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int[] TestStack(int maxSizeOfStackFrameInBytes)
    {
        try
        {
            int[] test = Enumerable.Range(1, maxSizeOfStackFrameInBytes / OsPageSizeInBytes).Select(k => k * OsPageSizeInBytes).ToArray();
            return test;
        }
        catch (Exception)
        {
            return [];
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        const int Pass = 100;

        const int megabyte = 1024 * 1024;
        int eq = 256;
        for(int m = megabyte; m < 32 * megabyte; m += megabyte, eq += 256) // from 1MB to 32MB
        {
            int test = TestStack(m).Length;
            if (test != eq) return eq;
        }

        return Pass;
    }
}
