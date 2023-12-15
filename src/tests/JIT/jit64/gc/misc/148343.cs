// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using Xunit;

public class T
{
    public static bool test(ref Object[] arr, ref Object o, int index)
    {
        GC.Collect();
        if (arr[index] == null)
        {
            Console.WriteLine("null");
            return false;
        }
        if (arr[index] != o)
        {
            return false;
        }

        for (int i = 0; i < arr.Length; i++)
        {
            Console.WriteLine(arr[i]);
        }
        return (true);
    }
    static Object[] o = new Object[5];

    [Fact]
    public static int TestEntryPoint()
    {
        o[1] = "1";
        o[2] = "2";
        o[3] = "3";
        if (test(ref o, ref o[2], 2)) return 100;
        //error
        return 1;
    }
}
