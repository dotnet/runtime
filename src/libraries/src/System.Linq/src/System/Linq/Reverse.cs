// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq
{
    public static partial class Enumerable
    {
        public static IEnumerable<TSource> Reverse<TSource>(this IEnumerable<TSource> source)
        {
            if (source == null) throw Error.ArgumentNull("source");
            return ReverseIterator(source);
        }

        private static IEnumerable<TSource> ReverseIterator<TSource>(IEnumerable<TSource> source)
        {
            Buffer<TSource> buffer = new Buffer<TSource>(source);
            for (int i = buffer.count - 1; i >= 0; i--) yield return buffer.items[i];
        }
    }
}
