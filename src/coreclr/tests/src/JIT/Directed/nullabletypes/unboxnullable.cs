// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
