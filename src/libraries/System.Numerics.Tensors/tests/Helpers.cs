// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;

namespace System.Numerics.Tensors.Tests
{
    internal static class Helpers
    {
        public static IEnumerable<int> TensorLengthsIncluding0 => Enumerable.Range(0, 257);

        public static IEnumerable<int> TensorLengths => Enumerable.Range(1, 256);

        // Tolerances taken from testing in the scalar math routines:
        // cf. https://github.com/dotnet/runtime/blob/89f7ad3b276fb0b48f20cb4e8408bdce85c2b415/src/libraries/System.Runtime/tests/System.Runtime.Extensions.Tests/System/Math.cs
        // and https://github.com/dotnet/runtime/blob/fd48b6f5d1ff81a81d09e9d72982cc9e8d139852/src/libraries/System.Runtime/tests/System.Runtime.Tests/System/HalfTests.cs
        public const double DefaultDoubleTolerance = 8.8817841970012523e-16;
        public const float DefaultFloatTolerance = 4.76837158e-07f;
        public const float DefaultHalfTolerance = 3.90625e-03f;
        public const double DefaultToleranceForEstimates = 1.171875e-02;

#if NETCOREAPP
        private static class DefaultTolerance<T> where T : unmanaged, INumber<T>
        {
            public static readonly T Value = DetermineTolerance<T>(DefaultDoubleTolerance, DefaultFloatTolerance, Half.CreateTruncating(DefaultHalfTolerance)) ?? T.CreateTruncating(0);
        }

        public static bool IsEqualWithTolerance<T>(T expected, T actual, T? tolerance = null) where T : unmanaged, INumber<T>
        {
            tolerance = tolerance ?? DefaultTolerance<T>.Value;
            T diff = T.Abs(expected - actual);
            return !(diff > tolerance && diff > T.Max(T.Abs(expected), T.Abs(actual)) * tolerance);
        }
#else
        public static bool IsEqualWithTolerance(float expected, float actual, float? tolerance = null)
        {
            tolerance ??= DefaultFloatTolerance;
            float diff = MathF.Abs(expected - actual);
            return !(diff > tolerance && diff > MathF.Max(MathF.Abs(expected), MathF.Abs(actual)) * tolerance);
        }
#endif

        public static T? DetermineTolerance<T>(
            double? doubleTolerance = null,
            float? floatTolerance = null
#if NETCOREAPP
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
#if NETCOREAPP
            else if (typeof(T) == typeof(Half) && halfTolerance != null)
            {
                return (T?)(object)halfTolerance;
            }
#endif

            return null;
        }
    }
}
