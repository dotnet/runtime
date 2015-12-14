// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
public class test
{
    public static int Main(String[] args)
    {
        bool flag = false;
        for (int i = 1; i <= -1; i++)
        {
            flag = true;
        }

        if (flag)
        {
            Console.WriteLine("FAIL"); return 101;
        }
        else
        {
            Console.WriteLine("PASS"); return 100;
        }
    }
}
