// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace System.Reflection.Extensions.Tests
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
            foreach (Object i in items)
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
