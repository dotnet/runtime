// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

internal static class Module
{
    private static int Main()
    {
        int Var1, Temp;
        try
        {
            for (Temp = int.MaxValue - 3; Temp <= int.MaxValue - 1; Temp++)
                Var1 = (int)(2 + Temp);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Expected Overflow Error: " + ex.ToString());
            return 100;
        }
        return -1;
    }
}
