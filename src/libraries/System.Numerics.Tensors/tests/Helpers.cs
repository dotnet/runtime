// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Numerics.Tensors;
using Xunit;

namespace System.Numerics.Tensors.Tests
{
    public static class Helpers
    {
        public static IEnumerable<int> TensorLengthsIncluding0 => Enumerable.Range(0, 257);

        public static IEnumerable<int> TensorLengths => Enumerable.Range(1, 256);
        public static IEnumerable<nint[]> TensorShapes => [[1], [2], [10], [1,1], [1,2], [2,2], [5, 5], [2, 2, 2], [5, 5, 5], [3, 3, 3, 3], [4, 4, 4, 4, 4], [1, 2, 3, 4, 5, 6, 7]];

        // Tolerances taken from testing in the scalar math routines:
        // cf. https://github.com/dotnet/runtime/blob/89f7ad3b276fb0b48f20cb4e8408bdce85c2b415/src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System/Math.cs
        // and https://github.com/dotnet/runtime/blob/fd48b6f5d1ff81a81d09e9d72982cc9e8d139852/src/libraries/System.Runtime/tests/System.Runtime.Tests/System/HalfTests.cs
        public const double DefaultDoubleTolerance = 8.8817841970012523e-16;
        public const float DefaultFloatTolerance = 4.76837158e-07f;
        public const float DefaultHalfTolerance = 3.90625e-03f;
        public const double DefaultToleranceForEstimates = 1.171875e-02;

#if NET
        private static class DefaultTolerance<T> where T : unmanaged, INumber<T>
        {
            public static readonly T Value = DetermineTolerance<T>(DefaultDoubleTolerance, DefaultFloatTolerance, Half.CreateTruncating(DefaultHalfTolerance)) ?? T.CreateTruncating(0);
        }

        public static bool IsEqualWithTolerance<T>(T expected, T actual, T? tolerance = null) where T : unmanaged, INumber<T>
        {
            if (T.IsNaN(expected) != T.IsNaN(actual))
            {
                return false;
            }

            tolerance = tolerance ?? DefaultTolerance<T>.Value;
            T diff = T.Abs(expected - actual);
            return !(diff > tolerance && diff > T.Max(T.Abs(expected), T.Abs(actual)) * tolerance);
        }
#else
        public static bool IsEqualWithTolerance(float expected, float actual, float? tolerance = null)
        {
            if (float.IsNaN(expected) != float.IsNaN(actual))
            {
                return false;
            }

            tolerance ??= DefaultFloatTolerance;
            float diff = MathF.Abs(expected - actual);
            return !(diff > tolerance && diff > MathF.Max(MathF.Abs(expected), MathF.Abs(actual)) * tolerance);
        }
#endif

        public static T? DetermineTolerance<T>(
            double? doubleTolerance = null,
            float? floatTolerance = null
#if NET
            , Half? halfTolerance = null
#endif
            ) where T : struct
        {
            if (typeof(T) == typeof(double) && doubleTolerance != null)
            {
                return (T?)(object)doubleTolerance;
            }
            else if (typeof(T) == typeof(float) && floatTolerance != null)
            {
                return (T?)(object)floatTolerance;
            }
#if NET
            else if (typeof(T) == typeof(Half) && halfTolerance != null)
            {
                return (T?)(object)halfTolerance;
            }
            else if (typeof(T) == typeof(NFloat))
            {
                if (IntPtr.Size == 8 && doubleTolerance != null)
                {
                    return (T?)(object)(NFloat)doubleTolerance;
                }
                else if (IntPtr.Size == 4 && floatTolerance != null)
                {
                    return (T?)(object)(NFloat)doubleTolerance;
                }
            }
#endif
            return null;
        }

#if NETCOREAPP
        public delegate void AssertThrowsAction<T>(TensorSpan<T> span);

        // Cannot use standard Assert.Throws() when testing Span - Span and closures don't get along.
        public static void AssertThrows<E, T>(TensorSpan<T> span, AssertThrowsAction<T> action) where E : Exception
        {
            try
            {
                action(span);
                Assert.Fail($"Expected exception: {typeof(E)}");
            }
            catch (Exception ex)
            {
                Assert.True(ex is E, $"Wrong exception thrown. Expected: {typeof(E)} Actual: {ex.GetType()}");
            }
        }

        public static void AdjustIndices(int curIndex, nint addend, ref nint[] curIndices, ReadOnlySpan<nint> lengths)
        {
            if (addend == 0 || curIndex < 0)
                return;
            curIndices[curIndex] += addend;
            AdjustIndices(curIndex - 1, curIndices[curIndex] / lengths[curIndex], ref curIndices, lengths);
            curIndices[curIndex] = curIndices[curIndex] % lengths[curIndex];
        }

#endif
    }
}
