// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

public class Program
{
    private static int Main(string[] args)
    {
        try
        {
            short i = 1;
            object o = i;
            int? k = (int?)o;
        }
        catch (InvalidCastException)
        {
            Console.WriteLine("Test SUCCESS");
            return 100;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            Console.WriteLine("Test FAILED");
            return -10;
        }

        Console.WriteLine("Test FAILED");
        return -11;
    }
}