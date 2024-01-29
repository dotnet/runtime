// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Internal.LowLevelLinq
{
    internal static partial class LowLevelEnumerable
    {
        public static bool Any<T>(this IEnumerable<T> values)
        {
            Debug.Assert(values != null);

            IEnumerator<T> enumerator = values.GetEnumerator();
            return enumerator.MoveNext();
        }

        public static IEnumerable<U> Select<T, U>(this IEnumerable<T> values, Func<T, U> func)
        {
            Debug.Assert(values != null);

            foreach (T value in values)
            {
                yield return func(value);
            }
        }
        public static IEnumerable<T> Where<T>(this IEnumerable<T> source, Func<T, bool> filter)
        {
            Debug.Assert(source != null);
            Debug.Assert(filter != null);

            foreach (T value in source)
            {
                if (filter(value))
                    yield return value;
            }
        }

        public static IEnumerable<T> AsEnumerable<T>(this IEnumerable<T> source)
        {
            Debug.Assert(source != null);
            return source;
        }

        public static int Count<T>(this IEnumerable<T> enumeration)
        {
            Debug.Assert(enumeration != null);

            var collection = enumeration as ICollection<T>;
            if (collection != null)
                return collection.Count;

            int i = 0;
            foreach (T element in enumeration)
            {
                i++;
            }

            return i;
        }
    }
}
