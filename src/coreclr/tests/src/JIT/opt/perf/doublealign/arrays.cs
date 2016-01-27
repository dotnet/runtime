// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

public class CMyException : System.Exception
{
}

public class CTest
{
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static void UseByte(byte x)
    {
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void CheckDoubleAlignment(double* p)
    {
        if (((int)p % sizeof(double)) != 0)
            throw new CMyException();
    }

    private static void DoGC()
    {
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);
    }


    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    private static unsafe void TestArrays1(int n, double d)
    {
        int i, j;

        double[][] a = new double[n][];
        byte[][] b = new byte[n][];


        for (i = 0; i < n; i++)
        {
            a[i] = new double[i + 1];
            fixed (double* p = &a[i][0])
            {
                CheckDoubleAlignment(p);
            }
            b[i] = new byte[i + 1];
        }

        for (i = 0; i < n; i++)
        {
            for (j = 1; j <= i; j++)
            {
                a[i][j] += a[i][j - 1];
                b[i][j] = 48;
            }
        }

        for (i = 0; i < n; i++)
        {
            UseByte(b[i][i]);
            UseByte((byte)a[i][i]);
        }
    }

    public static int Main()
    {
        try
        {
            TestArrays1(100, 2.0);
        }
        catch (CMyException)
        {
            Console.WriteLine("FAILED");
            return 101;
        }
        Console.WriteLine("PASSED");
        return 100;
    }
}
