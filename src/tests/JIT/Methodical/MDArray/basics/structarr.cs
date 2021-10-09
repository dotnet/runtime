// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Simple arithmatic manipulation of one 2D array elements

using System;

public struct ArrayStruct
{
    public double[,] a2d;
    public double[,,] a3d;

    public ArrayStruct(int size)
    {
        a2d = new double[size, size];
        a3d = new double[size, size, size];
    }
}
public class struct1
{
    public static Random rand;
    public const int DefaultSeed = 20010415;
    public static int size;
    public static ArrayStruct ima;

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

    public static void Process2DArray(ref double[,] a2d)
    {
        for (int i = 10; i < size + 10; i++)
            for (int j = 0; j < size; j++)
            {
                a2d[i - 10, j] += a2d[0, j] + a2d[1, j];
                a2d[i - 10, j] += 10;
                a2d[i - 10, j] *= a2d[i - 10, j] + a2d[2, j];
                a2d[i - 10, j] -= a2d[i - 10, j] * a2d[3, j];
                if ((a2d[i - 10, j] + a2d[4, j]) != 0)
                    a2d[i - 10, j] /= a2d[i - 10, j] + a2d[4, j];
                else
                    a2d[i - 10, j] += a2d[i - 10, j] * a2d[4, j];
                for (int k = 5; k < size; k++)
                    a2d[i - 10, j] += a2d[k, j];
            }
    }

    public static void ProcessJagged2DArray(ref double[][] a2d)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a2d[i][j] += a2d[0][j] + a2d[1][j];
                a2d[i][j] += 10;
                a2d[i][j] *= a2d[i][j] + a2d[2][j];
                a2d[i][j] -= a2d[i][j] * a2d[3][j];
                if ((a2d[i][j] + a2d[4][j]) != 0)
                    a2d[i][j] /= a2d[i][j] + a2d[4][j];
                else
                    a2d[i][j] += a2d[i][j] * a2d[4][j];
                for (int k = 5; k < size; k++)
                    a2d[i][j] += a2d[k][j];
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

    public static void Process3DArray(double[,,] a3d)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a3d[i, 0, j] += a3d[0, 0, j] + a3d[1, 0, j];
                a3d[i, 0, j] *= a3d[i, 0, j] + a3d[2, 0, j];
                a3d[i, 0, j] -= a3d[i, 0, j] * a3d[3, 0, j];
                if ((a3d[i, 0, j] + a3d[4, 0, j]) != 0)
                    a3d[i, 0, j] /= a3d[i, 0, j] + a3d[4, 0, j];
                else
                    a3d[i, 0, j] += a3d[i, 0, j] * a3d[4, 0, j];
                for (int k = 5; k < size; k++)
                    a3d[i, 0, j] += a3d[k, 0, j];
            }
    }

    public static void ProcessJagged3DArray(double[][] a3d)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                a3d[i][j] += a3d[0][j] + a3d[1][j];
                a3d[i][j] *= a3d[i][j] + a3d[2][j];
                a3d[i][j] -= a3d[i][j] * a3d[3][j];
                if ((a3d[i][j] + a3d[4][j]) != 0)
                    a3d[i][j] /= a3d[i][j] + a3d[4][j];
                else
                    a3d[i][j] += a3d[i][j] * a3d[4][j];
                for (int k = 5; k < size; k++)
                    a3d[i][j] += a3d[k][j];
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
        Console.WriteLine("Matrix is member of struct, element stores random double");
        Console.WriteLine("array set/get, ref/out param are used");

        ima = new ArrayStruct(size);
        double[][] refa2d = new double[size][];

        Init2DMatrix(out ima.a2d, out refa2d);

        int m = 0;
        int n;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process2DArray(ref ima.a2d);
                ProcessJagged2DArray(ref refa2d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima.a2d[i, j] != refa2d[i][j])
                    if (!Double.IsNaN(ima.a2d[i, j]) || !Double.IsNaN(refa2d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima.a2d[i,j] {2}!=refa2d[i][j] {3}", i, j, ima.a2d[i, j], refa2d[i][j]);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima.a2d[size, size] = 5;
                pass = false;
            }
            catch (IndexOutOfRangeException)
            { }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Element manipulation of 3D matrice with different arithmatic operations, size is {0}", size);
        Console.WriteLine("Matrix is member of struct, element stores random double");

        ima = new ArrayStruct(size);
        double[][] refa3d = new double[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new double[size];

        Init3DMatrix(ima.a3d, refa3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                Process3DArray(ima.a3d);
                ProcessJagged3DArray(refa3d);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (ima.a3d[i, 0, j] != refa3d[i][j])
                    if (!Double.IsNaN(ima.a3d[i, 0, j]) || !Double.IsNaN(refa3d[i][j]))
                    {
                        Console.WriteLine("i={0}, j={1}, ima.a3d[i,0,j] {2}!=refa3d[i][j] {3}", i, j, ima.a3d[i, 0, j], refa3d[i][j]);
                        pass = false;
                    }
        }

        if (pass)
        {
            try
            {
                ima.a3d[size, size, size] = 5;
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
