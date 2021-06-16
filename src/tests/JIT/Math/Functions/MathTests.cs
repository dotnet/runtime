// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.MathBenchmarks
{
    public partial class MathTests
    {
        // double has a machine epsilon of approx: 2.22e-16. However, due to floating-point precision
        // errors, this is too accurate when aggregating values of a set of iterations. Using the
        // single-precision machine epsilon as our epsilon should be 'good enough' for the purposes
        // of the perf testing as it ensures we get the expected value and that it is at least as precise
        // as we would have computed with the single-precision version of the function (without aggregation).
        public const double DoubleEpsilon = 1.19e-07;

        // 5000 iterations is enough to cover the full domain of inputs for certain functions (such
        // as Cos, which has a domain of 0 to PI) at reasonable intervals (in the case of Cos, the
        // interval is PI / 5000 which is 0.0006283185307180). It should also give reasonable coverage
        // for functions which have a larger domain (such as Atan, which covers the full set of real numbers).
        public const int Iterations = 5000;

        // float has a machine epsilon of approx: 1.19e-07. However, due to floating-point precision
        // errors, this is too accurate when aggregating values of a set of iterations. Using the
        // half-precision machine epsilon as our epsilon should be 'good enough' for the purposes
        // of the perf testing as it ensures we get the expected value and that it is at least as precise
        // as we would have computed with the half-precision version of the function (without aggregation).
        public const float SingleEpsilon = 9.77e-04f;
    }
}
