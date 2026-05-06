// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Functions
{
    public static partial class MathTests
    {
        // double has a machine epsilon of approx: 2.22e-16. However, due to floating-point precision
        // errors, this is too accurate when aggregating values of a set of iterations. Using the
        // single-precision machine epsilon as our epsilon should be 'good enough' for the purposes
        // of the perf testing as it ensures we get the expected value and that it is at least as precise
        // as we would have computed with the single-precision version of the function (without aggregation).
        private const double doubleEpsilon = 1.19e-07;

        // 5000 iterations is enough to cover the full domain of inputs for certain functions (such
        // as Cos, which has a domain of 0 to PI) at reasonable intervals (in the case of Cos, the
        // interval is PI / 5000 which is 0.0006283185307180). It should also give reasonable coverage
        // for functions which have a larger domain (such as Atan, which covers the full set of real numbers).
        private const int iterations = 5000;

        // float has a machine epsilon of approx: 1.19e-07. However, due to floating-point precision
        // errors, this is too accurate when aggregating values of a set of iterations. Using the
        // half-precision machine epsilon as our epsilon should be 'good enough' for the purposes
        // of the perf testing as it ensures we get the expected value and that it is at least as precise
        // as we would have computed with the half-precision version of the function (without aggregation).
        private const float singleEpsilon = 9.77e-04f;

        // While iterations covers the domain of inputs, the full span of results doesn't run long enough
        // to meet our siginificance criteria. So each test is repeated many times, using the factors below.
        private const int AbsDoubleIterations = 200000;
        private const int AcosDoubleIterations = 10000;
        private const int AsinDoubleIterations = 10000;
        private const int Atan2DoubleIterations = 6500;
        private const int AtanDoubleIterations = 13000;
        private const int CeilingDoubleIterations = 80000;
        private const int CosDoubleIterations = 16000;
        private const int CoshDoubleIterations = 8000;
        private const int ExpDoubleIterations = 16000;
        private const int FloorDoubleIterations = 80000;
        private const int Log10DoubleIterations = 16000;
        private const int LogDoubleIterations = 20000;
        private const int PowDoubleIterations = 4000;
        private const int RoundDoubleIterations = 35000;
        private const int SinDoubleIterations = 16000;
        private const int SinhDoubleIterations = 8000;
        private const int SqrtDoubleIterations = 40000;
        private const int TanDoubleIterations = 16000;
        private const int TanhDoubleIterations = 17000;

        private const int AbsSingleIterations = 200000;
        private const int AcosSingleIterations = 15000;
        private const int AsinSingleIterations = 15000;
        private const int Atan2SingleIterations = 9000;
        private const int AtanSingleIterations = 17000;
        private const int CeilingSingleIterations = 80000;
        private const int CosSingleIterations = 20000;
        private const int CoshSingleIterations = 10000;
        private const int ExpSingleIterations = 24000;
        private const int FloorSingleIterations = 80000;
        private const int Log10SingleIterations = 20000;
        private const int LogSingleIterations = 30000;
        private const int PowSingleIterations = 10000;
        private const int RoundSingleIterations = 35000;
        private const int SinSingleIterations = 20000;
        private const int SinhSingleIterations = 10000;
        private const int SqrtSingleIterations = 80000;
        private const int TanSingleIterations = 25000;
        private const int TanhSingleIterations = 20000;
    }
}
