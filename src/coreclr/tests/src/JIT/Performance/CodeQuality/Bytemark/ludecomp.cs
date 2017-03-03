// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
/*
** This program was translated to C# and adapted for xunit-performance.
** New variants of several tests were added to compare class versus 
** struct and to compare jagged arrays vs multi-dimensional arrays.
*/

/*
** BYTEmark (tm)
** BYTE Magazine's Native Mode benchmarks
** Rick Grehan, BYTE Magazine
**
** Create:
** Revision: 3/95
**
** DISCLAIMER
** The source, executable, and documentation files that comprise
** the BYTEmark benchmarks are made available on an "as is" basis.
** This means that we at BYTE Magazine have made every reasonable
** effort to verify that the there are no errors in the source and
** executable code.  We cannot, however, guarantee that the programs
** are error-free.  Consequently, McGraw-HIll and BYTE Magazine make
** no claims in regard to the fitness of the source code, executable
** code, and documentation of the BYTEmark.
** 
** Furthermore, BYTE Magazine, McGraw-Hill, and all employees
** of McGraw-Hill cannot be held responsible for any damages resulting
** from the use of this code or the results obtained from using
** this code.
*/

using System;

/***********************
**  LU DECOMPOSITION  **
** (Linear Equations) **
************************
** These routines come from "Numerical Recipes in Pascal".
** Note that, as in the assignment algorithm, though we
** separately define LUARRAYROWS and LUARRAYCOLS, the two
** must be the same value (this routine depends on a square
** matrix).
*/

internal class LUDecomp : LUStruct
{
    private const int MAXLUARRAYS = 1000;

    private static double[] s_LUtempvv;

    public override string Name()
    {
        return "LU DECOMPOSITION";
    }

    public override double Run()
    {
        double[][] a;
        double[] b;
        double[][][] abase = null;
        double[][] bbase = null;
        int n;
        int i;
        long accumtime;
        double iterations;

        /*
        ** Our first step is to build a "solvable" problem.  This
        ** will become the "seed" set that all others will be
        ** derived from. (I.E., we'll simply copy these arrays
        ** into the others.
        */
        a = new double[global.LUARRAYROWS][];
        for (int j = 0; j < global.LUARRAYROWS; j++)
        {
            a[j] = new double[global.LUARRAYCOLS];
        }
        b = new double[global.LUARRAYROWS];
        n = global.LUARRAYROWS;

        s_LUtempvv = new double[global.LUARRAYROWS];

        build_problem(a, n, b);

        if (this.adjust == 0)
        {
            for (i = 1; i <= MAXLUARRAYS; i++)
            {
                abase = new double[i + 1][][];
                for (int j = 0; j < i + 1; j++)
                {
                    abase[j] = new double[global.LUARRAYROWS][];
                    for (int k = 0; k < global.LUARRAYROWS; k++)
                    {
                        abase[j][k] = new double[global.LUARRAYCOLS];
                    }
                }

                bbase = new double[i + 1][];
                for (int j = 0; j < i + 1; j++)
                {
                    bbase[j] = new double[global.LUARRAYROWS];
                }

                if (DoLUIteration(a, b, abase, bbase, i) > global.min_ticks)
                {
                    this.numarrays = i;
                    break;
                }
            }

            if (this.numarrays == 0)
            {
                throw new Exception("FPU:LU -- Array limit reached");
            }
        }
        else
        {
            abase = new double[this.numarrays][][];
            for (int j = 0; j < this.numarrays; j++)
            {
                abase[j] = new double[global.LUARRAYROWS][];
                for (int k = 0; k < global.LUARRAYROWS; k++)
                {
                    abase[j][k] = new double[global.LUARRAYCOLS];
                }
            }
            bbase = new double[this.numarrays][];
            for (int j = 0; j < this.numarrays; j++)
            {
                bbase[j] = new double[global.LUARRAYROWS];
            }
        }

        accumtime = 0;
        iterations = 0.0;

        do
        {
            accumtime += DoLUIteration(a, b, abase, bbase, this.numarrays);
            iterations += (double)this.numarrays;
        } while (ByteMark.TicksToSecs(accumtime) < this.request_secs);

        if (this.adjust == 0) this.adjust = 1;

        return iterations / ByteMark.TicksToFracSecs(accumtime);
    }

