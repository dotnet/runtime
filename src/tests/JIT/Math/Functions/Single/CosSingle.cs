// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Cos(float) over 5000 iterations for the domain 0, PI

        private const float cosDelta = 0.000628318531f;
        private const float cosExpectedResult = -0.993487537f;

        public void Cos() => CosTest();

        public static void CosTest()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += cosDelta;
                result += MathF.Cos(value);
            }

            float diff = MathF.Abs(cosExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {cosExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
