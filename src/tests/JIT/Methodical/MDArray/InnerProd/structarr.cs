// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace Test_structarr_InnerProd
{
public struct ArrayStruct
{
    public int[,] a2d;
    public int[,,] a3d;

    public ArrayStruct(int size)
    {
        a2d = new int[size, size];
        a3d = new int[size + 2, size + 5, size + 1];
    }
}


public class intmm
{
    public static int size;
    public static Random rand;
    public const int DefaultSeed = 20010415;
    public static ArrayStruct ima;
    public static ArrayStruct imb;
    public static ArrayStruct imr;

    public static void Init2DMatrix(out ArrayStruct m, out int[][] refm)
    {
        int i, j, temp;
        i = 0;

        m = new ArrayStruct(size);
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

    public static void InnerProduct2D(out int res, ref ArrayStruct a2d, ref ArrayStruct b, int row, int col)
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

    public static void Init3DMatrix(ref ArrayStruct m, int[][] refm)
    {
        int i, j, temp;
        i = 0;

        while (i < size)
        {
            j = 0;
            while (j < size)
            {
                temp = rand.Next();
                m.a3d[i, size - 2, j] = temp - (temp / 120) * 120 - 60;
                refm[i][j] = temp - (temp / 120) * 120 - 60;
                j++;
            }
            i++;
        }
    }

    public static void InnerProduct3D(out int res, ref ArrayStruct a3d, ref ArrayStruct b, int row, int col)
    {
        int i;
        res = 0;
        i = 0;
        while (i < size)
        {
            res = res + a3d.a3d[row, size - 2, i] * b.a3d[i, size - 2, col];
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
        Console.WriteLine("the matrices are members of Struct");
        Console.WriteLine("Matrix element stores random integer");
        Console.WriteLine("array set/get, ref/out param are used");

        ima = new ArrayStruct(size);
        imb = new ArrayStruct(size);
        imr = new ArrayStruct(size);

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
        Console.WriteLine("Testing inner product of one slice of two 3D matrices, size is {0}", size);
        Console.WriteLine("the matrices are members of Struct, matrix element stores random integer");

        ima = new ArrayStruct(size);
        imb = new ArrayStruct(size);
        imr = new ArrayStruct(size);
        for (int i = 0; i < size; i++)
            for (int j = 0; j < size; j++)
                imr.a3d[i, j, size - 2] = 1;

        int[][] refa3d = new int[size][];
        int[][] refb3d = new int[size][];
        int[][] refr3d = new int[size][];
        for (int k = 0; k < refa3d.Length; k++)
            refa3d[k] = new int[size];
        for (int k = 0; k < refb3d.Length; k++)
            refb3d[k] = new int[size];
        for (int k = 0; k < refr3d.Length; k++)
            refr3d[k] = new int[size];

        Init3DMatrix(ref ima, refa3d);
        Init3DMatrix(ref imb, refb3d);

        m = 0;
        n = 0;

        while (m < size)
        {
            n = 0;
            while (n < size)
            {
                InnerProduct3D(out imr.a3d[m, n, size - 2], ref ima, ref imb, m, n);
                InnerProduct3DRef(out refr3d[m][n], refa3d, refb3d, m, n);
                n++;
            }
            m++;
        }

        for (int i = 0; i < size; i++)
        {
            pass = true;
            for (int j = 0; j < size; j++)
                if (imr.a3d[i, j, size - 2] != refr3d[i][j])
                {
                    Console.WriteLine("i={0}, j={1}, imr.a3d[i,j,size-2] {2}!=refr3d[i][j] {3}", i, j, imr.a3d[i, j, size - 2], refr3d[i][j]);
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
