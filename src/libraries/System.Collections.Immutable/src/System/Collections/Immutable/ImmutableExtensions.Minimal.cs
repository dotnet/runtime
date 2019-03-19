// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace System.Collections.Immutable
{
    internal static partial class ImmutableExtensions
    {
        /// <summary>
        /// Tries to divine the number of elements in a sequence without actually enumerating each element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="sequence">The enumerable source.</param>
        /// <param name="count">Receives the number of elements in the enumeration, if it could be determined.</param>
        /// <returns><c>true</c> if the count could be determined; <c>false</c> otherwise.</returns>
        internal static bool TryGetCount<T>(this IEnumerable<T> sequence, out int count)
        {
            return TryGetCount<T>((IEnumerable)sequence, out count);
        }

        /// <summary>
        /// Tries to divine the number of elements in a sequence without actually enumerating each element.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequence.</typeparam>
        /// <param name="sequence">The enumerable source.</param>
        /// <param name="count">Receives the number of elements in the enumeration, if it could be determined.</param>
        /// <returns><c>true</c> if the count could be determined; <c>false</c> otherwise.</returns>
        internal static bool TryGetCount<T>(this IEnumerable sequence, out int count)
        {
            var collection = sequence as ICollection;
            if (collection != null)
            {
                count = collection.Count;
                return true;
            }

            var collectionOfT = sequence as ICollection<T>;
            if (collectionOfT != null)
            {
                count = collectionOfT.Count;
                return true;
            }

            var readOnlyCollection = sequence as IReadOnlyCollection<T>;
            if (readOnlyCollection != null)
            {
                count = readOnlyCollection.Count;
                return true;
            }

            count = 0;
            return false;
        }

        /// <summary>
        /// Gets the number of elements in the specified sequence,
        /// while guaranteeing that the sequence is only enumerated once
        /// in total by this method and the caller.
        /// </summary>
        /// <typeparam name="T">The type of element in the collection.</typeparam>
        /// <param name="sequence">The sequence.</param>
        /// <returns>The number of elements in the sequence.</returns>
        internal static int GetCount<T>(ref IEnumerable<T> sequence)
        {
            int count;
            if (!sequence.TryGetCount(out count))
            {
                // We cannot predict the length of the sequence. We must walk the entire sequence
                // to find the count. But avoid our caller also having to enumerate by capturing
                // the enumeration in a snapshot and passing that back to the caller.
                var list = sequence.ToList();
                count = list.Count;
                sequence = list;
            }

            return count;
        }

        /// <summary>
        /// Tries to copy the elements in the sequence to the specified array,
        /// if the sequence is a well-known collection type. Otherwise, does
        /// nothing and returns <c>false</c>.
        /// </summary>
        /// <typeparam name="T">The type of element in the sequence.</typeparam>
        /// <param name="sequence">The sequence to copy.</param>
        /// <param name="array">The array to copy the elements to.</param>
        /// <param name="arrayIndex">The index in the array to start copying.</param>
        /// <returns><c>true</c> if the elements were successfully copied; <c>false</c> otherwise.</returns>
        /// <remarks>
        /// <para>
        /// The reason we don't copy anything other than for well-known types is that a malicious interface
        /// implementation of <see cref="ICollection{T}"/> could hold on to the array when its <see cref="ICollection{T}.CopyTo"/>
        /// method is called. If the array it holds onto underlies an <see cref="ImmutableArray{T}"/>, it could violate
        /// immutability by modifying the array.
        /// </para>
        /// </remarks>
        internal static bool TryCopyTo<T>(this IEnumerable<T> sequence, T[] array, int arrayIndex)
        {
            Debug.Assert(sequence != null);
            Debug.Assert(array != null);
            Debug.Assert(arrayIndex >= 0 && arrayIndex <= array.Length);

            // IList is the GCD of what the following types implement.
            var listInterface = sequence as IList<T>;
            if (listInterface != null)
            {
                var list = sequence as List<T>;
                if (list != null)
                {
                    list.CopyTo(array, arrayIndex);
                    return true;
                }

                // Array.Copy can throw an ArrayTypeMismatchException if the underlying type of
                // the destination array is not typeof(T[]), but is assignment-compatible with T[].
                // See https://github.com/dotnet/corefx/issues/2241 for more info.
                if (sequence.GetType() == typeof(T[]))
                {
                    var sourceArray = (T[])sequence;
                    Array.Copy(sourceArray, 0, array, arrayIndex, sourceArray.Length);
                    return true;
                }

                if (sequence is ImmutableArray<T>)
                {
                    var immutable = (ImmutableArray<T>)sequence;
                    Array.Copy(immutable.array, 0, array, arrayIndex, immutable.Length);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a copy of a sequence as an array.
        /// </summary>
        /// <typeparam name="T">The type of element.</typeparam>
        /// <param name="sequence">The sequence to be copied.</param>
        /// <param name="count">The number of elements in the sequence.</param>
        /// <returns>The array.</returns>
        /// <remarks>
        /// This is more efficient than the <see cref="M:System.Linq.Enumerable.ToArray``1(System.Collections.Generic.IEnumerable{``0})"/> extension method
        /// because that only tries to cast the sequence to <see cref="ICollection{T}"/> to determine
        /// the count before it falls back to reallocating arrays as it enumerates.
        /// </remarks>
        internal static T[] ToArray<T>(this IEnumerable<T> sequence, int count)
        {
            Requires.NotNull(sequence, nameof(sequence));
            Requires.Range(count >= 0, nameof(count));

            if (count == 0)
            {
                return ImmutableArray<T>.Empty.array;
            }

            T[] array = new T[count];

            if (!sequence.TryCopyTo(array, 0))
            {
                int i = 0;
                foreach (var item in sequence)
                {
                    Requires.Argument(i < count);
                    array[i++] = item;
                }

                Requires.Argument(i == count);
            }

            return array;
        }
    }
}
