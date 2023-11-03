// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Runtime_86538
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 99;
        int finallyResult = -1;
        try
        {
            try
            {
                throw new Exception();
            }
            finally
            {
                finallyResult = result;
            }
        }
        catch when (result++ == 99)
        {
        }

        if (finallyResult != 100)
        {
            Console.WriteLine("FAIL: finallyResult == {0}", finallyResult);
        }

        return finallyResult;
    }
}
