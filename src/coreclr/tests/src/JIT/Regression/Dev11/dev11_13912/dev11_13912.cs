// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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