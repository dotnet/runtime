// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression Test for Bug# 145842(Possible GC hole with byrefs into the large heap)

using System;
using Xunit;
public class LargePinned
{
    [System.Security.SecuritySafeCritical]
    [Fact]
    unsafe public static void TestEntryPoint()
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
    }
}
