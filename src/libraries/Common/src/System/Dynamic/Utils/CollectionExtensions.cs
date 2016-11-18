// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace System.Dynamic.Utils
{
    internal static partial class CollectionExtensions
    {
        /// <summary>
        /// Wraps the provided enumerable into a ReadOnlyCollection{T}
        ///
        /// Copies all of the data into a new array, so the data can't be
        /// changed after creation. The exception is if the enumerable is
        /// already a ReadOnlyCollection{T}, in which case we just return it.
        /// </summary>
        [Pure]
        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> enumerable)
        {
            if (enumerable == null)
            {
                return EmptyReadOnlyCollection<T>.Instance;
            }

            var troc = enumerable as TrueReadOnlyCollection<T>;
            if (troc != null)
            {
                return troc;
            }

            var builder = enumerable as ReadOnlyCollectionBuilder<T>;
            if (builder != null)
            {
                return builder.ToReadOnlyCollection();
            }

            T[] array = EnumerableHelpers.ToArray(enumerable);
            return array.Length == 0 ?
                EmptyReadOnlyCollection<T>.Instance :
                new TrueReadOnlyCollection<T>(array);
        }

        // We could probably improve the hashing here
        public static int ListHashCode<T>(this IEnumerable<T> list)
        {
            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            int h = 6551;
            foreach (T t in list)
            {
                h ^= (h << 5) ^ cmp.GetHashCode(t);
            }
            return h;
        }

        [Pure]
        public static bool ListEquals<T>(this ReadOnlyCollection<T> first, ReadOnlyCollection<T> second)
        {
            if (first == second)
            {
                return true;
            }

            int count = first.Count;

            if (count != second.Count)
            {
                return false;
            }

            EqualityComparer<T> cmp = EqualityComparer<T>.Default;
            for(int i = 0; i != count; ++i)
            {
                if (!cmp.Equals(first[i], second[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public static T[] RemoveFirst<T>(this T[] array)
        {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 1, result, 0, result.Length);
            return result;
        }

        public static T[] RemoveLast<T>(this T[] array)
        {
            T[] result = new T[array.Length - 1];
            Array.Copy(array, 0, result, 0, result.Length);
            return result;
        }
    }
}
