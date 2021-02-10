// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Log10(float) over 5000 iterations for the domain -1, +1

        private const float log10Delta = 0.0004f;
        private const float log10ExpectedResult = -664.094971f;

        /// <summary>
        /// this benchmark is dependent on loop alignment
        /// </summary>
        public void Log10() => Log10Test();

        public static void Log10Test()
        {
            float result = 0.0f, value = 0.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += log10Delta;
                result += MathF.Log10(value);
            }

            float diff = MathF.Abs(log10ExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {log10ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
