// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Sdk;

namespace System.Numerics.Tensors.Tests
{
    public static class Helpers
    {
        public static int SizeGreaterThanByte => 260;
        public static int SizeGreaterThanInt16 => 65540;

        public static IEnumerable<int> TensorLengthsIncluding0 => Enumerable.Range(0, 257);

        public static IEnumerable<int> TensorLengths => Enumerable.Range(1, 256);
        public static IEnumerable<nint[]> TensorShapes => [[1], [2], [10], [1, 1], [1, 2], [2, 2], [5, 5], [2, 2, 2], [5, 5, 5], [3, 3, 3, 3], [4, 4, 4, 4, 4], [1, 2, 3, 4, 5, 6, 7, 1, 2]];
        public static nint[][] TensorSliceShapes => [[1], [1], [5], [1, 1], [1, 1], [1, 2], [3, 3], [2, 2, 1], [5, 3, 5], [3, 2, 1, 3], [4, 3, 2, 1, 2], [1, 2, 2, 2, 2, 1, 1, 1, 1]];
        public static nint[][] TensorSliceShapesForBroadcast => [[1], [1], [1], [1, 1], [1, 1], [1, 2], [1, 1], [2, 2, 1], [1, 5, 5], [3, 1, 1, 3], [4, 1, 4, 1, 4], [1, 2, 1, 4, 1, 1, 7, 1, 1]];

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

        public static void AssertEqualWithTolerance<T>(T expected, T actual, T? tolerance = null, string? banner = null) where T : unmanaged, INumber<T>
        {
            T actualTolerance = tolerance ?? DefaultTolerance<T>.Value;
            try
            {
                T scaledTolerance = checked(T.Max(T.Abs(expected), T.Abs(actual)) * actualTolerance);
                if (T.IsFinite(scaledTolerance))
                {
                    actualTolerance = T.Max(scaledTolerance, actualTolerance);
                }
            }
            catch (OverflowException) { } // Multiplication and T.Abs can throw for integers, just keep the original tolerance in that case.

            // Delegate to AssertExtensions.Equal for special value comparisons (NaN, +-inf, +-0)
            if (typeof(T) == typeof(double))
            {
                AssertExtensions.Equal((double)(object)expected, (double)(object)actual, (double)(object)actualTolerance, banner);
            }
            else if (typeof(T) == typeof(float))
            {
                AssertExtensions.Equal((float)(object)expected, (float)(object)actual, (float)(object)actualTolerance, banner);
            }
            else if (typeof(T) == typeof(Half))
            {
                AssertExtensions.Equal((Half)(object)expected, (Half)(object)actual, (Half)(object)actualTolerance, banner);
            }
            else if (typeof(T) == typeof(NFloat))
            {
                AssertExtensions.Equal((NFloat)(object)expected, (NFloat)(object)actual, (NFloat)(object)actualTolerance, banner);
            }
            else if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(byte) ||
                typeof(T) == typeof(short) || typeof(T) == typeof(ushort) || typeof(T) == typeof(char) ||
                typeof(T) == typeof(int) || typeof(T) == typeof(uint) ||
                typeof(T) == typeof(long) || typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(nint) || typeof(T) == typeof(nuint) ||
                typeof(T) == typeof(Int128) || typeof(T) == typeof(UInt128))
            {
                T delta;
                try
                {
                    delta = T.Abs(checked(expected - actual));
                }
                catch (OverflowException)
                {
                    // Subtraction and T.Abs can throw for integers, in that case the mismatch is large enough to fail assertion
                    throw EqualException.ForMismatchedValues(expected.ToString(), actual.ToString(), banner);
                }
                if (delta > actualTolerance)
                {
                    throw EqualException.ForMismatchedValues(expected.ToString(), actual.ToString(), banner);
                }
            }
            else
            {
                throw new NotImplementedException($"Type not supported for {nameof(AssertEqualWithTolerance)}: {typeof(T).Name}");
            }
        }
#else
        public static void AssertEqualWithTolerance(float expected, float actual, float? tolerance = null, string? banner = null)
        {
            float actualTolerance = tolerance ?? DefaultFloatTolerance;
            float scaledTolerance = MathF.Max(MathF.Abs(expected), MathF.Abs(actual)) * (tolerance ?? DefaultFloatTolerance);
            if (!float.IsNaN(scaledTolerance) && !float.IsInfinity(scaledTolerance))
            {
                actualTolerance = MathF.Max(actualTolerance, scaledTolerance);
            }

            AssertExtensions.Equal(expected, actual, actualTolerance, banner);
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
                if (NFloat.Size == 8 && doubleTolerance != null)
                {
                    return (T?)(object)(NFloat)doubleTolerance;
                }
                else if (NFloat.Size == 4 && floatTolerance != null)
                {
                    return (T?)(object)(NFloat)floatTolerance;
                }
            }
#endif
            return null;
        }

#if NET
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
