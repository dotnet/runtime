// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Solving AX=B and the inverse of A with Gauss-Jordan algorithm

using System;
using Xunit;

public class jaggedarr
{
    public static double[][,] jaggeda;
    public static double[][,] jaggedb;

    private static double s_tolerance = 0.0000000000001;
    public static bool AreEqual(double left, double right)
    {
        return Math.Abs(left - right) < s_tolerance;
    }

    public static void swap(double a, double b)
    {
        double temp;
        temp = a;
        a = b;
        b = temp;
    }

    public static void gaussj(double[][,] a, int n, double[][,] b, int m)
    {
        int i, icol = 0, irow = 0, j, k, l, ll;
        double big = 0.0, dum = 0.0, pivinv = 0.0;

        int[] indxc = new int[3];
        int[] indxr = new int[3];
        int[] ipiv = new int[3];

        for (j = 0; j < n; j++)
            ipiv[j] = 0;

        for (i = 0; i < n; i++)
        {
            big = 0.0;
            for (j = 0; j < n; j++)
                if (ipiv[j] != 1)
                    for (k = 0; k < n; k++)
                    {
                        if (ipiv[k] == 0)
                        {
                            if (Math.Abs(jaggeda[2][j, k]) >= big)
                            {
                                big = Math.Abs(jaggeda[2][j, k]);
                                irow = j;
                                icol = k;
                            }
                        }
                        else if (ipiv[k] > 1)
                            Console.WriteLine("GAUSSJ: Singular matrix-1\n");
                    }
            ++(ipiv[icol]);
            if (irow != icol)
            {
                for (l = 0; l < n; l++) swap(jaggeda[2][irow, l], jaggeda[2][icol, l]);
                for (l = 0; l < m; l++) swap(jaggedb[1][irow, l], jaggedb[1][icol, l]);
            }

            indxr[i] = irow;
            indxc[i] = icol;
            if (jaggeda[2][icol, icol] == 0.0)
                Console.WriteLine("GAUSSJ: Singular Matrix-2. icol is {0}\n", icol);
            pivinv = 1.0 / jaggeda[2][icol, icol];
            jaggeda[2][icol, icol] = 1.0;
            for (l = 0; l < n; l++) jaggeda[2][icol, l] *= pivinv;
            for (l = 0; l < m; l++) jaggedb[1][icol, l] *= pivinv;
            for (ll = 0; ll < n; ll++)
                if (ll != icol)
                {
                    dum = jaggeda[2][ll, icol];
                    jaggeda[2][ll, icol] = 0.0;
                    for (l = 0; l < n; l++) jaggeda[2][ll, l] -= jaggeda[2][icol, l] * dum;
                    for (l = 0; l < m; l++) jaggedb[1][ll, l] -= jaggedb[1][icol, l] * dum;
                }
        }
        for (l = n - 1; l >= 0; l--)
        {
            if (indxr[l] != indxc[l])
                for (k = 0; k < n; k++)
                    swap(jaggeda[2][k, indxr[l]], jaggeda[2][k, indxc[l]]);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = false;

        Console.WriteLine("Solving AX=B and the inverse of A with Gauss-Jordan algorithm");
        int n = 3;
        int m = 1;

        jaggeda = new double[3][,];
        jaggeda[2] = new double[3, 3];

        jaggedb = new double[5][,];
        jaggedb[1] = new double[3, 1];

        jaggeda[2][0, 0] = 1;
        jaggeda[2][0, 1] = 1;
        jaggeda[2][0, 2] = 1;
        jaggeda[2][1, 0] = 1;
        jaggeda[2][1, 1] = 2;
        jaggeda[2][1, 2] = 4;
        jaggeda[2][2, 0] = 1;
        jaggeda[2][2, 1] = 3;
        jaggeda[2][2, 2] = 9;

        jaggedb[1][0, 0] = -1;
        jaggedb[1][1, 0] = 3;
        jaggedb[1][2, 0] = 3;

        /*
		int i, j;
				
		Console.WriteLine("Matrix A is \n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<n; j++)
				Console.Write("{0}\t", jaggeda[2][i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("Matrix B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", jaggedb[1][i,j]);
			Console.WriteLine();
		}
		*/

        gaussj(jaggeda, n, jaggedb, m);

        /*
		Console.WriteLine();
		Console.WriteLine("The inverse of matrix A is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<n; j++)
				Console.Write("{0}\t", jaggeda[2][i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("The solution X of AX=B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", jaggedb[1][i,j]);
			Console.WriteLine();
		}
		*/

        if (
               AreEqual(jaggeda[2][0, 0], 3)
            && AreEqual(jaggeda[2][1, 1], 4)
            && AreEqual(jaggedb[1][0, 0], -9)
            && AreEqual(jaggedb[1][1, 0], 10)
            )
            pass = true;

        if (!pass)
        {
            Console.WriteLine("FAILED");
            return 1;
        }
        else
        {
            Console.WriteLine("PASSED");
            return 100;
        }
    }
}
