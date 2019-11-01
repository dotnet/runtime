// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
internal class A
{
    public static int Main()
    {
        int[] arr = new int[10];

        arr[5] = 100;

        short idx = 5;
        byte bdx = 5;
        char cdx = Convert.ToChar(5);
        System.Console.WriteLine(arr[idx] + " " + arr[bdx] + " " + arr[cdx]);
        if (arr[idx] == 100 && arr[bdx] == 100 && arr[cdx] == 100)
        {
            Console.WriteLine("Passed");
            return 100;
        }
        else
        {
            Console.WriteLine("Failed");
            return 101;
        }
    }
}
