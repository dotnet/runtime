// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Numerics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static double Average(this IEnumerable<int> source)
        {
            if (source.TryGetSpan(out ReadOnlySpan<int> span))
            {
                // Int32 is special-cased separately from the rest of the types as it can be vectorized:
                // with at most Int32.MaxValue values, and with each being at most Int32.MaxValue, we can't
                // overflow a long accumulator, and order of operations doesn't matter.

                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = 0;
                int i = 0;

                if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
                {
                    Vector<long> sums = default;
                    do
                    {
                        Vector.Widen(new Vector<int>(span.Slice(i)), out Vector<long> low, out Vector<long> high);
                        sums += low;
                        sums += high;
                        i += Vector<int>.Count;
                    }
                    while (i <= span.Length - Vector<int>.Count);
                    sum += Vector.Sum(sums);
                }

                for (; (uint)i < (uint)span.Length; i++)
                {
                    sum += span[i];
                }

                return (double)sum / span.Length;
            }

            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = e.Current;
                long count = 1;

                while (e.MoveNext())
                {
                    checked { sum += e.Current; }
                    count++;
                }

                return (double)sum / count;
            }
        }

        public static double Average(this IEnumerable<long> source) => Average<long, long, double>(source);

        public static float Average(this IEnumerable<float> source) => (float)Average<float, double, double>(source);

        public static double Average(this IEnumerable<double> source) => Average<double, double, double>(source);

        public static decimal Average(this IEnumerable<decimal> source) => Average<decimal, decimal, decimal>(source);

        private static TResult Average<TSource, TAccumulator, TResult>(this IEnumerable<TSource> source)
            where TSource : struct, INumber<TSource>
            where TAccumulator : struct, INumber<TAccumulator>
            where TResult : struct, INumber<TResult>
        {
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                return TResult.CreateChecked(Sum<TSource, TAccumulator>(span)) / TResult.CreateChecked(span.Length);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                TAccumulator sum = TAccumulator.CreateChecked(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    checked { sum += TAccumulator.CreateChecked(e.Current); }
                    count++;
                }

                return TResult.CreateChecked(sum) / TResult.CreateChecked(count);
            }
        }


        public static double? Average(this IEnumerable<int?> source) => Average<int, long, double>(source);

        public static double? Average(this IEnumerable<long?> source) => Average<long, long, double>(source);

        public static float? Average(this IEnumerable<float?> source) => Average<float, double, double>(source) is double result ? (float)result : null;

        public static double? Average(this IEnumerable<double?> source) => Average<double, double, double>(source);

        public static decimal? Average(this IEnumerable<decimal?> source) => Average<decimal, decimal, decimal>(source);

        private static TResult? Average<TSource, TAccumulator, TResult>(this IEnumerable<TSource?> source)
            where TSource : struct, INumber<TSource>
            where TAccumulator : struct, INumber<TAccumulator>
            where TResult : struct, INumber<TResult>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<TSource?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSource? value = e.Current;
                    if (value.HasValue)
                    {
                        TAccumulator sum = TAccumulator.CreateChecked(value.GetValueOrDefault());
                        long count = 1;

                        while (e.MoveNext())
                        {
                            value = e.Current;
                            if (value.HasValue)
                            {
                                checked { sum += TAccumulator.CreateChecked(value.GetValueOrDefault()); }
                                count++;
                            }
                        }

                        return TResult.CreateChecked(sum) / TResult.CreateChecked(count);
                    }
                }
            }

            return null;
        }


        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector) => Average<TSource, int, long, double>(source, selector);

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector) => Average<TSource, long, long, double>(source, selector);

        public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector) => (float)Average<TSource, float, double, double>(source, selector);

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector) => Average<TSource, double, double, double>(source, selector);

        public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector) => Average<TSource, decimal, decimal, decimal>(source, selector);

        private static TResult Average<TSource, TSelector, TAccumulator, TResult>(this IEnumerable<TSource> source, Func<TSource, TSelector> selector)
            where TSelector : struct, INumber<TSelector>
            where TAccumulator : struct, INumber<TAccumulator>
            where TResult : struct, INumber<TResult>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                TAccumulator sum = TAccumulator.CreateChecked(selector(e.Current));
                long count = 1;

                while (e.MoveNext())
                {
                    checked { sum += TAccumulator.CreateChecked(selector(e.Current)); }
                    count++;
                }

                return TResult.CreateChecked(sum) / TResult.CreateChecked(count);
            }
        }


        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector) => Average<TSource, int, long, double>(source, selector);

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector) => Average<TSource, long, long, double>(source, selector);

        public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector) => Average<TSource, float, double, double>(source, selector) is double result ? (float)result : null;

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector) => Average<TSource, double, double, double>(source, selector);

        public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector) => Average<TSource, decimal, decimal, decimal>(source, selector);

        private static TResult? Average<TSource, TSelector, TAccumulator, TResult>(this IEnumerable<TSource> source, Func<TSource, TSelector?> selector)
            where TSelector : struct, INumber<TSelector>
            where TAccumulator : struct, INumber<TAccumulator>
            where TResult : struct, INumber<TResult>
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    TSelector? value = selector(e.Current);
                    if (value.HasValue)
                    {
                        TAccumulator sum = TAccumulator.CreateChecked(value.GetValueOrDefault());
                        long count = 1;

                        while (e.MoveNext())
                        {
                            value = selector(e.Current);
                            if (value.HasValue)
                            {
                                checked { sum += TAccumulator.CreateChecked(value.GetValueOrDefault()); }
                                count++;
                            }
                        }

                        return TResult.CreateChecked(sum) / TResult.CreateChecked(count);
                    }
                }
            }

            return null;
        }
    }
}
