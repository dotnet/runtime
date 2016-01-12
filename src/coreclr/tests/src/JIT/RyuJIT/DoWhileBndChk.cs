// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// Ensure bounds checks aren't elided for this do-while loop.
//
// Reference: TF Bug 150041

using System;
using System.Runtime.ExceptionServices;

public class Program
{
    [HandleProcessCorruptedStateExceptions]
    public static int Main()
    {
        int ret = 99;

        try
        {
            int[] a = new int[0];
            int i = 0x1FFFFFFF;
            do
            {
                a[i] = 0;
                ++i;
            }
            while (i < a.Length);
        }
        catch (Exception e)
        {
            if (e is IndexOutOfRangeException)
            {
                ret = 100;
            }
        }

        return ret;
    }
}

