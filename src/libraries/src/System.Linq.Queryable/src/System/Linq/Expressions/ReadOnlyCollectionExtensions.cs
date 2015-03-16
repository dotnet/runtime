// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace System.Linq.Expressions
{
    internal static class ReadOnlyCollectionExtensions
    {
        internal static ReadOnlyCollection<T> ToReadOnlyCollection<T>(this IEnumerable<T> sequence)
        {
            if (sequence == null)
                return DefaultReadOnlyCollection<T>.Empty;
            ReadOnlyCollection<T> col = sequence as ReadOnlyCollection<T>;
            if (col != null)
                return col;
            return new ReadOnlyCollection<T>(sequence.ToArray());
        }
        private static class DefaultReadOnlyCollection<T>
        {
            private static volatile ReadOnlyCollection<T> s_defaultCollection;
            internal static ReadOnlyCollection<T> Empty
            {
                get
                {
                    if (s_defaultCollection == null)
                        s_defaultCollection = new ReadOnlyCollection<T>(new T[] { });
                    return s_defaultCollection;
                }
            }
        }
    }
}
