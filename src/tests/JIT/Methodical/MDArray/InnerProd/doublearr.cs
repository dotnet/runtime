// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_doublearr_InnerProd
{
public class doublemm
{
    public static Random rand;
    public const int DefaultSeed = 20010415;
    public static int size;

    public static double GenerateDbl()
    {
        int e;
        int op1 = (int)((float)rand.Next() / Int32.MaxValue * 2);
        if (op1 == 0)
            e = -rand.Next(0, 1000);
        else
            e = +rand.Next(0, 1000);
        return Math.Pow(2, e);
    }

    public static void Init2DMatrix(out double[,] m, out double[][] refm)
    {
        int i, j;
        i = 0;
        double temp;

        m = new double[size, size];
        refm = new double[size][];

        for (int k = 0; k < refm.Length; k++)
            refm[k] = new double[size];

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = GenerateDbl();
                m[i, j] = temp - 60;
                refm[i][j] = temp - 60;
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct2D(out double res, ref double[,] a, ref double[,] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a[row, i] * b[i, col];
            i++;
        }
    }

    public static void InnerProduct2DRef(out double res, ref double[][] a, ref double[][] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a[row][i] * b[i][col];
            i++;
        }
    }

    public static void Init3DMatrix(double[,,] m, double[][] refm)
    {
        int i, j;
        i = 0;
        double temp;

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = GenerateDbl();
                m[i, 0, j] = temp - 60;
                refm[i][j] = temp - 60;
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct3D(out double res, double[,,] a, double[,,] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a[row, 0, i] * b[i, 0, col];
            i++;
        }
    }

    public static void InnerProduct3DRef(out double res, double[][] a, double[][] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a[row][i] * b[i][col];
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
        Console.WriteLine("Matrix element stores random double");
        Console.WriteLine("array set/get, ref/out param are used");

        double[,] ima2d = new double[size, size];
        double[,] imb2d = new double[size, size];
        double[,] imr2d = new double[size, size];

        double[][] refa2d = new double[size][];
        double[][] refb2d = new double[size][];
        double[][] refr2d = new double[size][];
        for (int k = 0; k < refr2d.Length; k++)
            refr2d[k] = new double[size];

        Init2DMatrix(out ima2d, out refa2d);
        Init2DMatrix(out imb2d, out refb2d);

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
        Console.WriteLine("Testing inner product of one slice of two {0} by 2 by {0} matrices", size);
        Console.WriteLine("Matrix element stores random double");

        double[,,] ima3d = new double[size, 2, size];
        double[,,] imb3d = new double[size, 2, size];
        double[,,] imr3d = new double[size, 2, size];

        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                imr3d[i, 0, j] = 1.0;

        double[][] refa3d = new double[size][];
        double[][] refb3d = new double[size][];
        double[][] refr3d = new double[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new double[size];
        for (int k = 0; k < refb3d.Length; k++)
            refb3d[k] = new double[size];
        for (int k = 0; k < refr3d.Length; k++)
            refr3d[k] = new double[size];

        Init3DMatrix(ima3d, refa3d);
        Init3DMatrix(imb3d, refb3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct3D(out imr3d[m, 0, n], ima3d, imb3d, m, n);
                InnerProduct3DRef(out refr3d[m][n], refa3d, refb3d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr3d[i, 0, j] != refr3d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr3d[i,0,j] {2}!=refr3d[i][j] {3}", i, j, imr3d[i, 0, j], refr3d[i][j]);
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
}
