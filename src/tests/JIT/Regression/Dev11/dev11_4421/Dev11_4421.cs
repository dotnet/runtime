// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public static class Module
{
    [Fact]
    public static int TestEntryPoint()
    {
        int Var1, Temp;
        try
        {
            checked
            {
                for (Temp = int.MaxValue - 3; Temp <= int.MaxValue - 1; Temp++)
                    Var1 = (int)(2 + Temp);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Expected Overflow Error: " + ex.ToString());
            return 100;
        }
        return -1;
    }
}