    private static long DoLUIteration(double[][] a, double[] b, double[][][] abase, double[][] bbase, int numarrays)
    {
        double[][] locabase;
        double[] locbbase;
        long elapsed;
        int k, j, i;

        for (j = 0; j < numarrays; j++)
        {
            locabase = abase[j];
            locbbase = bbase[j];
            for (i = 0; i < global.LUARRAYROWS; i++)
                for (k = 0; k < global.LUARRAYCOLS; k++)
                    locabase[i][k] = a[i][k];
            for (i = 0; i < global.LUARRAYROWS; i++)
                locbbase[i] = b[i];
        }

        elapsed = ByteMark.StartStopwatch();

        for (i = 0; i < numarrays; i++)
        {
            locabase = abase[i];
            locbbase = bbase[i];

            lusolve(locabase, global.LUARRAYROWS, locbbase);
        }

        return (ByteMark.StopStopwatch(elapsed));
    }

    private static void build_problem(double[][] a, int n, double[] b)
    {
        int i, j, k, k1;
        double rcon;

        ByteMark.randnum(13);

        for (i = 0; i < n; i++)
        {
            b[i] = (double)(ByteMark.abs_randwc(100) + 1);
            for (j = 0; j < n; j++)
                if (i == j)
                    a[i][j] = (double)(ByteMark.abs_randwc(1000) + 1);
                else
                    a[i][j] = (double)0.0;
        }

        for (i = 0; i < 8 * n; i++)
        {
            k = ByteMark.abs_randwc(n);
            k1 = ByteMark.abs_randwc(n);
            if (k != k1)
            {
                if (k < k1) rcon = 1.0;
                else rcon = -1.0;
                for (j = 0; j < n; j++)
                    a[k][j] += a[k1][j] * rcon;
                b[k] += b[k1] * rcon;
            }
        }
    }

    private static int ludcmp(double[][] a, int n, int[] indx, out int d)
    {
        double big;
        double sum;
        double dum;
        int i, j, k;
        int imax = 0;
        double tiny;

        tiny = 1.0e-20;
        d = 1;

        for (i = 0; i < n; i++)
        {
            big = 0.0;
            for (j = 0; j < n; j++)
                if (Math.Abs(a[i][j]) > big)
                    big = Math.Abs(a[i][j]);
            if (big == 0.0) return 0;
            s_LUtempvv[i] = 1.0 / big;
        }

        for (j = 0; j < n; j++)
        {
            if (j != 0)
                for (i = 0; i < j; i++)
                {
                    sum = a[i][j];
                    if (i != 0)
                        for (k = 0; k < i; k++)
                            sum -= a[i][k] * a[k][j];
                    a[i][j] = sum;
                }
            big = 0.0;

            for (i = j; i < n; i++)
            {
                sum = a[i][j];
                if (j != 0)
                    for (k = 0; k < j; k++)
                        sum -= a[i][k] * a[k][j];
                a[i][j] = sum;
                dum = s_LUtempvv[i] * Math.Abs(sum);
                if (dum >= big)
                {
                    big = dum;
                    imax = i;
                }
            }

            if (j != imax)
            {
                for (k = 0; k < n; k++)
                {
                    dum = a[imax][k];
                    a[imax][k] = a[j][k];
                    a[j][k] = dum;
                }
                d = -d;
                dum = s_LUtempvv[imax];
                s_LUtempvv[imax] = s_LUtempvv[j];
                s_LUtempvv[j] = dum;
            }

            indx[j] = imax;
            if (a[j][j] == 0.0)
                a[j][j] = tiny;
            if (j != (n - 1))
            {
                dum = 1.0 / a[j][j];
                for (i = j + 1; i < n; i++)
                    a[i][j] = a[i][j] * dum;
            }
        }

        return 1;
    }

    private static void lubksb(double[][] a, int n, int[] indx, double[] b)
    {
        int i, j;
        int ip;
        int ii;
        double sum;

        ii = -1;

        for (i = 0; i < n; i++)
        {
            ip = indx[i];
            sum = b[ip];
            b[ip] = b[i];
            if (ii != -1)
                for (j = ii; j < i; j++)
                    sum = sum - a[i][j] * b[j];
            else
                if (sum != (double)0.0)
                ii = i;
            b[i] = sum;
        }

        for (i = (n - 1); i >= 0; i--)
        {
            sum = b[i];
            if (i != (n - 1))
                for (j = (i + 1); j < n; j++)
                    sum = sum - a[i][j] * b[j];
            b[i] = sum / a[i][i];
        }
    }

    private static int lusolve(double[][] a, int n, double[] b)
    {
        int[] indx = new int[global.LUARRAYROWS];
        int d;

        if (ludcmp(a, n, indx, out d) == 0) return 0;

        lubksb(a, n, indx, b);

        return 1;
    }
}
