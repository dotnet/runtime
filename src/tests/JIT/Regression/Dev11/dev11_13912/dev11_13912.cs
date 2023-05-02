// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class P
{
    [Fact]
    public static int TestEntryPoint()
    {
        // This bug is caused by a broken flowgraph due to a return from
        // a try inside a catch block

        TestCatchReturn();

        // Successfully jitted a return from a try inside a catch block
        return 100;
    }

    internal static void TestCatchReturn()
    {
        try
        {
        }
        catch (Exception)
        {
            try
            {
                try
                {
                    return;
                }
                catch
                {
                    return;
                }
                finally
                {
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
