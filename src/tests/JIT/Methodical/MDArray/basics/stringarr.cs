// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Simple arithmatic manipulation of one 2D array elements

using System;

public class string1
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

    public static void Init2DMatrix(out String[,] m, out String[][] refm)
    {
        int i, j;
        i = 0;
        double temp;

        m = new String[size, size];
        refm = new String[size][];

        for (int k = 0; k < refm.Length; k++)
            refm[k] = new String[size];

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = GenerateDbl();
                m[i, j] = Convert.ToString(temp - 60);
                refm[i][j] = Convert.ToString(temp - 60);
                j++;
            }
            i++;
        }
    }

    public static void Process2DArray(ref String[,] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                double temp = Convert.ToDouble(a[i, j]);
                temp += Convert.ToDouble(a[0, j]) + Convert.ToDouble(a[1, j]);
                temp *= Convert.ToDouble(a[i, j]) + Convert.ToDouble(a[2, j]);
                temp -= Convert.ToDouble(a[i, j]) * Convert.ToDouble(a[3, j]);
                temp /= Convert.ToDouble(a[i, j]) + Convert.ToDouble(a[4, j]);
                for (int k = 5; k < size; k++)
                    temp += Convert.ToDouble(a[k, j]);
                a[i, j] = Convert.ToString(temp);
            }
    }

    public static void ProcessJagged2DArray(ref String[][] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                double temp = Convert.ToDouble(a[i][j]);
                temp += Convert.ToDouble(a[0][j]) + Convert.ToDouble(a[1][j]);
                temp *= Convert.ToDouble(a[i][j]) + Convert.ToDouble(a[2][j]);
                temp -= Convert.ToDouble(a[i][j]) * Convert.ToDouble(a[3][j]);
                temp /= Convert.ToDouble(a[i][j]) + Convert.ToDouble(a[4][j]);
                for (int k = 5; k < size; k++)
                    temp += Convert.ToDouble(a[k][j]);
                a[i][j] = Convert.ToString(temp);
            }
    }

    public static void Init3DMatrix(String[,,] m, String[][] refm)
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
                m[i, size, j] = Convert.ToString(temp - 60);
                refm[i][j] = Convert.ToString(temp - 60);
                j++;
            }
            i++;
        }
    }

    public static void Process3DArray(String[,,] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                double temp = Convert.ToDouble(a[i, size, j]);
                temp += Convert.ToDouble(a[0, size, j]) + Convert.ToDouble(a[1, size, j]);
                temp *= Convert.ToDouble(a[i, size, j]) + Convert.ToDouble(a[2, size, j]);
                temp -= Convert.ToDouble(a[i, size, j]) * Convert.ToDouble(a[3, size, j]);
                temp /= Convert.ToDouble(a[i, size, j]) + Convert.ToDouble(a[4, size, j]);
                for (int k = 5; k < size; k++)
                    temp += Convert.ToDouble(a[k, size, j]);
                a[i, size, j] = Convert.ToString(temp);
            }
    }

    public static void ProcessJagged3DArray(String[][] a)
    {
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
            {
                double temp = Convert.ToDouble(a[i][j]);
                temp += Convert.ToDouble(a[0][j]) + Convert.ToDouble(a[1][j]);
                temp *= Convert.ToDouble(a[i][j]) + Convert.ToDouble(a[2][j]);
                temp -= Convert.ToDouble(a[i][j]) * Convert.ToDouble(a[3][j]);
                temp /= Convert.ToDouble(a[i][j]) + Convert.ToDouble(a[4][j]);
                for (int k = 5; k < size; k++)
                    temp += Convert.ToDouble(a[k][j]);
                a[i][j] = Convert.ToString(temp);
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
        size = rand.Next(5, 8);

        Console.WriteLine();
        Console.WriteLine("2D Array");
        Console.WriteLine("Random seed: {0}; set environment variable CORECLR_SEED to this value to reproduce", seed);
        Console.WriteLine("Element manipulation of {0} by {0} matrices with different arithmatic operations", size);
        Console.WriteLine("Matrix element stores string converted from random double");
        Console.WriteLine("array set/get, ref/out param are used");

        String[,] ima2d = new String[size, size];
        String[][] refa2d = new String[size][];

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

        pass = true;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (ima2d[i, j] != refa2d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr[i,j] {2}!=refr[i][j] {3}", i, j, ima2d[i, j], refa2d[i][j]);
                    pass = false;
                }
            }
        }

        if (pass)
        {
            try
            {
                ima2d[0, -1] = "5";
                pass = false;
            }
            catch (IndexOutOfRangeException)
            { }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Element manipulation of {0} by {1} by {0} matrices with different arithmatic operations", size, size + 1);

        String[,,] ima3d = new String[size, size + 1, size];
        String[][] refa3d = new String[size][];

        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new String[size];

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

        pass = true;
        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                if (ima3d[i, size, j] != refa3d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr[i,{4},j] {2}!=refr[i][j] {3}", i, j, ima3d[i, size, j], refa3d[i][j], size);
                    pass = false;
                }
            }
        }

        if (pass)
        {
            try
            {
                ima3d[0, 100, 0] = "";
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
