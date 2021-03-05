// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.MathBenchmarks
{
    public partial class Single
    {
        // Tests Math.CopySign(float) over 5000 iterations for the domain -1, +1

        private const float copySignDelta = 0.0004f;
        private const int copySignExpectedResult = 0;

        public void CopySign() => CopySignTest();

        public static void CopySignTest()
        {
            float result = 1.0f, value = -1.0f;

            for (int iteration = 0; iteration < MathTests.Iterations; iteration++)
            {
                value += copySignDelta;
                result += MathF.CopySign(result, value);
            }

            if (result != copySignExpectedResult)
            {
                throw new Exception($"Expected Result {copySignExpectedResult}; Actual Result {result}");
            }
        }
    }
}
