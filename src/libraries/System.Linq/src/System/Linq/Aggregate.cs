// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static TSource Aggregate<TSource>(this IEnumerable<TSource> source, Func<TSource, TSource, TSource> func)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            TSource result;
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                if (span.IsEmpty)
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                result = span[0];
                for (int i = 1; i < span.Length; i++)
                {
                    result = func(result, span[i]);
                }
            }
            else
            {
                using IEnumerator<TSource> e = source.GetEnumerator();

                if (!e.MoveNext())
                {
                    ThrowHelper.ThrowNoElementsException();
                }

                result = e.Current;
                while (e.MoveNext())
                {
                    result = func(result, e.Current);
                }
            }

            return result;
        }

        public static TAccumulate Aggregate<TSource, TAccumulate>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            TAccumulate result = seed;
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                foreach (TSource element in span)
                {
                    result = func(result, element);
                }
            }
            else
            {
                foreach (TSource element in source)
                {
                    result = func(result, element);
                }
            }

            return result;
        }

        public static TResult Aggregate<TSource, TAccumulate, TResult>(this IEnumerable<TSource> source, TAccumulate seed, Func<TAccumulate, TSource, TAccumulate> func, Func<TAccumulate, TResult> resultSelector)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (func is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.func);
            }

            if (resultSelector is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
            }

            TAccumulate result = seed;
            if (source.TryGetSpan(out ReadOnlySpan<TSource> span))
            {
                foreach (TSource element in span)
                {
                    result = func(result, element);
                }
            }
            else
            {
                foreach (TSource element in source)
                {
                    result = func(result, element);
                }
            }

            return resultSelector(result);
        }
    }
}
