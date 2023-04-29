// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// This benchmark is also in the github.com/dotnet/performance repo,
// (in Benchmark.NET form). It is duplicated here for the purposes of tracking
// correctness and assembly diffs.
//
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Diagnostics;
using Xunit;

public class BilinearTest
{
    int returnVal = 100;

    // Set this to a larger value to measure as a benchmark.
    int nbIter = 10;

    // These must be a multiple of the largest available vector of doubles.
    const int inputVectorSize = 1024;
    const int outputVectorSize = 1024;
    const double minXA = 0.0;
    const double maxXA = Math.PI;
    const int lengthA = 500;
    const double deltaA = (maxXA - minXA) / (double)(lengthA - 1);

    const double minXB = minXA + 2.0 * deltaA;
    const double maxXB = maxXA - 2.0 * deltaA;
    const double weightB = 0.15;
    const int lengthB = 500;
    const double deltaB = (maxXB - minXB) / (double)(lengthB - 1);

    //ref values 
    double[] A, B, input, output;

    internal void Setup()
    {
        A = new double[lengthA];
        B = new double[lengthB];
        for (int i = 0; i < lengthA; i++) A[i] = Math.Cos(i * deltaA + minXA);
        for (int i = 0; i < lengthB; i++) B[i] = Math.Cos(i * deltaB + minXB);

        //Init X values
        input = new double[inputVectorSize];
        double incr = (maxXA - minXA) / (double)(inputVectorSize - 3);
        for (int i = 0; i < inputVectorSize; i++) input[i] = minXA + (i - 1) * incr;
    }

    private static double[] BilinearInterpol(double[] x,
                                             double[] A,
                                             double minXA,
                                             double maxXA,
                                             double[] B,
                                             double minXB,
                                             double maxXB,
                                             double weightB)
    {
        double[] z = new double[outputVectorSize];

        var weightA = 1.0 - weightB;

        var deltaA = (maxXA - minXA) / (double)(A.Length - 1);
        var deltaB = (maxXB - minXB) / (double)(B.Length - 1);

        var invDeltaA = 1.0 / deltaA;
        var invDeltaB = 1.0 / deltaB;

        for (var i = 0; i < x.Length; i++)
        {
            var currentX = x[i];

            // Determine the largest a, such that A[i] = f(xA) and xA <= x[i].
            // This involves casting from double to int.
            var a = (int)((currentX - minXA) * invDeltaA);
            // We must ensure that it lies within our available range.
            a = Math.Max(0, Math.Min(a, A.Length - 1));
            var aPlusOne = Math.Min(a + 1, A.Length - 1);

            // Now, get the reference input, xA, for our index a.
            // This involves casting from  int to double.
            var xA = (a * deltaA) + minXA;

            // Now, compute the lambda for our A reference point.
            var currentXNormA = Math.Max(minXA, Math.Min(currentX, maxXA));
            var lambdaA = (currentXNormA - xA) * invDeltaA;

            // Finally, get our A reference points.
            var refALower = A[a];
            var refAUpper = A[aPlusOne];

            // Now, do the all of the above for our B reference point.
            var b = (int)((currentX - minXB) * invDeltaB);
            b = Math.Max(0, Math.Min(b, B.Length - 1));
            var bPlusOne = Math.Min(b + 1, B.Length - 1);
            var xB = (b * deltaB) + minXB;
            var currentXNormB = Math.Max(minXB, Math.Min(currentX, maxXB));
            var lambdaB = (currentXNormB - xB) * invDeltaB;
            var refBLower = B[b];
            var refBUpper = B[bPlusOne];

            // Finally, compute our result.
            z[i] = weightA * (refALower + lambdaA * (refAUpper - refALower)) +
                    weightB * (refBLower + lambdaB * (refBUpper - refBLower));
        }
        return z;
    }

