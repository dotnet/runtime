// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Simple arithmatic manipulation of one 2D array elements

using System;
using Xunit;

namespace Test_doublearr_basics
{
public class double1
{
    public static Random rand;
    private const int DefaultSeed = 20010415;
    public static int size;

    public static double GenerateDbl()
    {
        int e;
        int op1 = (int)((float)rand.Next() / Int32.MaxValue * 2);
        if (op1 == 0)
            e = -rand.Next(0, 14);
        else
            e = +rand.Next(0, 14);
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

    public static void Process2DArray(ref double[,] a)
    {
        for (int i = 10; i < size + 10; i++)
            for (int j = 0; j < size; j++)
            {
                a[i - 10, j] += a[0, j] + a[1, j];
                a[i - 10, j] *= a[i - 10, j] + a[2, j];
                a[i - 10, j] -= a[i - 10, j] * a[3, j];
                a[i - 10, j] /= a[i - 10, j] + a[4, j];
                for (int k = 5; k < size; k++)
                    a[i - 10, j] += a[k, j];
            }
    }

    public static void ProcessJagged2DArray(ref double[][] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a[i][j] += a[0][j] + a[1][j];
                a[i][j] *= a[i][j] + a[2][j];
                a[i][j] -= a[i][j] * a[3][j];
                a[i][j] /= a[i][j] + a[4][j];
                for (int k = 5; k < size; k++)
                    a[i][j] += a[k][j];
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
                m[size, i, j] = temp - 60;
                refm[i][j] = temp - 60;
                j++;
            }
            i++;
        }
    }

    public static void Process3DArray(double[,,] a)
    {
        for (int i = 10; i < size + 10; i++)
            for (int j = -4; j < size - 4; j++)
            {
                int b = j + 1;
                a[size, i - 10, j + 4] += a[size, 0, j + 4] + a[size, 1, b + 3];
                a[size, i - 10, j + 4] += 2;
                b = b + 1;
                a[size, i - 10, b + 2] *= a[size, i - 10, b + 2] + a[size, 2, j + 4];
                b = b + 1;
                a[size, i - 10, j + 4] -= a[size, i - 10, j + 4] * a[size, 3, b + 1];
                b = b + 1;
                a[size, i - 10, j + 4] /= a[size, i - 10, j + 4] + a[size, 4, b];
                for (int k = 5; k < size; k++)
                    a[size, i - 10, j + 4] += a[size, k, j + 4];
            }
    }

    public static void ProcessJagged3DArray(double[][] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a[i][j] += a[0][j] + a[1][j];
                a[i][j] += 2;
                a[i][j] *= a[i][j] + a[2][j];
                a[i][j] -= a[i][j] * a[3][j];
                a[i][j] /= a[i][j] + a[4][j];
                for (int k = 5; k < size; k++)
                    a[i][j] += a[k][j];
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

        size = rand.Next(5, 10);

        Console.WriteLine();
        Console.WriteLine("2D Array");
        Console.WriteLine("Random seed: {0}; set environment variable CORECLR_SEED to this value to reproduce", seed);
        Console.WriteLine("Element manipulation of {0} by {0} matrices with different arithmatic operations", size);
        Console.WriteLine("Matrix element stores random double");
        Console.WriteLine("array set/get, ref/out param are used");

        double[,] ima2d = new double[size, size];
        double[][] refa2d = new double[size][];

        Init2DMatrix(out ima2d, out refa2d);

        int m = 0;
        int n;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process2DArray(ref ima2d);
                ProcessJagged2DArray(ref refa2d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima2d[i, j] != refa2d[i][j])
                    if (!Double.IsNaN(ima2d[i, j]) || !Double.IsNaN(refa2d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima2d[i,j] {2}!=refa2d[i][j] {3}", i, j, ima2d[i, j], refa2d[i][j]);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima2d[-1, -1] = 5;
                pass = false;
            }
            catch (IndexOutOfRangeException)
            { }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Element manipulation of {0} by {1} by {2} matrices with different arithmatic operations", size + 1, size + 2, size + 3);
        Console.WriteLine("Matrix element stores random double");

        double[,,] ima3d = new double[size + 1, size + 2, size + 3];
        double[][] refa3d = new double[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new double[size];

        Init3DMatrix(ima3d, refa3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process3DArray(ima3d);
                ProcessJagged3DArray(refa3d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima3d[size, i, j] != refa3d[i][j])
                    if (!Double.IsNaN(ima3d[size, i, j]) || !Double.IsNaN(refa3d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima3d[{4},i,j] {2}!=refa3d[i][j] {3}", i, j, ima3d[size, i, j], refa3d[i][j], size);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima3d[Int32.MaxValue, 0, 0] = 5;
                pass = false;
            }
            catch (IndexOutOfRangeException)
            { }
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
