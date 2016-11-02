// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        public static T[] AddLast<T>(this IList<T> list, T item)
        {
            T[] res = new T[list.Count + 1];
            list.CopyTo(res, 0);
            res[list.Count] = item;
            return res;
        }

        public static T First<T>(this IEnumerable<T> source)
        {
            var list = source as IList<T>;
            if (list != null)
            {
                return list[0];
            }
            using (var e = source.GetEnumerator())
            {
                if (e.MoveNext()) return e.Current;
            }
            throw new InvalidOperationException();
        }
    }
}
