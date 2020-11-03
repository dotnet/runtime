// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TResult> OfType<TResult>(this IEnumerable source)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return OfTypeIterator<TResult>(source);
        }

        private static IEnumerable<TResult> OfTypeIterator<TResult>(IEnumerable source)
        {
            foreach (object? obj in source)
            {
                if (obj is TResult result)
                {
                    yield return result;
                }
            }
        }

        public static IEnumerable<
#nullable disable // there's no way to annotate the connection of the nullability of TResult to that of the source
                TResult
#nullable restore
                > Cast<TResult>(this IEnumerable source)
        {
            if (source is IEnumerable<TResult> typedSource)
            {
                return typedSource;
            }

            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            return CastIterator<TResult>(source);
        }

        private static IEnumerable<TResult> CastIterator<TResult>(IEnumerable source)
        {
            foreach (object obj in source)
            {
                yield return (TResult)obj;
            }
        }
    }
}
