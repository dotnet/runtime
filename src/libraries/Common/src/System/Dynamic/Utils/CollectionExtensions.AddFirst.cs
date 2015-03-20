// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        public static T[] AddFirst<T>(this IList<T> list, T item)
        {
            T[] res = new T[list.Count + 1];
            res[0] = item;
            list.CopyTo(res, 1);
            return res;
        }
    }
}
