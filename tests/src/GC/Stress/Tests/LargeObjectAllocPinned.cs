// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Regression Test for Bug# 145842(Possible GC hole with byrefs into the large heap)

using System;
internal class LargePinned
{
    [System.Security.SecuritySafeCritical]
    unsafe public static int Main(String[] args)
    {
        for (int i = 0; i < 25; i++)
        {
            byte[] x = new byte[130000];
            fixed (byte* z = x)
            {
                for (int j = 0; j < 100; j++)
                {
                    byte[] y = new byte[120000];
                }
                *z = 23;
            }

            TestLibrary.Logging.WriteLine("End of Loop: {0} \n", i);
        }
        TestLibrary.Logging.WriteLine("Test Passed\n");
        return 100;
    }
}
