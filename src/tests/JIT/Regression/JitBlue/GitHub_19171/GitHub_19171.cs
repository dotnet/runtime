// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Threading;
using System.Runtime.CompilerServices;
using Xunit;
public class GitHub_19171
{
    public static long g_static = -1;
    public static int returnVal = 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool checkResult(long result)
    {
        return(result == g_static);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Function(long value)
    {
        g_static = value;

        if (!checkResult(Interlocked.Read(ref g_static)))
        {
            returnVal = -1;
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        Function(7);
        Function(11);
        Function(0x123456789abcdefL);
        return returnVal;
    }
}
