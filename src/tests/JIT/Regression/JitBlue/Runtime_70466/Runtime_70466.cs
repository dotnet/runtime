// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_70466
{
    [Fact]
    public static int TestEntryPoint()
    {
        Problem(1, 0);
        return 100;
    }

    // This method is carefully crafted such that we end up with an unused ARR_ADDR node by rationalization
    // time, of which the child operand will need to be explicitly marked "unused value" for LIR purposes.
    //
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Problem(byte b, int idx)
    {
        var a = new byte[] { b };
        var a1 = new byte[] { b, b };
        var a2 = new byte[] { b, b, b };

        if (idx == 0)
        {
            if (a[idx] == 1)
            {
                Use(1);
            }
            JitUse(a1[idx] + a2[idx]);
        }
    }

    internal static void Use<T>(T arg) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void JitUse<T>(T arg) { }
}
