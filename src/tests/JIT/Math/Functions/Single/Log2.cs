// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Log2(float) over 5000 iterations for the domain +1, +3

        private const float log2Delta = 0.0004f;
        private const float log2ExpectedResult = 4672.73193f;

        public void Log2() => Log2Test();

        public static void Log2Test()
        {
            float result = 0.0f, value = 1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                result += MathF.Log2(value);
                value += log2Delta;
            }

            float diff = MathF.Abs(log2ExpectedResult - result);

            if (float.IsNaN(result) || (diff > MathTests.SingleEpsilon))
            {
                throw new Exception($"Expected Result {log2ExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