    private double[] BilinearInterpol_Vector(
                                            double[] x,
                                            double[] A,
                                            double minXA,
                                            double maxXA,
                                            double[] B,
                                            double minXB,
                                            double maxXB,
                                            double weightB)
    {
        double[] z = new double[outputVectorSize];

        var vWeightB = new Vector<double>(weightB);
        var vWeightA = new Vector<double>(1 - weightB);

        var vMinXA = new Vector<double>(minXA);
        var vMaxXA = new Vector<double>(maxXA);
        var vMinXB = new Vector<double>(minXB);
        var vMaxXB = new Vector<double>(maxXB);

        var deltaA = (maxXA - minXA) / (double)(A.Length - 1);
        var deltaB = (maxXB - minXB) / (double)(B.Length - 1);
        var vDeltaA = new Vector<double>(deltaA);
        var vDeltaB = new Vector<double>(deltaB);

        var vInvDeltaA = Vector<double>.One / vDeltaA;
        var vInvDeltaB = Vector<double>.One / vDeltaB;

        var ALengthMinusOne = new Vector<int>(A.Length - 1);
        var BLengthMinusOne = new Vector<int>(B.Length - 1);

        double[] doubleTemp = new double[Vector<double>.Count];
        for (var i = 0; i < x.Length; i += Vector<double>.Count)
        {
            var currentX = new Vector<double>(x, i);

            // Determine the largest a, such that A[i] = f(xA) and xA <= x[i].
            // This involves casting from double to int; here we use two Vector conversions.
            Vector<int> a = Vector.ConvertToInt32(Vector.Narrow((currentX - vMinXA) * vInvDeltaA, Vector<double>.Zero));
            a = Vector.Min(Vector.Max(a, Vector<int>.Zero), ALengthMinusOne);
            Vector<int> aPlusOne = Vector.Min(a + Vector<int>.One, ALengthMinusOne);

            // Now, get the reference input, xA, for our index a.
            // This involves casting from  int to double.
            Vector<double> tALeft;
            Vector<double> tADummy;
            Vector.Widen(Vector.ConvertToSingle(a), out tALeft, out tADummy);
            Vector<double> xA = (tALeft * vDeltaA) + vMinXA;

            // Now, compute the lambda for our A reference point.
            Vector<double> currentXNormA = Vector.Max(vMinXA, Vector.Min(currentX, vMaxXA));
            Vector<double> lambdaA = (currentXNormA - xA) * vInvDeltaA;

            // Now, we need to load up our reference points.
            // This is basically a "gather" operation, for which we use a temporary double array.
            for (var j = 0; j < Vector<double>.Count; j++) doubleTemp[j] = A[a[j]];
            Vector<double> AVector = new Vector<double>(doubleTemp);
            for (var j = 0; j < Vector<double>.Count; j++) doubleTemp[j] = A[aPlusOne[j]];
            Vector<double> AVectorPlusOne = new Vector<double>(doubleTemp);

            // Now, do the all of the above for our B reference point.
            Vector<int> b = Vector.ConvertToInt32(Vector.Narrow((currentX - vMinXB) * vInvDeltaB, Vector<double>.Zero));
            b = Vector.Min(Vector.Max(b, Vector<int>.Zero), BLengthMinusOne);
            Vector<int> bPlusOne = Vector.Min(b + Vector<int>.One, BLengthMinusOne);

            Vector<double> tBLeft;
            Vector<double> tBDummy;
            Vector.Widen(Vector.ConvertToSingle(b), out tBLeft, out tBDummy);
            Vector<double> xB = (tBLeft * vDeltaB) + vMinXB;

            Vector<double> currentXNormB = Vector.Max(vMinXB, Vector.Min(currentX, vMaxXB));
            Vector<double> lambdaB = (currentXNormB - xB) * vInvDeltaB;

            for (var j = 0; j < Vector<double>.Count; j++) doubleTemp[j] = B[b[j]];
            Vector<double> BVector = new Vector<double>(doubleTemp);
            for (var j = 0; j < Vector<double>.Count; j++) doubleTemp[j] = B[bPlusOne[j]];
            Vector<double> BVectorPlusOne = new Vector<double>(doubleTemp);

            Vector<double> newZ = vWeightA * (AVector + lambdaA * (AVectorPlusOne - AVector)) +
                        vWeightB * (BVector + lambdaB * (BVectorPlusOne - BVector));
            newZ.CopyTo(z, i);
        }
        return z;
    }

