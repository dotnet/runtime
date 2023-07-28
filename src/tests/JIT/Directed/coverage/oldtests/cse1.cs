// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//testing common sub-expression elimination

using System;
using Xunit;
public class CL
{
    public int item;
};

public class CSE1
{
    private static int s_sa;
    private static int s_sb;
    private static int[] s_arr1d = { 10, 20, 30, 40, 50 };

    private static int DoIt(ref int pa)
    {
        int result = 0;
        if (s_sa + s_sb == 0)
            result++;
        pa = 1;
        if (s_sa + s_sb == 1)
            result++;
        pa = 2;
        if (s_sa + s_sb == 2)
            result++;
        pa = 3;
        if (s_sa + s_sb == 3)
            result++;
        return result;
    }

    private static int DoAdd(ref int pa)
    {
        int result = 0;
        if (s_sa + s_sb == 0)
            result += s_arr1d[s_sa + s_sb];
        pa = 1;
        if (s_sa + s_sb == 1)
            result += s_arr1d[s_sa + s_sb];
        pa = 2;
        if (s_sa + s_sb == 2)
            result += s_arr1d[s_sa + s_sb];
        pa = 3;
        if (s_sa + s_sb == 3)
            result += s_arr1d[s_sa + s_sb];
        result += s_arr1d[s_sa + s_sb + 1];
        return result;
    }

    private static int DoSub(ref int pa)
    {
        int result = 0;
        if (s_sa - s_sb == 3)
            result += s_arr1d[s_sa - s_sb - 3];
        pa = 1;
        if (s_sa - s_sb == 1)
            result += s_arr1d[s_sa - s_sb];
        pa = 2;
        if (s_sa - s_sb == 2)
            result += s_arr1d[s_sa - s_sb];
        pa = 3;
        if (s_sa - s_sb == 3)
            result += s_arr1d[s_sa - s_sb];
        result += s_arr1d[s_sa - s_sb + 1];
        return result;
    }

    private static int DoMul(ref int pa)
    {
        int result = 0;
        if (s_sa * s_sb == 3)
            result += s_arr1d[s_sa * s_sb * result];
        pa = 1;
        if (s_sa * s_sb == 1)
            result += s_arr1d[s_sa * s_sb];
        pa = 2;
        if (s_sa * s_sb == 2)
            result += s_arr1d[s_sa * s_sb];
        pa = 3;
        if (s_sa * s_sb == 3)
            result += s_arr1d[s_sa * s_sb];
        result += s_arr1d[s_sa * s_sb + 1];
        return result;
    }

    private static int DoDiv(ref int pa)
    {
        int result = 0;
        if (s_sa / s_sb == 3)
            result += s_arr1d[s_sa / s_sb];
        pa = 1;
        if (s_sa / s_sb == 1)
            result += s_arr1d[s_sa / s_sb];
        pa = 2;
        if (s_sa / s_sb == 2)
            result += s_arr1d[s_sa / s_sb];
        pa = 3;
        if (s_sa / s_sb == 3)
            result += s_arr1d[s_sa / s_sb - 3];
        result += s_arr1d[s_sa / s_sb + 1];
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int result;
        s_sa = 0;
        s_sb = 0;

        result = DoIt(ref s_sa);
        if ((result != 4) || (s_sa != 3))
        {
            Console.WriteLine("testcase 0 FAILED, result is {0}, sa is {1}", result, s_sa);
            return 1;
        }

        s_sa = 0;
        result = DoAdd(ref s_sa);
        if ((result != 150) || (s_sa != 3))
        {
            Console.WriteLine("testcase 1 FAILED, result is {0}, sa is {1}", result, s_sa);
            return 1;
        }

        result = DoSub(ref s_sa);
        if ((result != 150) || (s_sa != 3))
        {
            Console.WriteLine("testcase 2 FAILED, result is {0}, sa is {1}", result, s_sa);
            return 1;
        }

        s_sb = 1;
        result = DoMul(ref s_sa);
        if ((result != 150) || (s_sa != 3))
        {
            Console.WriteLine("testcase 3 FAILED, result is {0}, sa is {1}", result, s_sa);
            return 1;
        }

        result = DoDiv(ref s_sa);
        if ((result != 150) || (s_sa != 3))
        {
            Console.WriteLine("testcase 4 FAILED, result is {0}, sa is {1}", result, s_sa);
            return 1;
        }

        CL CL1 = new CL();
        CL1.item = 10;
        if (CL1.item * 2 < 30)
        {
            CL1.item = CL1.item * 2;
        }
        else
        {
            CL1.item = 5 * (CL1.item * 2);
        }

        if (CL1.item * 2 != 40)
        {
            Console.WriteLine("testcase 5 FAILED, CL1.item is {0}", CL1.item);
            return 1;
        }

        Console.WriteLine("PASSED");
        return 100;
    }
}
