// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

            var collection = enumerable as ICollection<T>;
            if (collection != null)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return EmptyReadOnlyCollection<T>.Instance;
                }

                T[] clone = new T[count];
                collection.CopyTo(clone, 0);
                return new TrueReadOnlyCollection<T>(clone);
            }

            // ToArray trims the excess space and speeds up access
            return new TrueReadOnlyCollection<T>(new List<T>(enumerable).ToArray());
        }

        // We could probably improve the hashing here
        public static int ListHashCode<T>(this IEnumerable<T> list)
        {
            var cmp = EqualityComparer<T>.Default;
            int h = 6551;
            foreach (T t in list)
            {
                h ^= (h << 5) ^ cmp.GetHashCode(t);
            }
            return h;
        }

        [Pure]
        public static bool ListEquals<T>(this ICollection<T> first, ICollection<T> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }
            var cmp = EqualityComparer<T>.Default;
            var f = first.GetEnumerator();
            var s = second.GetEnumerator();
            while (f.MoveNext())
            {
                s.MoveNext();

                if (!cmp.Equals(f.Current, s.Current))
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
