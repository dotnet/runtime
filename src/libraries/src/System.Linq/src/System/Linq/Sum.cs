// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static int Sum(this IEnumerable<int> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int sum = 0;
            checked
            {
                foreach (int v in source)
                {
                    sum += v;
                }
            }

            return sum;
        }

        public static int? Sum(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            int sum = 0;
            checked
            {
                foreach (int? v in source)
                {
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        public static long Sum(this IEnumerable<long> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (long v in source)
                {
                    sum += v;
                }
            }

            return sum;
        }

        public static long? Sum(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (long? v in source)
                {
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        public static float Sum(this IEnumerable<float> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (float v in source)
            {
                sum += v;
            }

            return (float)sum;
        }

        public static float? Sum(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (float? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return (float)sum;
        }

        public static double Sum(this IEnumerable<double> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (double v in source)
            {
                sum += v;
            }

            return sum;
        }

        public static double? Sum(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            double sum = 0;
            foreach (double? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        public static decimal Sum(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal sum = 0;
            foreach (decimal v in source)
            {
                sum += v;
            }

            return sum;
        }

        public static decimal? Sum(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            decimal sum = 0;
            foreach (decimal? v in source)
            {
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        public static int Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    sum += selector(item);
                }
            }

            return sum;
        }

        public static int? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            int sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    int? v = selector(item);
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        public static long Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            long sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    sum += selector(item);
                }
            }

            return sum;
        }

        public static long? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            long sum = 0;
            checked
            {
                foreach (TSource item in source)
                {
                    long? v = selector(item);
                    if (v != null)
                    {
                        sum += v.GetValueOrDefault();
                    }
                }
            }

            return sum;
        }

        public static float Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return (float)sum;
        }

        public static float? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                float? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return (float)sum;
        }

        public static double Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return sum;
        }

        public static double? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            double sum = 0;
            foreach (TSource item in source)
            {
                double? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }

        public static decimal Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal sum = 0;
            foreach (TSource item in source)
            {
                sum += selector(item);
            }

            return sum;
        }

        public static decimal? Sum<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (selector == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.selector);
            }

            decimal sum = 0;
            foreach (TSource item in source)
            {
                decimal? v = selector(item);
                if (v != null)
                {
                    sum += v.GetValueOrDefault();
                }
            }

            return sum;
        }
    }
}
