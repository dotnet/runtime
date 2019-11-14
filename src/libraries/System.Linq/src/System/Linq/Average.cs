// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static double Average(this IEnumerable<int> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        public static double? Average(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    int? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        public static double Average(this IEnumerable<long> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = e.Current;
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += e.Current;
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        public static double? Average(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    long? v = e.Current;
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        public static float Average(this IEnumerable<float> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }

                return (float)(sum / count);
            }
        }

        public static float? Average(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    float? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (float)(sum / count);
                    }
                }
            }

            return null;
        }

        public static double Average(this IEnumerable<double> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    // There is an opportunity to short-circuit here, in that if e.Current is
                    // ever NaN then the result will always be NaN. Assuming that this case is
                    // rare enough that not checking is the better approach generally.
                    sum += e.Current;
                    ++count;
                }

                return sum / count;
            }
        }

        public static double? Average(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    double? v = e.Current;
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = e.Current;
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        public static decimal Average(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                decimal sum = e.Current;
                long count = 1;
                while (e.MoveNext())
                {
                    sum += e.Current;
                    ++count;
                }

                return sum / count;
            }
        }

        public static decimal? Average(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    decimal? v = e.Current;
                    if (v.HasValue)
                    {
                        decimal sum = v.GetValueOrDefault();
                        long count = 1;
                        while (e.MoveNext())
                        {
                            v = e.Current;
                            if (v.HasValue)
                            {
                                sum += v.GetValueOrDefault();
                                ++count;
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = selector(e.Current);
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += selector(e.Current);
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    int? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                long sum = selector(e.Current);
                long count = 1;
                checked
                {
                    while (e.MoveNext())
                    {
                        sum += selector(e.Current);
                        ++count;
                    }
                }

                return (double)sum / count;
            }
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    long? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        long sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (double)sum / count;
                    }
                }
            }

            return null;
        }

        public static float Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    sum += selector(e.Current);
                    ++count;
                }

                return (float)(sum / count);
            }
        }

        public static float? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    float? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return (float)(sum / count);
                    }
                }
            }

            return null;
        }

        public static double Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                double sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    // There is an opportunity to short-circuit here, in that if e.Current is
                    // ever NaN then the result will always be NaN. Assuming that this case is
                    // rare enough that not checking is the better approach generally.
                    sum += selector(e.Current);
                    ++count;
                }

                return sum / count;
            }
        }

        public static double? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    double? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        double sum = v.GetValueOrDefault();
                        long count = 1;
                        checked
                        {
                            while (e.MoveNext())
                            {
                                v = selector(e.Current);
                                if (v.HasValue)
                                {
                                    sum += v.GetValueOrDefault();
                                    ++count;
                                }
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }

        public static decimal Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                decimal sum = selector(e.Current);
                long count = 1;
                while (e.MoveNext())
                {
                    sum += selector(e.Current);
                    ++count;
                }

                return sum / count;
            }
        }

        public static decimal? Average<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    decimal? v = selector(e.Current);
                    if (v.HasValue)
                    {
                        decimal sum = v.GetValueOrDefault();
                        long count = 1;
                        while (e.MoveNext())
                        {
                            v = selector(e.Current);
                            if (v.HasValue)
                            {
                                sum += v.GetValueOrDefault();
                                ++count;
                            }
                        }

                        return sum / count;
                    }
                }
            }

            return null;
        }
    }
}
