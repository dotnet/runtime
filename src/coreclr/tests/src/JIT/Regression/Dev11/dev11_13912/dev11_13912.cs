// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

public class P
{
    public static int Main()
    {
        // This bug is caused by a broken flowgraph due to a return from
        // a try inside a catch block

        TestCatchReturn();

        // Successfully jitted a return from a try inside a catch block
        return 100;
    }

    public static void TestCatchReturn()
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
