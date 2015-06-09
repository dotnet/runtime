// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;
public struct AA
{
    public static int[,] Static2()
    {
    label1:
        try
        {
        }
        finally
        {
        }
    label2:
        return (new int[1, 1]);
    }

    static int Main()
    {
        try
        {
            Console.WriteLine("Testing AA::Static2");
            AA.Static2();
        }
        catch (Exception x)
        {
            Console.WriteLine("Exception handled: " + x.ToString());
        }
        Console.WriteLine("Passed.");
        return 100;
    }
}