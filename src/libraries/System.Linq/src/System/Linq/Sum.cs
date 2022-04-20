// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static int Sum(this IEnumerable<int> source) => Sum<int, int>(source);

        public static long Sum(this IEnumerable<long> source) => Sum<long, long>(source);

        public static float Sum(this IEnumerable<float> source) => (float)Sum<float, double>(source);

        public static double Sum(this IEnumerable<double> source) => Sum<double, double>(source);

        public static decimal Sum(this IEnumerable<decimal> source) => Sum<decimal, decimal>(source);

        private static TResult Sum<TSource, TResult>(this IEnumerable<TSource> source)
            where TSource : struct, INumber<TSource>
            where TResult : struct, INumber<TResult>
        {
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                return Sum<TSource, TResult>(span);
            }

            TResult sum = TResult.Zero;
            foreach (TSource value in source)
            {
                checked { sum += TResult.CreateChecked(value); }
            }

            return sum;
        }

        private static TResult Sum<T, TResult>(ReadOnlySpan<T> span)
            where T : struct, INumber<T>
            where TResult : struct, INumber<TResult>
        {
            TResult sum = TResult.Zero;
            foreach (T value in span)
            {
                checked { sum += TResult.CreateChecked(value); }
            }

            return sum;
        }


        public static int? Sum(this IEnumerable<int?> source) => Sum<int, int>(source);

        public static long? Sum(this IEnumerable<long?> source) => Sum<long, long>(source);

        public static float? Sum(this IEnumerable<float?> source) => Sum<float, double>(source);

        public static double? Sum(this IEnumerable<double?> source) => Sum<double, double>(source);

        public static decimal? Sum(this IEnumerable<decimal?> source) => Sum<decimal, decimal>(source);

        private static TSource? Sum<TSource, TAccumulator>(this IEnumerable<TSource?> source)
            where TSource : struct, INumber<TSource>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource? value in source)
            {
                if (value is not null)
                {
                    checked { sum += TAccumulator.CreateChecked(value.GetValueOrDefault()); }
                }
            }

            return TSource.CreateTruncating(sum);
        }


        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => Sum<TSource, int, int>(source, selector);

        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => Sum<TSource, long, long>(source, selector);

        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) => Sum<TSource, float, double>(source, selector);

        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) => Sum<TSource, double, double>(source, selector);

        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector) => Sum<TSource, decimal, decimal>(source, selector);

        private static TResult Sum<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
            where TResult : struct, INumber<TResult>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource value in source)
            {
                checked { sum += TAccumulator.CreateChecked(selector(value)); }
            }

            return TResult.CreateTruncating(sum);
        }


        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => Sum<TSource, int, int>(source, selector);

        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => Sum<TSource, long, long>(source, selector);

        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) => Sum<TSource, float, double>(source, selector);

        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) => Sum<TSource, double, double>(source, selector);

        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector) => Sum<TSource, decimal, decimal>(source, selector);

        private static TResult? Sum<TSource, TResult, TAccumulator>(this IEnumerable<TSource> source, Func<TSource, TResult?> selector)
            where TResult : struct, INumber<TResult>
            where TAccumulator : struct, INumber<TAccumulator>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            TAccumulator sum = TAccumulator.Zero;
            foreach (TSource item in source)
            {
                if (selector(item) is TResult selectedValue)
                {
                    checked { sum += TAccumulator.CreateChecked(selectedValue); }
                }
            }

            return TResult.CreateTruncating(sum);
        }
    }
}
