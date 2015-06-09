// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public class AA
{
    static sbyte su;

    static void Method1(ref int a, ref Array[,] b, ref double c, ref object d)
    {
        try
        {
            sbyte[,] aa = new sbyte[10, 10];
            aa[0, a] += su;
            aa[a, a] += su;
        }
        catch (IndexOutOfRangeException)
        {
            b[a, a] = null;
        }
    }

    static int Main()
    {
        Main1();
        return 100;
    }

    static void Main1()
    {
        int L1 = 0;
        Array[,] L2 = null;
        double L3 = 0.0;
        object L4 = null;
        Method1(ref L1, ref L2, ref L3, ref L4);
    }
}
