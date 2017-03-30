// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Numerics.Tests
{
    public static class VectorTests
    {
        public const int DefaultInnerIterationsCount = 100000000;

        #region Single Helper Values
        // float has a machine epsilon of approx: 1.19e-07. However, due to floating-point precision
        // errors, this is too accurate when aggregating values of a set of iterations. Using the
        // half-precision machine epsilon as our epsilon should be 'good enough' for the purposes
        // of the perf testing as it ensures we get the expected value and that it is at least as precise
        // as we would have computed with the half-precision version of the function (without aggregation).
        public const float SingleEpsilon = 9.77e-04f;

        public const float SinglePositiveDelta = 1.0f / DefaultInnerIterationsCount;

        public const float SingleNegativeDelta = -1.0f / DefaultInnerIterationsCount;
        #endregion

        #region Vector2 Helper Values
        public static readonly Vector2 Vector2Delta = new Vector2(SinglePositiveDelta, SingleNegativeDelta);

        public static readonly Vector2 Vector2Value = new Vector2(-1.0f, 1.0f);

        public static readonly Vector2 Vector2ValueInverted = new Vector2(1.0f, -1.0f);
        #endregion

        #region Vector3 Helper Values
        public static readonly Vector3 Vector3Delta = new Vector3(SinglePositiveDelta, SingleNegativeDelta, SinglePositiveDelta);

        public static readonly Vector3 Vector3Value = new Vector3(-1.0f, 1.0f, -1.0f);

        public static readonly Vector3 Vector3ValueInverted = new Vector3(1.0f, -1.0f, 1.0f);
        #endregion

        #region Vector4 Helper Values
        public static readonly Vector4 Vector4Delta = new Vector4(SinglePositiveDelta, SingleNegativeDelta, SinglePositiveDelta, SingleNegativeDelta);

        public static readonly Vector4 Vector4Value = new Vector4(-1.0f, 1.0f, -1.0f, 1.0f);

        public static readonly Vector4 Vector4ValueInverted = new Vector4(1.0f, -1.0f, 1.0f, -1.0f);
        #endregion

        #region Assert Helpers
        public static void AssertEqual(float expectedResult, float actualResult)
        {
            if (!SingleAreEqual(expectedResult, actualResult))
            {
                throw new Exception($"Expected Result: {expectedResult:g9}; Actual Result: {actualResult:g9}");
            }
        }

        public static void AssertEqual(int expectedResult, int actualResult)
        {
            if (expectedResult != actualResult)
            {
                throw new Exception($"Expected Result: {expectedResult}; Actual Result: {actualResult}");
            }
        }

        public static void AssertEqual(Vector2 expectedResult, Vector2 actualResult)
        {
            if (!SingleAreEqual(expectedResult.X, actualResult.X) ||
                !SingleAreEqual(expectedResult.Y, actualResult.Y))
            {
                throw new Exception($"Expected Result: {expectedResult:g9}; Actual Result: {actualResult:g9}");
            }
        }

        public static void AssertEqual(Vector3 expectedResult, Vector3 actualResult)
        {
            if (!SingleAreEqual(expectedResult.X, actualResult.X) ||
                !SingleAreEqual(expectedResult.Y, actualResult.Y) ||
                !SingleAreEqual(expectedResult.Z, actualResult.Z))
            {
                throw new Exception($"Expected Result: {expectedResult:g9}; Actual Result: {actualResult:g9}");
            }
        }

        public static void AssertEqual(Vector4 expectedResult, Vector4 actualResult)
        {
            if (!SingleAreEqual(expectedResult.X, actualResult.X) ||
                !SingleAreEqual(expectedResult.Y, actualResult.Y) ||
                !SingleAreEqual(expectedResult.Z, actualResult.Z) ||
                !SingleAreEqual(expectedResult.W, actualResult.W))
            {
                throw new Exception($"Expected Result: {expectedResult:g9}; Actual Result: {actualResult:g9}");
            }
        }

        public static bool SingleAreEqual(float expectedResult, float actualResult)
        {
            if (float.IsNaN(expectedResult))
            {
                return float.IsNaN(actualResult);
            }
            else if (float.IsNaN(actualResult))
            {
                // expectedResult is finite
                return false;
            }
            else
            {
                var diff = Math.Abs(expectedResult - actualResult);
                return (diff <= SingleEpsilon);
            }
        }
        #endregion
    }
}
