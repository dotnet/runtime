// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    public static partial class Enumerable
    {
        /// <summary>
        /// Flattens an enumerable of enumerables into a single concatenated enumerable.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of the input sequences.</typeparam>
        /// <param name="sources">The source of enumerables to flatten.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> enumerates all inner enumerates in a concatenated view.</returns>
        public static IEnumerable<TSource> Flatten<TSource>(this IEnumerable<IEnumerable<TSource>> sources)
        {
            if (sources == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(sources));
            }

            return FlattenIterator(sources);
        }

        private static IEnumerable<TSource> FlattenIterator<TSource>(IEnumerable<IEnumerable<TSource>> source)
        {
            foreach (IEnumerable<TSource> segment in source)
            {
                foreach (TSource item in segment)
                {
                    yield return item;
                }
            }
        }
    }
}
