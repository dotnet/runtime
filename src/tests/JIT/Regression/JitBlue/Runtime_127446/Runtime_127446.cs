// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_127446;

using System;
using Xunit;

public class Runtime_127446
{
    // Regression test: the JIT's if-conversion phase used to assert when the
    // "Then" block was the start of an EH region (and therefore marked
    // BBF_DONT_REMOVE). Compilation should succeed without hitting the assert.
    [Fact]
    public static void TestEntryPoint()
    {
        try
        {
            M0();
        }
        catch (NullReferenceException)
        {
        }
        catch (DivideByZeroException)
        {
        }
    }

    private static void M0()
    {
        int var1 = default(int);
        bool[,] var4 = default(bool[,]);
        if (var4[0, 0])
        {
            try
            {
                var1 = 0;
            }
            catch (System.Exception)
            {
                try
                {
                    var4[0, 0] = var4[0, 0];
                }
                catch (System.Exception)
                {
                }
            }
        }

        var1 = (0 / var1);
    }
}
