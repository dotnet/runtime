// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Simple arithmatic manipulation of one 2D array elements

using System;

public class double1
{
    public static Random rand;
    public const int DefaultSeed = 20010415;
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
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a[i, j] += a[0, j] + a[1, j];
                a[i, j] *= a[i, j] + a[2, j];
                a[i, j] -= a[i, j] * a[3, j];
                a[i, j] /= a[i, j] + a[4, j];
                for (int k = 5; k < size; k++)
                    a[i, j] += a[k, j];
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
                m[i, size, j] = temp - 60;
                refm[i][j] = temp - 60;
                j++;
            }
            i++;
        }
    }

    public static void Process3DArray(double[,,] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a[i, size, j] += a[0, size, j] + a[1, size, j];
                a[i, size, j] *= a[i, size, j] + a[2, size, j];
                a[i, size, j] -= a[i, size, j] * a[3, size, j];
                a[i, size, j] /= a[i, size, j] + a[4, size, j];
                for (int k = 5; k < size; k++)
                    a[i, size, j] += a[k, size, j];
            }
    }

    public static void ProcessJagged3DArray(double[][] a)
    {
        for (int i = 10; i < size + 10; i++)
            for (int j = 0; j < size; j++)
            {
                int b = i - 4;
                a[i - 10][j] += a[0][j] + a[1][j];
                a[i - 10][j] *= a[b - 6][j] + a[2][j];
                a[b - 6][j] -= a[i - 10][j] * a[3][j];
                a[i - 10][j] /= a[b - 6][j] + a[4][j];
                for (int k = 5; k < size; k++)
                    a[i - 10][j] += a[k][j];
            }
    }

    public static int Main()
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
        Console.WriteLine("Matrix is member of a Jagged array, element stores random double");
        Console.WriteLine("array set/get, ref/out param are used");

        double[][,] ima2d = new double[3][,];
        ima2d[2] = new double[size, size];

        double[][] refa2d = new double[size][];

        Init2DMatrix(out ima2d[2], out refa2d);

        int m = 0;
        int n;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process2DArray(ref ima2d[2]);
                ProcessJagged2DArray(ref refa2d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima2d[2][i, j] != refa2d[i][j])
                    if (!Double.IsNaN(ima2d[2][i, j]) || !Double.IsNaN(refa2d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima2d[2][i,j] {2}!=refa2d[i][j] {3}", i, j, ima2d[2][i, j], refa2d[i][j]);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima2d[2][Int32.MinValue, 0] = 5;
                pass = false;
            }
            catch (IndexOutOfRangeException)
            { }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Element manipulation of {0} by {1} by {2} matrices with different arithmatic operations", size, size + 1, size + 2);
        Console.WriteLine("Matrix is member of a Jagged array, element stores random double");

        double[][,,] ima3d = new double[3][,,];
        ima3d[2] = new double[size, size + 1, size + 2];
        double[][] refa3d = new double[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new double[size];

        Init3DMatrix(ima3d[2], refa3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process3DArray(ima3d[2]);
                ProcessJagged3DArray(refa3d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima3d[2][i, size, j] != refa3d[i][j])
                    if (!Double.IsNaN(ima3d[2][i, size, j]) || !Double.IsNaN(refa3d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima3d[2][i,{3},j] {4}!=refa3d[i][j] {5}", i, j, size, ima3d[2][i, size, j], refa3d[i][j]);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima3d[2][size, -1, size] = 5;
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
