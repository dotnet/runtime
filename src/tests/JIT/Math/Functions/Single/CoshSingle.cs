// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Cosh(float) over 5000 iterations for the domain -1, +1

        private const float coshDelta = 0.0004f;
        private const float coshExpectedResult = 5876.02588f;

        public void Cosh() => CoshTest();

        public static void CoshTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += coshDelta;
                result += MathF.Cosh(value);
            }

            float diff = MathF.Abs(coshExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {coshExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
