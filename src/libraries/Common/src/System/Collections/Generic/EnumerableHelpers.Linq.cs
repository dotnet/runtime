// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Linq;

namespace System.Collections.Generic
{
    /// <summary>
    /// Internal helper functions for working with enumerables.
    /// </summary>
    internal static partial class EnumerableHelpers
    {
        /// <summary>
        /// Tries to get the count of the enumerable cheaply.
        /// </summary>
        /// <typeparam name="T">The element type of the source enumerable.</typeparam>
        /// <param name="source">The enumerable to count.</param>
        /// <param name="count">The count of the enumerable, if it could be obtained cheaply.</param>
        /// <returns><c>true</c> if the enumerable could be counted cheaply; otherwise, <c>false</c>.</returns>
        internal static bool TryGetCount<T>(IEnumerable<T> source, out int count)
        {
            Debug.Assert(source != null);

            if (source is ICollection<T> collection)
            {
                count = collection.Count;
                return true;
            }

            if (source is IIListProvider<T> provider)
            {
                return (count = provider.GetCount(onlyIfCheap: true)) >= 0;
            }

            count = -1;
            return false;
        }
    }
}