    private static unsafe double[] BilinearInterpol_AVX(
                                            double[] x,
                                            double[] A,
                                            double minXA,
                                            double maxXA,
                                            double[] B,
                                            double minXB,
                                            double maxXB,
                                            double weightB)
    {
        double[] z = new double[outputVectorSize];

        fixed (double* pX = &x[0], pA = &A[0], pB = &B[0], pZ = &z[0])
        {
            Vector256<double> vWeightB = Vector256.Create(weightB);
            Vector256<double> vWeightA = Vector256.Create(1 - weightB);

            Vector256<double> vMinXA = Vector256.Create(minXA);
            Vector256<double> vMaxXA = Vector256.Create(maxXA);
            Vector256<double> vMinXB = Vector256.Create(minXB);
            Vector256<double> vMaxXB = Vector256.Create(maxXB);

            double deltaA = (maxXA - minXA) / (double)(A.Length - 1);
            double deltaB = (maxXB - minXB) / (double)(B.Length - 1);
            Vector256<double> vDeltaA = Vector256.Create(deltaA);
            Vector256<double> vDeltaB = Vector256.Create(deltaB);

            double invDeltaA = 1.0 / deltaA;
            double invDeltaB = 1.0 / deltaB;
            Vector256<double> vInvDeltaA = Vector256.Create(invDeltaA);
            Vector256<double> vInvDeltaB = Vector256.Create(invDeltaB);

            Vector128<int> ALengthMinusOne = Vector128.Create(A.Length - 1);
            Vector128<int> BLengthMinusOne = Vector128.Create(B.Length - 1);
            Vector128<int> One = Vector128.Create(1);

            for (var i = 0; i < x.Length; i += Vector256<double>.Count)
            {
                Vector256<double> currentX = Avx.LoadVector256(pX + i);

                // Determine the largest a, such that A[i] = f(xA) and xA <= x[i].
                // This involves casting from double to int; here we use a Vector conversion.
                Vector256<double> aDouble = Avx.Multiply(Avx.Subtract(currentX, vMinXA), vInvDeltaA);
                Vector128<int> a = Avx.ConvertToVector128Int32WithTruncation(aDouble);
                a = Sse41.Min(Sse41.Max(a, Vector128<int>.Zero), ALengthMinusOne);
                Vector128<int> aPlusOne = Sse41.Min(Sse2.Add(a, One), ALengthMinusOne);

                // Now, get the reference input, xA, for our index a.
                // This involves casting from  int to double.
                Vector256<double> xA = Avx.Add(Avx.Multiply(Avx.ConvertToVector256Double(a), vDeltaA), vMinXA);

                // Now, compute the lambda for our A reference point.
                Vector256<double> currentXNormA = Avx.Max(vMinXA, Avx.Min(currentX, vMaxXA));
                Vector256<double> lambdaA = Avx.Multiply(Avx.Subtract(currentXNormA, xA), vInvDeltaA);

                // Now, we need to load up our reference points using Vector Gather operations.
                Vector256<double> AVector = Avx2.GatherVector256(pA, a, 8);
                Vector256<double> AVectorPlusOne = Avx2.GatherVector256(pA, aPlusOne, 8);

                // Now, do the all of the above for our B reference point.
                Vector256<double> bDouble = Avx.Multiply(Avx.Subtract(currentX, vMinXB), vInvDeltaB);
                Vector128<int> b = Avx.ConvertToVector128Int32WithTruncation(bDouble);
                b = Sse41.Min(Sse41.Max(b, Vector128<int>.Zero), BLengthMinusOne);
                Vector128<int> bPlusOne = Sse41.Min(Sse2.Add(b, One), BLengthMinusOne);

                Vector256<double> xB = Avx.Add(Avx.Multiply(Avx.ConvertToVector256Double(b), vDeltaB), vMinXB);
                Vector256<double> currentXNormB = Avx.Max(vMinXB, Avx.Min(currentX, vMaxXB));
                Vector256<double> lambdaB = Avx.Multiply(Avx.Subtract(currentXNormB, xB), vInvDeltaB);

                Vector256<double> BVector = Avx2.GatherVector256(pB, b, 8);
                Vector256<double> BVectorPlusOne = Avx2.GatherVector256(pB, bPlusOne, 8);

                Vector256<double> newZ = Avx.Add(Avx.Multiply(vWeightA, Avx.Add(AVector, Avx.Multiply(lambdaA, Avx.Subtract(AVectorPlusOne, AVector)))),
                                             Avx.Multiply(vWeightB, Avx.Add(BVector, Avx.Multiply(lambdaB, Avx.Subtract(BVectorPlusOne, BVector)))));
                Avx.Store(pZ + i, newZ);
            }
        }
        return z;
    }

    public static bool CheckResult(double[] output, double[] vectorOutput)
    {
        double eps = 1e-16;
        for (int i = 0; i < output.Length; i++)
        {
            if (Math.Abs(output[i] - vectorOutput[i]) > eps)
            {
                Console.WriteLine("Failed at " + i + ": output is " + output[i] + ", vectorOutput is " + vectorOutput[i]);
                return false;
            }
        }
        return true;
    }

    internal void RunTests()
    {
        Setup();

        double[] output = BilinearInterpol(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
        Stopwatch timer = new Stopwatch();
        timer.Start();
        for (int i = 0; i < nbIter; i++)
        {
            output = BilinearInterpol(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
        }
        timer.Stop();
        Console.WriteLine("Interpolation time: " + timer.ElapsedMilliseconds + "ms (" + nbIter + " iterations)");

        // Vector

        double[] vectorOutput = BilinearInterpol_Vector(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
        Stopwatch timer2 = new Stopwatch();
        timer2.Start();
        for (int i = 0; i < nbIter; i++)
        {
            vectorOutput = BilinearInterpol_Vector(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
        }
        timer2.Stop();
        Console.WriteLine("Interpolation Vector time: " + timer2.ElapsedMilliseconds + "ms (" + nbIter + " iterations)");
        if (!CheckResult(output, vectorOutput))
        {
            returnVal = -1;
        }

        // AVX

        if (Avx2.IsSupported)
        {
            vectorOutput = BilinearInterpol_AVX(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
            Stopwatch timer3 = new Stopwatch();
            timer3.Start();
            for (int i = 0; i < nbIter; i++)
            {
                vectorOutput = BilinearInterpol_AVX(input, A, minXA, maxXA, B, minXB, maxXB, weightB);
            }
            timer3.Stop();
            Console.WriteLine("Interpolation AVX time: " + timer3.ElapsedMilliseconds + "ms (" + nbIter + " iterations)");
            if (!CheckResult(output, vectorOutput))
            {
                returnVal = -1;
            }
        }
    }
    [Fact]
    public static int TestEntryPoint()
    {
        BilinearTest test = new BilinearTest();
        test.RunTests();
        return test.returnVal;
    }
}
