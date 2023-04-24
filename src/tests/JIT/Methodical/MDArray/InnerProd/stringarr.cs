// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public class Stringmm
{
    public static int size;
    public static Random rand;
    public const int DefaultSeed = 20010415;

    public static void InitMatrix2D(out String[,] m, out String[][] refm)
    {
        int i, j, temp;
        i = 0;

        m = new String[size, size];
        refm = new String[size][];

        for (int k = 0; k < refm.Length; k++)
            refm[k] = new String[size];

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = rand.Next();
                temp = temp - (temp / 120) * 120 - 60;
                m[i, j] = Convert.ToString(temp);
                refm[i][j] = Convert.ToString(temp);
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct2D(out String res, ref String[,] a, ref String[,] b, int row, int col)
    {
        int i;
        res = "";

        int temp1, temp2, temp3;
        temp3 = 0;

        i = 0;
        while (i < size)
        {
            temp1 = Convert.ToInt32(a[row, i]);
            temp2 = Convert.ToInt32(b[i, col]);
            temp3 = temp3 + temp1 * temp2;
            res = Convert.ToString(temp3);
            i++;
        }
    }

    public static void InnerProduct2DRef(out String res, ref String[][] a, ref String[][] b, int row, int col)
    {
        int i;
        res = "";

        int temp1, temp2, temp3;
        temp3 = 0;

        i = 0;
        while (i < size)
        {
            temp1 = Convert.ToInt32(a[row][i]);
            temp2 = Convert.ToInt32(b[i][col]);
            temp3 = temp3 + temp1 * temp2;
            res = Convert.ToString(temp3);
            i++;
        }
    }

    public static void Init3DMatrix(String[,,] m, String[][] refm)
    {
        int i, j, temp;
        i = 0;

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = rand.Next();
                temp = temp - (temp / 120) * 120 - 60;
                m[i, 0, j] = Convert.ToString(temp);
                refm[i][j] = Convert.ToString(temp);
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct3D(out String res, String[,,] a, String[,,] b, int row, int col)
    {
        int i;
        res = "";

        int temp1, temp2, temp3;
        temp3 = 0;

        i = 0;
        while (i < size)
        {
            temp1 = Convert.ToInt32(a[row, 0, i]);
            temp2 = Convert.ToInt32(b[i, 0, col]);
            temp3 = temp3 + temp1 * temp2;
            res = Convert.ToString(temp3);
            i++;
        }
    }

    public static void InnerProduct3DRef(out String res, String[][] a, String[][] b, int row, int col)
    {
        int i;
        res = "";

        int temp1, temp2, temp3;
        temp3 = 0;

        i = 0;
        while (i < size)
        {
            temp1 = Convert.ToInt32(a[row][i]);
            temp2 = Convert.ToInt32(b[i][col]);
            temp3 = temp3 + temp1 * temp2;
            res = Convert.ToString(temp3);
            i++;
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = false;

        int seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
        {
            string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
            string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
            _ => DefaultSeed
        };

        rand = new Random(seed);
        size = rand.Next(2, 10);

        Console.WriteLine();
        Console.WriteLine("2D Array");
        Console.WriteLine("Random seed: {0}; set environment variable CORECLR_SEED to this value to reproduce", seed);
        Console.WriteLine("Testing inner product of {0} by {0} matrices", size);
        Console.WriteLine("Matrix element stores string data converted from random integer");
        Console.WriteLine("array set/get, ref/out param are used");

        String[,] ima2d = new String[size, size];
        String[,] imb2d = new String[size, size];
        String[,] imr2d = new String[size, size];

        String[][] refa2d = new String[size][];
        String[][] refb2d = new String[size][];
        String[][] refr2d = new String[size][];
        for (int k = 0; k < refr2d.Length; k++)
            refr2d[k] = new String[size];

        InitMatrix2D(out ima2d, out refa2d);
        InitMatrix2D(out imb2d, out refb2d);

        int m = 0;
        int n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct2D(out imr2d[m, n], ref ima2d, ref imb2d, m, n);
                InnerProduct2DRef(out refr2d[m][n], ref refa2d, ref refb2d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr2d[i, j] != refr2d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr2d[i,j] {2}!=refr2d[i][j] {3}", i, j, imr2d[i, j], refr2d[i][j]);
                    pass = false;
                }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Testing inner product of one slice of two 3D matrices");
        Console.WriteLine("Matrix element stores string data converted from random integer");

        String[,,] ima3d = new String[size, 2, size];
        String[,,] imb3d = new String[size, 3, size];
        String[,,] imr3d = new String[size, size, size];

        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                imr3d[i, j, 0] = "";


        String[][] refa3d = new String[size][];
        String[][] refb3d = new String[size][];
        String[][] refr3d = new String[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new String[size];
        for (int k = 0; k < refb3d.Length; k++)
            refb3d[k] = new String[size];
        for (int k = 0; k < refr3d.Length; k++)
            refr3d[k] = new String[size];

        Init3DMatrix(ima3d, refa3d);
        Init3DMatrix(imb3d, refb3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct3D(out imr3d[m, n, 0], ima3d, imb3d, m, n);
                InnerProduct3DRef(out refr3d[m][n], refa3d, refb3d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr3d[i, j, 0] != refr3d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr3d[i,j,0] {2}!=refr3d[i][j] {3}", i, j, imr3d[i, j, 0], refr3d[i][j]);
                    pass = false;
                }
        }

        Console.WriteLine();

        if (pass)
        {
            Console.WriteLine("PASSED");
            return 100;
        }
        else
        {
            Console.WriteLine("FAILED");
            return 1;
        }
    }
}
