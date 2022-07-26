// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
/// <license>
/// This is a port of the SciMark2a Java Benchmark to C# by
/// Chris Re (cmr28@cornell.edu) and Werner Vogels (vogels@cs.cornell.edu)
///
/// For details on the original authors see http://math.nist.gov/scimark2
///
/// This software is likely to burn your processor, bitflip your memory chips
/// anihilate your screen and corrupt all your disks, so you it at your
/// own risk.
/// </license>


using System;

namespace SciMark2
{
    /// <summary>
    /// LU matrix factorization. (Based on TNT implementation.)
    /// Decomposes a matrix A  into a triangular lower triangular
    /// factor (L) and an upper triangular factor (U) such that
    /// A = L*U.  By convnetion, the main diagonal of L consists
    /// of 1's so that L and U can be stored compactly in
    /// a NxN matrix.
    /// </summary>
    public class LU
    {
        private double[][] _LU;
        private int[] _pivot;

        /// <summary>
        /// Returns a <em>copy</em> of the compact LU factorization.
        /// (useful mainly for debugging.)
        /// </summary>
        ///
        /// <returns>
        /// the compact LU factorization.  The U factor
        /// is stored in the upper triangular portion, and the L
        /// factor is stored in the lower triangular portion.
        /// The main diagonal of L consists (by convention) of
        /// ones, and is not explicitly stored.
        /// </returns>
        public static double num_flops(int N)
        {
            // rougly 2/3*N^3

            double Nd = (double)N;

            return (2.0 * Nd * Nd * Nd / 3.0);
        }

        protected internal static double[] new_copy(double[] x)
        {
            double[] T = new double[x.Length];
            x.CopyTo(T, 0);
            return T;
        }


        protected internal static double[][] new_copy(double[][] A)
        {
            int M = A.Length;
            int N = A[0].Length;

            double[][] T = new double[M][];
            for (int i = 0; i < M; i++)
            {
                T[i] = new double[N];
            }

            for (int i = 0; i < M; i++)
            {
                A[i].CopyTo(T[i], 0);
            }

            return T;
        }



        public static int[] new_copy(int[] x)
        {
            int[] T = new int[x.Length];
            x.CopyTo(T, 0);
            return T;
        }

        protected internal static void insert_copy(double[][] B, double[][] A)
        {
            for (int i = 0; i < A.Length; i++)
            {
                A[i].CopyTo(B[i], 0);
            }
        }

        /// <summary>
        /// Initialize LU factorization from matrix.
        /// </summary>
        /// <param name="A">
        /// (in) the matrix to associate with this factorization.
        ///
        /// </param>
        public LU(double[][] A)
        {
            _LU = new_copy(A);
            _pivot = new int[A.Length];

            factor(_LU, _pivot);
        }

        /// <summary>
        /// Solve a linear system, with pre-computed factorization.
        /// </summary>
        /// <param name="b">
        /// (in) the right-hand side.
        /// </param>
        /// <returns>
        /// solution vector.
        /// </returns>
        public virtual double[] solve(double[] b)
        {
            double[] x = new_copy(b);

            solve(_LU, _pivot, x);
            return x;
        }


        /// <summary>
        /// LU factorization (in place).
        /// </summary>
        /// <param name="A">
        /// (in/out) On input, the matrix to be factored.
        /// On output, the compact LU factorization.
        /// </param>
        /// <param name="pivot">
        /// (out) The pivot vector records the
        /// reordering of the rows of A during factorization.
        /// </param>
        /// <returns>
        /// 0, if OK, nozero value, othewise.
        /// </returns>
        public static int factor(double[][] A, int[] pivot)
        {
            int N = A.Length;
            int M = A[0].Length;

            int minMN = Math.Min(M, N);

            for (int j = 0; j < minMN; j++)
            {
                // find pivot in column j and  test for singularity.
                int jp = j;

                double t = Math.Abs(A[j][j]);
                for (int i = j + 1; i < M; i++)
                {
                    double ab = Math.Abs(A[i][j]);
                    if (ab > t)
                    {
                        jp = i;
                        t = ab;
                    }
                }

                pivot[j] = jp;

                // jp now has the index of maximum element
                // of column j, below the diagonal
                if (A[jp][j] == 0)
                    return 1;

                // factorization failed because of zero pivot
                if (jp != j)
                {
                    // swap rows j and jp
                    double[] tA = A[j];
                    A[j] = A[jp];
                    A[jp] = tA;
                }

                if (j < M - 1)
                {
                    // compute elements j+1:M of jth column
                    // note A(j,j), was A(jp,p) previously which was
                    // guarranteed not to be zero (Label #1)
                    //
                    double recp = 1.0 / A[j][j];

                    for (int k = j + 1; k < M; k++)
                        A[k][j] *= recp;
                }

                if (j < minMN - 1)
                {
                    // rank-1 update to trailing submatrix:   E = E - x*y;
                    //
                    // E is the region A(j+1:M, j+1:N)
                    // x is the column vector A(j+1:M,j)
                    // y is row vector A(j,j+1:N)
                    for (int ii = j + 1; ii < M; ii++)
                    {
                        double[] Aii = A[ii];
                        double[] Aj = A[j];
                        double AiiJ = Aii[j];
                        for (int jj = j + 1; jj < N; jj++)
                            Aii[jj] -= AiiJ * Aj[jj];
                    }
                }
            }

            return 0;
        }


        /// <summary>Solve a linear system, using a prefactored matrix
        /// in LU form.
        /// </summary>
        /// <param name="A">(in) the factored matrix in LU form.
        /// </param>
        /// <param name="pivot">(in) the pivot vector which lists
        /// the reordering used during the factorization
        /// stage.
        /// </param>
        /// <param name="b">   (in/out) On input, the right-hand side.
        /// On output, the solution vector.
        ///
        /// </param>
        public static void solve(double[][] A, int[] pvt, double[] b)
        {
            int M = A.Length;
            int N = A[0].Length;
            int ii = 0;

            for (int i = 0; i < M; i++)
            {
                int ip = pvt[i];
                double sum = b[ip];

                b[ip] = b[i];
                if (ii == 0)
                {
                    for (int j = ii; j < i; j++)
                    {
                        sum -= A[i][j] * b[j];
                    }
                }
                else
                {
                    if (sum == 0.0)
                    {
                        ii = i;
                    }
                }
                b[i] = sum;
            }

            for (int i = N - 1; i >= 0; i--)
            {
                double sum = b[i];
                for (int j = i + 1; j < N; j++)
                {
                    sum -= A[i][j] * b[j];
                }
                b[i] = sum / A[i][i];
            }
        }
    }
}