// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic
{
    public static class CollectionExtensions
    {
        public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key) =>
            dictionary.GetValueOrDefault(key, default!);

        public static TValue GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            return dictionary.TryGetValue(key, out TValue? value) ? value : defaultValue;
        }

        public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            if (!dictionary.ContainsKey(key))
            {
                dictionary.Add(key, value);
                return true;
            }

            return false;
        }

        public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (dictionary is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.dictionary);
            }

            if (dictionary.TryGetValue(key, out value))
            {
                dictionary.Remove(key);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Returns a read-only <see cref="ReadOnlyCollection{T}"/> wrapper
        /// for the specified list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="list">The list to wrap.</param>
        /// <returns>An object that acts as a read-only wrapper around the current <see cref="IList{T}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="list"/> is null.</exception>
        public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> list) =>
            new ReadOnlyCollection<T>(list);

        /// <summary>
        /// Returns a read-only <see cref="ReadOnlyDictionary{TKey, TValue}"/> wrapper
        /// for the current dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
        /// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
        /// <param name="dictionary">The dictionary to wrap.</param>
        /// <returns>An object that acts as a read-only wrapper around the current <see cref="IDictionary{TKey, TValue}"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        public static ReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> dictionary) where TKey : notnull =>
            new ReadOnlyDictionary<TKey, TValue>(dictionary);

        /// <summary>Adds the elements of the specified span to the end of the <see cref="List{T}"/>.</summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list to which the elements should be added.</param>
        /// <param name="source">The span whose elements should be added to the end of the <see cref="List{T}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="list"/> is null.</exception>
        public static void AddRange<T>(this List<T> list, params ReadOnlySpan<T> source)
        {
            if (list is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
            }

            if (!source.IsEmpty)
            {
                if (list._items.Length - list._size < source.Length)
                {
                    list.Grow(checked(list._size + source.Length));
                }

                source.CopyTo(list._items.AsSpan(list._size));
                list._size += source.Length;
                list._version++;
            }
        }

        /// <summary>Inserts the elements of a span into the <see cref="List{T}"/> at the specified index.</summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list into which the elements should be inserted.</param>
        /// <param name="index">The zero-based index at which the new elements should be inserted.</param>
        /// <param name="source">The span whose elements should be added to the <see cref="List{T}"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="list"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than 0 or greater than <paramref name="list"/>'s <see cref="List{T}.Count"/>.</exception>
        public static void InsertRange<T>(this List<T> list, int index, params ReadOnlySpan<T> source)
        {
            if (list is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
            }

            if ((uint)index > (uint)list._size)
            {
                ThrowHelper.ThrowArgumentOutOfRange_IndexMustBeLessOrEqualException();
            }

            if (!source.IsEmpty)
            {
                if (list._items.Length - list._size < source.Length)
                {
                    list.Grow(checked(list._size + source.Length));
                }

                // If the index at which to insert is less than the number of items in the list,
                // shift all items past that location in the list down to the end, making room
                // to copy in the new data.
                if (index < list._size)
                {
                    Array.Copy(list._items, index, list._items, index + source.Length, list._size - index);
                }

                // Copy the source span into the list.
                // Note that this does not handle the unsafe case of trying to insert a CollectionsMarshal.AsSpan(list)
                // or some slice thereof back into the list itself; such an operation has undefined behavior.
                source.CopyTo(list._items.AsSpan(index));
                list._size += source.Length;
                list._version++;
            }
        }

        /// <summary>Copies the entire <see cref="List{T}"/> to a span.</summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="list">The list from which the elements are copied.</param>
        /// <param name="destination">The span that is the destination of the elements copied from <paramref name="list"/>.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="list"/> is null.</exception>
        /// <exception cref="ArgumentException">The number of elements in the source <see cref="List{T}"/> is greater than the number of elements that the destination span can contain.</exception>
        public static void CopyTo<T>(this List<T> list, Span<T> destination)
        {
            if (list is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.list);
            }

            new ReadOnlySpan<T>(list._items, 0, list._size).CopyTo(destination);
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="Dictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys used by the dictionary instance.</typeparam>
        /// <typeparam name="TValue">The type of the values used by the dictionary instance.</typeparam>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <param name="dictionary">The dictionary instance.</param>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="dictionary"/>'s comparer is not compatible with <typeparamref name="TAlternateKey"/>.</exception>
        /// <remarks>
        /// The supplied <paramref name="dictionary"/> must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public static Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> GetAlternateLookup<TKey, TValue, TAlternateKey>(
            this Dictionary<TKey, TValue> dictionary)
            where TKey : notnull
            where TAlternateKey : notnull, allows ref struct
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (!Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>.IsCompatibleKey(dictionary))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IncompatibleComparer);
            }

            return new Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>(dictionary);
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="Dictionary{TKey, TValue}"/>
        /// using a <typeparamref name="TAlternateKey"/> as a key instead of a <typeparamref name="TKey"/>.
        /// </summary>
        /// <typeparam name="TKey">The type of the keys used by the dictionary instance.</typeparam>
        /// <typeparam name="TValue">The type of the values used by the dictionary instance.</typeparam>
        /// <typeparam name="TAlternateKey">The alternate type of a key for performing lookups.</typeparam>
        /// <param name="dictionary">The dictionary instance.</param>
        /// <param name="lookup">The created lookup instance when the method returns true, or a default instance that should not be used if the method returns false.</param>
        /// <returns>true if a lookup could be created; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="dictionary"/> is null.</exception>
        /// <remarks>
        /// The supplied <paramref name="dictionary"/> must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternateKey, TKey}"/> with
        /// <typeparamref name="TAlternateKey"/> and <typeparamref name="TKey"/>. If it doesn't, the method will return false.
        /// </remarks>
        public static bool TryGetAlternateLookup<TKey, TValue, TAlternateKey>(
            this Dictionary<TKey, TValue> dictionary,
            out Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey> lookup)
            where TKey : notnull
            where TAlternateKey : notnull, allows ref struct
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            if (Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>.IsCompatibleKey(dictionary))
            {
                lookup = new Dictionary<TKey, TValue>.AlternateLookup<TAlternateKey>(dictionary);
                return true;
            }

            lookup = default;
            return false;
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="HashSet{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items used by the set instance.</typeparam>
        /// <typeparam name="TAlternate">The alternate type of instance for performing lookups.</typeparam>
        /// <param name="set">The set instance.</param>
        /// <returns>The created lookup instance.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        /// <exception cref="InvalidOperationException"><paramref name="set"/>'s comparer is not compatible with <typeparamref name="TAlternate"/>.</exception>
        /// <remarks>
        /// The supplied <paramref name="set"/> must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternate, T}"/> with
        /// <typeparamref name="TAlternate"/> and <typeparamref name="T"/>. If it doesn't, an exception will be thrown.
        /// </remarks>
        public static HashSet<T>.AlternateLookup<TAlternate> GetAlternateLookup<T, TAlternate>(
            this HashSet<T> set)
            where TAlternate : allows ref struct
        {
            ArgumentNullException.ThrowIfNull(set);

            if (!HashSet<T>.AlternateLookup<TAlternate>.IsCompatibleItem(set))
            {
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_IncompatibleComparer);
            }

            return new HashSet<T>.AlternateLookup<TAlternate>(set);
        }

        /// <summary>
        /// Gets an instance of a type that may be used to perform operations on a <see cref="HashSet{T}"/>
        /// using a <typeparamref name="TAlternate"/> instead of a <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the items used by the set instance.</typeparam>
        /// <typeparam name="TAlternate">The alternate type of instance for performing lookups.</typeparam>
        /// <param name="set">The set instance.</param>
        /// <param name="lookup">The created lookup instance when the method returns true, or a default instance that should not be used if the method returns false.</param>
        /// <returns>true if a lookup could be created; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="set"/> is null.</exception>
        /// <remarks>
        /// The supplied <paramref name="set"/> must be using a comparer that implements <see cref="IAlternateEqualityComparer{TAlternate, T}"/> with
        /// <typeparamref name="TAlternate"/> and <typeparamref name="T"/>. If it doesn't, the method returns false.
        /// </remarks>
        public static bool TryGetAlternateLookup<T, TAlternate>(
            this HashSet<T> set,
            out HashSet<T>.AlternateLookup<TAlternate> lookup)
            where TAlternate : allows ref struct
        {
            ArgumentNullException.ThrowIfNull(set);

            if (HashSet<T>.AlternateLookup<TAlternate>.IsCompatibleItem(set))
            {
                lookup = new HashSet<T>.AlternateLookup<TAlternate>(set);
                return true;
            }

            lookup = default;
            return false;
        }
    }
}
