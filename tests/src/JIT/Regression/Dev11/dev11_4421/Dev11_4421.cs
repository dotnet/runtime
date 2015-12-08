// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
