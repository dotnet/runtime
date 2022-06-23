// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Solving AX=B and the inverse of A with Gauss-Jordan algorithm

using System;
using Xunit;

public class plainarr
{
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

    public static void gaussj(double[,] a, int n, double[,] b, int m)
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
                            if (Math.Abs(a[j, k]) >= big)
                            {
                                big = Math.Abs(a[j, k]);
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
                for (l = 0; l < n; l++) swap(a[irow, l], a[icol, l]);
                for (l = 0; l < m; l++) swap(b[irow, l], b[icol, l]);
            }

            indxr[i] = irow;
            indxc[i] = icol;
            if (a[icol, icol] == 0.0)
                Console.WriteLine("GAUSSJ: Singular Matrix-2. icol is {0}\n", icol);
            pivinv = 1.0 / a[icol, icol];
            a[icol, icol] = 1.0;
            for (l = 0; l < n; l++) a[icol, l] *= pivinv;
            for (l = 0; l < m; l++) b[icol, l] *= pivinv;
            for (ll = 0; ll < n; ll++)
                if (ll != icol)
                {
                    dum = a[ll, icol];
                    a[ll, icol] = 0.0;
                    for (l = 0; l < n; l++) a[ll, l] -= a[icol, l] * dum;
                    for (l = 0; l < m; l++) b[ll, l] -= b[icol, l] * dum;
                }
        }
        for (l = n - 1; l >= 0; l--)
        {
            if (indxr[l] != indxc[l])
                for (k = 0; k < n; k++)
                    swap(a[k, indxr[l]], a[k, indxc[l]]);
        }
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool pass = false;

        Console.WriteLine("Solving AX=B and the inverse of A with Gauss-Jordan algorithm");
        int n = 3;
        int m = 1;

        double[,] a = new double[3, 3];
        double[,] b = new double[3, 1];

        a[0, 0] = 1;
        a[0, 1] = 1;
        a[0, 2] = 1;
        a[1, 0] = 1;
        a[1, 1] = 2;
        a[1, 2] = 4;
        a[2, 0] = 1;
        a[2, 1] = 3;
        a[2, 2] = 9;

        b[0, 0] = -1;
        b[1, 0] = 3;
        b[2, 0] = 3;

        /*
		int i, j;
				
		Console.WriteLine("Matrix A is \n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<n; j++)
				Console.Write("{0}\t", a[i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("Matrix B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", b[i,j]);
			Console.WriteLine();
		}
		*/

        gaussj(a, n, b, m);

        /*
		Console.WriteLine();
		Console.WriteLine("The inverse of matrix A is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<n; j++)
				Console.Write("{0}\t", a[i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("The solution X of AX=B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", b[i,j]);
			Console.WriteLine();
		}
		*/

        if (
               AreEqual(a[0, 0], 3)
            && AreEqual(a[1, 1], 4)
            && AreEqual(b[0, 0], -9)
            && AreEqual(b[1, 0], 10)
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
