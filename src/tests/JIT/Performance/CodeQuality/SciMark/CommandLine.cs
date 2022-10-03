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
    /// SciMark2: A Java numerical benchmark measuring performance
    /// of computational kernels for FFTs, Monte Carlo simulation,
    /// sparse matrix computations, Jacobi SOR, and dense LU matrix
    /// factorizations.
    /// </summary>

    public class CommandLine
    {
        /// <summary>
        ///  Benchmark 5 kernels with individual Mflops.
        ///  "results[0]" has the average Mflop rate.
        /// </summary>
        public static int Main(System.String[] args)
        {
#if DEBUG
            double min_time = Constants.RESOLUTION_TINY;
#else
            double min_time = Constants.RESOLUTION_DEFAULT;
#endif

            int FFT_size = Constants.FFT_SIZE;
            int SOR_size = Constants.SOR_SIZE;
            int Sparse_size_M = Constants.SPARSE_SIZE_M;
            int Sparse_size_nz = Constants.SPARSE_SIZE_nz;
            int LU_size = Constants.LU_SIZE;

            // look for runtime options
            if (args.Length > 0)
            {
                if (args[0].ToUpper().Equals("-h") ||
                    args[0].ToUpper().Equals("-help"))
                {
                    Console.WriteLine("Usage: [-large] [iterations]");
                    return -1;
                }

                int current_arg = 0;
                if (args[current_arg].ToUpper().Equals("-LARGE"))
                {
                    FFT_size = Constants.LG_FFT_SIZE;
                    SOR_size = Constants.LG_SOR_SIZE;
                    Sparse_size_M = Constants.LG_SPARSE_SIZE_M;
                    Sparse_size_nz = Constants.LG_SPARSE_SIZE_nz;
                    LU_size = Constants.LG_LU_SIZE;

                    current_arg++;
                }

                if (args.Length > current_arg)
                    min_time = Double.Parse(args[current_arg]);
            }
            Console.WriteLine("**                                                               **");
            Console.WriteLine("** SciMark2a Numeric Benchmark, see http://math.nist.gov/scimark **");
            Console.WriteLine("**                                                               **");

            // run the benchmark
            double[] res = new double[6];
            SciMark2.Random R = new SciMark2.Random(Constants.RANDOM_SEED);

            Console.WriteLine("Minimum running time = {0} seconds", min_time);

            res[1] = kernel.measureFFT(FFT_size, min_time, R);

            res[2] = kernel.measureSOR(SOR_size, min_time, R);

            res[3] = kernel.measureMonteCarlo(min_time, R);

            res[4] = kernel.measureSparseMatmult(Sparse_size_M, Sparse_size_nz, min_time, R);

            res[5] = kernel.measureLU(LU_size, min_time, R);

            res[0] = (res[1] + res[2] + res[3] + res[4] + res[5]) / 5;

            // print out results
            Console.WriteLine();
            Console.WriteLine("Composite Score: {0:F2} MFlops", res[0]);
            Console.WriteLine("FFT            : {0} - ({1})", res[1] == 0.0 ? "ERROR, INVALID NUMERICAL RESULT!" : res[1].ToString("F2"), FFT_size);
            Console.WriteLine("SOR            : {1:F2} - ({0}x{0})", SOR_size, res[2]);
            Console.WriteLine("Monte Carlo    :  {0:F2}", res[3]);
            Console.WriteLine("Sparse MatMult : {2:F2} - (N={0}, nz={1})", Sparse_size_M, Sparse_size_nz, res[4]);
            Console.WriteLine("LU             : {1} - ({0}x{0})", LU_size, res[1] == 0.0 ? "ERROR, INVALID NUMERICAL RESULT!" : res[5].ToString("F2"));

            return 100;
        }
    }
}
