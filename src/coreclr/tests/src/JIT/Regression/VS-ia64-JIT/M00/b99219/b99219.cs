// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public class a
{
    static int temp = 0;

    public static void MiddleMethod()
    {
        temp += 1;
        try
        {
            temp *= 2;
            throw new System.ArgumentException();
        }
        finally
        {
            temp += 5;
            Console.WriteLine("In Finally");
        }
        temp *= 3;
        Console.WriteLine("Done...");
    }

    public static int Main(string[] args)
    {
        Console.WriteLine("Starting....");

        try
        {
            MiddleMethod();
        }
        catch
        {
            if (temp == 7)
                Console.WriteLine("PASS");
            return 100;
        }
        Console.WriteLine("Failed - temp = " + temp);
        return 0;
    }
}