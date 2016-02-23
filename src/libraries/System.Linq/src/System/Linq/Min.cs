// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static int Min(this IEnumerable<int> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            int value;
            using (IEnumerator<int> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    int x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static int? Min(this IEnumerable<int?> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            int? value = null;
            using (IEnumerator<int?> e = source.GetEnumerator())
            {
                // Start off knowing that we've a non-null value (or exit here, knowing we don't)
                // so we don't have to keep testing for nullity.
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                // Keep hold of the wrapped value, and do comparisons on that, rather than
                // using the lifted operation each time.
                int valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    int? cur = e.Current;
                    int x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static long Min(this IEnumerable<long> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            long value;
            using (IEnumerator<long> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    long x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static long? Min(this IEnumerable<long?> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            long? value = null;
            using (IEnumerator<long?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                long valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    long? cur = e.Current;
                    long x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static float Min(this IEnumerable<float> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            float value;
            using (IEnumerator<float> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    float x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity.
                    // Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (float.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        public static float? Min(this IEnumerable<float?> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            float? value = null;
            using (IEnumerator<float?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                float valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    float? cur = e.Current;
                    if (cur.HasValue)
                    {
                        float x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (float.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static double Min(this IEnumerable<double> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            double value;
            using (IEnumerator<double> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    double x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }
                    else if (double.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        public static double? Min(this IEnumerable<double?> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            double? value = null;
            using (IEnumerator<double?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                double valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    double? cur = e.Current;
                    if (cur.HasValue)
                    {
                        double x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (double.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static decimal Min(this IEnumerable<decimal> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            decimal value;
            using (IEnumerator<decimal> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = e.Current;
                while (e.MoveNext())
                {
                    decimal x = e.Current;
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static decimal? Min(this IEnumerable<decimal?> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            decimal? value = null;
            using (IEnumerator<decimal?> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = e.Current;
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = e.Current;
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TSource Min<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            Comparer<TSource> comparer = Comparer<TSource>.Default;
            TSource value = default(TSource);
            if (value == null)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = e.Current;
                    }
                    while (value == null);

                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (x != null && comparer.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        throw Error.NoElements();
                    }

                    value = e.Current;
                    while (e.MoveNext())
                    {
                        TSource x = e.Current;
                        if (comparer.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
            }

            return value;
        }

        public static int Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            int value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    int x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static int? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, int?> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            int? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                // Start off knowing that we've a non-null value (or exit here, knowing we don't)
                // so we don't have to keep testing for nullity.
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                // Keep hold of the wrapped value, and do comparisons on that, rather than
                // using the lifted operation each time.
                int valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    int? cur = selector(e.Current);
                    int x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static long Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            long value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    long x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static long? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, long?> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            long? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                long valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    long? cur = selector(e.Current);
                    long x = cur.GetValueOrDefault();

                    // Do not replace & with &&. The branch prediction cost outweighs the extra operation
                    // unless nulls either never happen or always happen.
                    if (cur.HasValue & x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static float Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            float value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    float x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }

                    // Normally NaN < anything is false, as is anything < NaN
                    // However, this leads to some irksome outcomes in Min and Max.
                    // If we use those semantics then Min(NaN, 5.0) is NaN, but
                    // Min(5.0, NaN) is 5.0!  To fix this, we impose a total
                    // ordering where NaN is smaller than every value, including
                    // negative infinity.
                    // Not testing for NaN therefore isn't an option, but since we
                    // can't find a smaller value, we can short-circuit.
                    else if (float.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        public static float? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, float?> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            float? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                float valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    float? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        float x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (float.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static double Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            double value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    double x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                    else if (double.IsNaN(x))
                    {
                        return x;
                    }
                }
            }

            return value;
        }

        public static double? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, double?> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            double? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                double valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    double? cur = selector(e.Current);
                    if (cur.HasValue)
                    {
                        double x = cur.GetValueOrDefault();
                        if (x < valueVal)
                        {
                            valueVal = x;
                            value = cur;
                        }
                        else if (double.IsNaN(x))
                        {
                            return cur;
                        }
                    }
                }
            }

            return value;
        }

        public static decimal Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            decimal value;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                if (!e.MoveNext())
                {
                    throw Error.NoElements();
                }

                value = selector(e.Current);
                while (e.MoveNext())
                {
                    decimal x = selector(e.Current);
                    if (x < value)
                    {
                        value = x;
                    }
                }
            }

            return value;
        }

        public static decimal? Min<TSource>(this IEnumerable<TSource> source, Func<TSource, decimal?> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            decimal? value = null;
            using (IEnumerator<TSource> e = source.GetEnumerator())
            {
                do
                {
                    if (!e.MoveNext())
                    {
                        return value;
                    }

                    value = selector(e.Current);
                }
                while (!value.HasValue);

                decimal valueVal = value.GetValueOrDefault();
                while (e.MoveNext())
                {
                    decimal? cur = selector(e.Current);
                    decimal x = cur.GetValueOrDefault();
                    if (cur.HasValue && x < valueVal)
                    {
                        valueVal = x;
                        value = cur;
                    }
                }
            }

            return value;
        }

        public static TResult Min<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (selector == null)
            {
                throw Error.ArgumentNull(nameof(selector));
            }

            Comparer<TResult> comparer = Comparer<TResult>.Default;
            TResult value = default(TResult);
            if (value == null)
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    do
                    {
                        if (!e.MoveNext())
                        {
                            return value;
                        }

                        value = selector(e.Current);
                    }
                    while (value == null);

                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (x != null && comparer.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
            }
            else
            {
                using (IEnumerator<TSource> e = source.GetEnumerator())
                {
                    if (!e.MoveNext())
                    {
                        throw Error.NoElements();
                    }

                    value = selector(e.Current);
                    while (e.MoveNext())
                    {
                        TResult x = selector(e.Current);
                        if (comparer.Compare(x, value) < 0)
                        {
                            value = x;
                        }
                    }
                }
            }

            return value;
        }
    }
}
