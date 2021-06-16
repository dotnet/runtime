// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Asin(float) over 5000 iterations for the domain -1, +1

        private const float asinDelta = 0.0004f;
        private const float asinExpectedResult = 1.57079590f;

        public void Asin() => AsinTest();

        public static void AsinTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += asinDelta;
                result += MathF.Asin(value);
            }

            float diff = MathF.Abs(asinExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {asinExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
