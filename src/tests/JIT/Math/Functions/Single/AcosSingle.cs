// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests MathF.Acos(float) over 5000 iterations for the domain -1, +1

        private const float acosDelta = 0.0004f;
        private const float acosExpectedResult = 7852.41084f;

        public void Acos() => AcosTest();

        public static void AcosTest()
        {
            float result = 0.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += acosDelta;
                result += MathF.Acos(value);
            }

            float diff = MathF.Abs(acosExpectedResult - result);

            if (diff > MathTests.SingleEpsilon)
            {
                throw new Exception($"Expected Result {acosExpectedResult,10:g9}; Actual Result {result,10:g9}");
            }
        }
    }
}
