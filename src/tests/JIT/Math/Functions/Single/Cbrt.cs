// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Cbrt(float) over 5000 iterations for the domain +0, +PI

        private const float cbrtDelta = 0.000628318531f;
        private const float cbrtExpectedResult = 5491.4541f;

        public void Cbrt() => CbrtTest();

        public static void CbrtTest()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.Cbrt(value);
                value += cbrtDelta;
            }

            float diff = MathF.Abs(cbrtExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {cbrtExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
