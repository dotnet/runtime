// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value) =>
            source is ICollection<TSource> collection ? collection.Contains(value) :
            Contains(source, value, null);

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            if (source == null)
            {
                throw Error.ArgumentNull(nameof(source));
            }

            if (comparer == null)
            {
                foreach (TSource element in source)
                {
                    if (EqualityComparer<TSource>.Default.Equals(element, value)) // benefits from devirtualization and likely inlining
                    {
                        return true;
                    }
                }
            }
            else
            {
                foreach (TSource element in source)
                {
                    if (comparer.Equals(element, value))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
