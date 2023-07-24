// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_55131
{
    // When merging the assertion props for the finally block, we should consider the assertions
    // out of the BBJ_CALLFINALLY block. Otherwise, we could propagate the wrong assertions inside
    // the finally block.
    //
    // Althogh there can be several ways to reproduce this problem, an easier way is to turn off
    // finally cloning for below example.
    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool False() => false;

    static ushort s_6;
    static uint[] s_15 = new uint[] { 0 };
    static bool s_19 = false;
    [Fact]
    public static int TestEntryPoint()
    {
        bool condition = False();
        int result = 100;
        if (condition)
        {
            result -= 1;
        }

        try
        {
            if (condition)
            {
                result -= 1;
            }
        }
        finally
        {
            if (condition)
            {
                result -= 1;
            }
        }
        return result;
    }
}
