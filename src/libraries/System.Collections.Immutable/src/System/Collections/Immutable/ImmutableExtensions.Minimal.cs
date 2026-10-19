// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
            if (sequence is ICollection collection)
            {
                count = collection.Count;
                return true;
            }

            if (sequence is ICollection<T> collectionOfT)
            {
                count = collectionOfT.Count;
                return true;
            }

            if (sequence is IReadOnlyCollection<T> readOnlyCollection)
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
                List<T> list = sequence.ToList();
                count = list.Count;
                sequence = list;
            }

            return count;
        }

        /// <summary>
        /// Tries to copy the elements in the sequence to the specified array,
        /// if the sequence implements <see cref="ICollection{T}"/>. Otherwise, does
        /// nothing and returns <c>false</c>.
        /// </summary>
        /// <typeparam name="T">The type of element in the sequence.</typeparam>
        /// <param name="sequence">The sequence to copy.</param>
        /// <param name="array">The array to copy the elements to.</param>
        /// <param name="arrayIndex">The index in the array to start copying.</param>
        /// <returns><c>true</c> if the elements were successfully copied; <c>false</c> otherwise.</returns>
        internal static bool TryCopyTo<T>(this IEnumerable<T> sequence, T[] array, int arrayIndex)
        {
            Debug.Assert(sequence != null);
            Debug.Assert(array != null);
            Debug.Assert(arrayIndex >= 0 && arrayIndex <= array.Length);

            if (sequence is ICollection<T> collection)
            {
#if !NET
                // On .NET Framework, an array's ICollection<T>.CopyTo calls Array.Copy, which throws ArrayTypeMismatchException for some arrays whose element type is not T
                // (for example, a uint[] passed as IEnumerable<int>), so conservatively fall back to enumeration for any array that is not exactly a T[].
                // See https://github.com/dotnet/runtime/issues/14794 for more info.
                if (collection is Array && collection.GetType() != typeof(T[]))
                {
                    return false;
                }
#endif

                collection.CopyTo(array, arrayIndex);
                return true;
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
        /// This is more efficient than the <see cref="System.Linq.Enumerable.ToArray{TSource}(IEnumerable{TSource})"/> extension method
        /// because that only tries to cast the sequence to <see cref="ICollection{T}"/> to determine
        /// the count before it falls back to reallocating arrays as it enumerates.
        /// </remarks>
        internal static T[] ToArray<T>(this IEnumerable<T> sequence, int count)
        {
            Requires.NotNull(sequence, nameof(sequence));
            Requires.Range(count >= 0, nameof(count));

            if (count == 0)
            {
                return ImmutableArray<T>.Empty.array!;
            }

            T[] array = new T[count];

            if (!sequence.TryCopyTo(array, 0))
            {
                int i = 0;
                foreach (T item in sequence)
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
