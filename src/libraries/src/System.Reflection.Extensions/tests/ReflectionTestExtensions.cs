// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.Tests
{
    public static class Extensions
    {
        public static T First<T>(this IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                return item;
            }

            return default(T);
        }

        public static int Count<T>(this IEnumerable<T> items)
        {
            int count = 0;
            foreach (object i in items)
            {
                count++;
            }
            return count;
        }

        public static int Count<T>(this IEnumerable<T> items, Func<T, bool> predicate)
        {
            int count = 0;
            foreach (T i in items)
            {
                if (predicate(i)) count++;
            }
            return count;
        }
    }
}
