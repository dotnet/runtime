// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        // Name needs to be different so it doesn't conflict with Enumerable.Select
        public static U[] Map<T, U>(this ICollection<T> collection, Func<T, U> select)
        {
            int count = collection.Count;
            U[] result = new U[count];
            count = 0;
            foreach (T t in collection)
            {
                result[count++] = select(t);
            }
            return result;
        }
    }
}
