// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Inner Product of two 2D array

using System;

public class ArrayClass
{
    public int[,] a2d;
    public int[,,] a3d;

    public ArrayClass(int size)
    {
        a2d = new int[size, size];
        a3d = new int[size, size, size];
    }
}


public class intmm
{
    public static int size;
    public static Random rand;
    public const int DefaultSeed = 20010415;
    public static ArrayClass ima;
    public static ArrayClass imb;
    public static ArrayClass imr;

    public static void Init2DMatrix(out ArrayClass m, out int[][] refm)
    {
        int i, j, temp;
        i = 0;

        //m = new int[size, size];
        m = new ArrayClass(size);
        refm = new int[size][];

        for (int k = 0; k < refm.Length; k++)
            refm[k] = new int[size];

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = rand.Next();
                m.a2d[i, j] = temp - (temp / 120) * 120 - 60;
                refm[i][j] = temp - (temp / 120) * 120 - 60;
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct2D(out int res, ref ArrayClass a2d, ref ArrayClass b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a2d.a2d[row, i] * b.a2d[i, col];
            i++;
        }
    }

    public static void InnerProduct2DRef(out int res, ref int[][] a2d, ref int[][] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a2d[row][i] * b[i][col];
            i++;
        }
    }

    public static void Init3DMatrix(ArrayClass m, int[][] refm)
    {
        int i, j, temp;
        i = 0;

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = rand.Next();
                m.a3d[i, j, 0] = temp - (temp / 120) * 120 - 60;
                refm[i][j] = temp - (temp / 120) * 120 - 60;
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct3D(out int res, ArrayClass a3d, ArrayClass b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a3d.a3d[row, i, 0] * b.a3d[i, col, 0];
            i++;
        }
    }

    public static void InnerProduct3DRef(out int res, int[][] a3d, int[][] b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a3d[row][i] * b[i][col];
            i++;
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
        size = rand.Next(2, 10);

        Console.WriteLine();
        Console.WriteLine("2D Array");
        Console.WriteLine("Random seed: {0}; set environment variable CORECLR_SEED to this value to reproduce", seed);
        Console.WriteLine("Testing inner product of {0} by {0} matrices", size);
        Console.WriteLine("the matrices are members of class");
        Console.WriteLine("Matrix element stores random integer");
        Console.WriteLine("array set/get, ref/out param are used");

        ima = new ArrayClass(size);
        imb = new ArrayClass(size);
        imr = new ArrayClass(size);

        int[][] refa2d = new int[size][];
        int[][] refb2d = new int[size][];
        int[][] refr2d = new int[size][];
        for (int k = 0; k < refr2d.Length; k++)
            refr2d[k] = new int[size];

        Init2DMatrix(out ima, out refa2d);
        Init2DMatrix(out imb, out refb2d);

        int m = 0;
        int n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct2D(out imr.a2d[m, n], ref ima, ref imb, m, n);
                InnerProduct2DRef(out refr2d[m][n], ref refa2d, ref refb2d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr.a2d[i, j] != refr2d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr.a2d[i,j] {2}!=refr2d[i][j] {3}", i, j, imr.a2d[i, j], refr2d[i][j]);
                    pass = false;
                }
        }

        Console.WriteLine();
        Console.WriteLine("3D Array");
        Console.WriteLine("Testing inner product of one slice of two {0} by {0} by {0} matrices", size);
        Console.WriteLine("the matrices are members of class and matrix element stores random integer");

        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                imr.a3d[i, j, 0] = 1;

        int[][] refa3d = new int[size][];
        int[][] refb3d = new int[size][];
        int[][] refr3d = new int[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new int[size];
        for (int k = 0; k < refb3d.Length; k++)
            refb3d[k] = new int[size];
        for (int k = 0; k < refr3d.Length; k++)
            refr3d[k] = new int[size];

        Init3DMatrix(ima, refa3d);
        Init3DMatrix(imb, refb3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct3D(out imr.a3d[m, n, 0], ima, imb, m, n);
                InnerProduct3DRef(out refr3d[m][n], refa3d, refb3d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr.a3d[i, j, 0] != refr3d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr.a3d[i,j,0] {2}!=refr3d[i][j] {3}", i, j, imr.a3d[i, j, 0], refr3d[i][j]);
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
