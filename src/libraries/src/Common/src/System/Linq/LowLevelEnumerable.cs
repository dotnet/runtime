// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace System.Linq
{
    internal static class LowLevelEnumerable
    {
        public static bool Any<T>(this IEnumerable<T> values)
        {
            Debug.Assert(values != null);

            IEnumerator<T> enumerator = values.GetEnumerator();
            return enumerator.MoveNext();
        }

        public static bool Any<T>(this IEnumerable<T> values, Func<T, bool> predicate)
        {
            Debug.Assert(values != null);
            Debug.Assert(predicate != null);
            foreach (T value in values)
            {
                if (predicate(value))
                    return true;
            }
            return false;
        }

        public static IEnumerable<U> Select<T, U>(this IEnumerable<T> values, Func<T, U> func)
        {
            Debug.Assert(values != null);

            foreach (T value in values)
            {
                yield return func(value);
            }
        }

        public static T[] ToArray<T>(this IEnumerable<T> values)
        {
            Debug.Assert(values != null);

            LowLevelList<T> list = new LowLevelList<T>();
            foreach (T value in values)
            {
                list.Add(value);
            }
            return list.ToArray();
        }
    }
}
