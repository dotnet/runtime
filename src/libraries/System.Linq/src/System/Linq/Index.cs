// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using static System.Linq.Utilities;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>Returns an enumerable that incorporates the element's index into a tuple.</summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source" />.</typeparam>
        /// <param name="source">The source enumerable providing the elements.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source" /> is <see langword="null" />.</exception>
        public static IEnumerable<(int Index, TSource Item)> Index<TSource>(this IEnumerable<TSource> source)
        {
            if (source is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            if (IsEmptyArray(source))
            {
                return [];
            }
#if OPTIMIZE_FOR_SIZE
            return new IEnumerableSelect2Iterator<TSource, (int Index, TSource Item)>(source, (x, i) => (i, x));
#else
            return new IEnumerableIndexIterator<TSource>(source);
#endif
        }
    }
}
