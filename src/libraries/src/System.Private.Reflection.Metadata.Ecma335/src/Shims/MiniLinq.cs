// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq
{
    internal static class MiniLinq
    {
        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static List<T> ToList<T>(this IEnumerable<T> source)
        {
            Debug.Assert(source != null);
            return new List<T>(source);
        }

        /// <summary>Converts an enumerable to an array.</summary>
        /// <param name="source">The enumerable to convert.</param>
        /// <returns>The resulting array.</returns>
        internal static T[] ToArray<T>(this IEnumerable<T> source)
        {
            Debug.Assert(source != null);

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                int count = collection.Count;
                if (count == 0)
                {
                    return Array.Empty<T>();
                }

                var result = new T[count];
                collection.CopyTo(result, arrayIndex: 0);
                return result;
            }

            using (var en = source.GetEnumerator())
            {
                if (en.MoveNext())
                {
                    const int DefaultCapacity = 4;
                    T[] arr = new T[DefaultCapacity];
                    arr[0] = en.Current;
                    int count = 1;

                    while (en.MoveNext())
                    {
                        if (count == arr.Length)
                        {
                            // MaxArrayLength is defined in Array.MaxArrayLength and in gchelpers in CoreCLR.
                            // It represents the maximum number of elements that can be in an array where
                            // the size of the element is greater than one byte; a separate, slightly larger constant,
                            // is used when the size of the element is one.
                            const int MaxArrayLength = 0x7FEFFFFF;

                            // This is the same growth logic as in List<T>:
                            // If the array is currently empty, we make it a default size.  Otherwise, we attempt to
                            // double the size of the array.  Doubling will overflow once the size of the array reaches
                            // 2^30, since doubling to 2^31 is 1 larger than Int32.MaxValue.  In that case, we instead
                            // constrain the length to be MaxArrayLength (this overflow check works because of the
                            // cast to uint).  Because a slightly larger constant is used when T is one byte in size, we
                            // could then end up in a situation where arr.Length is MaxArrayLength or slightly larger, such
                            // that we constrain newLength to be MaxArrayLength but the needed number of elements is actually
                            // larger than that.  For that case, we then ensure that the newLength is large enough to hold
                            // the desired capacity.  This does mean that in the very rare case where we've grown to such a
                            // large size, each new element added after MaxArrayLength will end up doing a resize.
                            int newLength = count << 1;
                            if ((uint)newLength > MaxArrayLength)
                            {
                                newLength = MaxArrayLength <= count ? count + 1 : MaxArrayLength;
                            }

                            Array.Resize(ref arr, newLength);
                        }

                        arr[count++] = en.Current;
                    }

                    if (count != arr.Length)
                        Array.Resize(ref arr, count);

                    return arr;
                }
                else
                {
                    return Array.Empty<T>();
                }
            }
        }
    }
}