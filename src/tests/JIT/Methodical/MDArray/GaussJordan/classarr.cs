// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Solving AX=B and the inverse of A with Gauss-Jordan algorithm

using System;
using Xunit;

public class MatrixCls
{
    public double[,] arr;
    public MatrixCls(int n, int m)
    {
        arr = new double[n, m];
    }
}

public class classarr
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

    public static void gaussj(MatrixCls a, int n, MatrixCls b, int m)
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
                            if (Math.Abs(a.arr[j, k]) >= big)
                            {
                                big = Math.Abs(a.arr[j, k]);
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
                for (l = 0; l < n; l++) swap(a.arr[irow, l], a.arr[icol, l]);
                for (l = 0; l < m; l++) swap(b.arr[irow, l], b.arr[icol, l]);
            }

            indxr[i] = irow;
            indxc[i] = icol;
            if (a.arr[icol, icol] == 0.0)
                Console.WriteLine("GAUSSJ: Singular Matrix-2. icol is {0}\n", icol);
            pivinv = 1.0 / a.arr[icol, icol];
            a.arr[icol, icol] = 1.0;
            for (l = 0; l < n; l++) a.arr[icol, l] *= pivinv;
            for (l = 0; l < m; l++) b.arr[icol, l] *= pivinv;
            for (ll = 0; ll < n; ll++)
                if (ll != icol)
                {
                    dum = a.arr[ll, icol];
                    a.arr[ll, icol] = 0.0;
                    for (l = 0; l < n; l++) a.arr[ll, l] -= a.arr[icol, l] * dum;
                    for (l = 0; l < m; l++) b.arr[ll, l] -= b.arr[icol, l] * dum;
                }
        }
        for (l = n - 1; l >= 0; l--)
        {
            if (indxr[l] != indxc[l])
                for (k = 0; k < n; k++)
                    swap(a.arr[k, indxr[l]], a.arr[k, indxc[l]]);
        }
    }

    [Fact]
    [OuterLoop]
    public static int TestEntryPoint()
    {
        bool pass = false;

        Console.WriteLine("Solving AX=B and the inverse of A with Gauss-Jordan algorithm");
        int n = 3;
        int m = 1;

        MatrixCls a = new MatrixCls(3, 3);
        MatrixCls b = new MatrixCls(3, 1);

        a.arr[0, 0] = 1;
        a.arr[0, 1] = 1;
        a.arr[0, 2] = 1;
        a.arr[1, 0] = 1;
        a.arr[1, 1] = 2;
        a.arr[1, 2] = 4;
        a.arr[2, 0] = 1;
        a.arr[2, 1] = 3;
        a.arr[2, 2] = 9;

        b.arr[0, 0] = -1;
        b.arr[1, 0] = 3;
        b.arr[2, 0] = 3;

        /*
		int i, j;
				
		Console.WriteLine("Matrix A is \n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<n; j++)
				Console.Write("{0}\t", a.arr[i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("Matrix B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", b.arr[i,j]);
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
				Console.Write("{0}\t", a.arr[i,j]);
			Console.WriteLine();
		}

		Console.WriteLine();
		Console.WriteLine("The solution X of AX=B is:\n");
		for (i=0; i<n; i++)
		{
			for (j=0; j<m; j++)
				Console.Write("{0}\t", b.arr[i,j]);
			Console.WriteLine();
		}
		*/

        if (
               AreEqual(a.arr[0, 0], 3)
            && AreEqual(a.arr[1, 1], 4)
            && AreEqual(b.arr[0, 0], -9)
            && AreEqual(b.arr[1, 0], 10)
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
