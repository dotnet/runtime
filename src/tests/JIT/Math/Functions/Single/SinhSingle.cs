// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Sinh(float) over 5000 iterations for the domain -1, +1

        private const float sinhDelta = 0.0004f;
        private const float sinhExpectedResult = 1.26028216f;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        public void Sinh() => SinhTest();

        public static void SinhTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += sinhDelta;
                result += MathF.Sinh(value);
            }

            float diff = MathF.Abs(sinhExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {sinhExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
