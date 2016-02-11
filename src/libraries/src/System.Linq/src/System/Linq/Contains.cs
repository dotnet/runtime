// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value)
        {
            ICollection<TSource> collection = source as ICollection<TSource>;
            if (collection != null) return collection.Contains(value);
            return Contains(source, value, null);
        }

        public static bool Contains<TSource>(this IEnumerable<TSource> source, TSource value, IEqualityComparer<TSource> comparer)
        {
            if (comparer == null) comparer = EqualityComparer<TSource>.Default;
            if (source == null) throw Error.ArgumentNull("source");
            foreach (TSource element in source)
                if (comparer.Equals(element, value)) return true;
            return false;
        }
    }
}
