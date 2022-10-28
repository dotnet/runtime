// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A frozen dictionary with integer keys.
    /// </summary>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// Frozen dictionaries are immutable and are optimized for situations where a dictionary
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a dictionary is created at startup of an application and used throughout the life
    /// of the application.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IFrozenIntDictionaryDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenIntDictionary<TValue> : IFrozenDictionary<int, TValue>, IDictionary<int, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly TValue[] _values;

        /// <summary>
        /// Gets an empty frozen integer dictionary.
        /// </summary>
        public static FrozenIntDictionary<TValue> Empty => new(Array.Empty<KeyValuePair<int, TValue>>());

        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenIntDictionary{TValue}"/> struct.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        internal FrozenIntDictionary(IEnumerable<KeyValuePair<int, TValue>> pairs)
        {
            KeyValuePair<int, TValue>[] incoming = MakeUniqueArray(pairs);

            _values = incoming.Length == 0 ? Array.Empty<TValue>() : new TValue[incoming.Length];

            TValue[] values = _values;
            _hashTable = FrozenHashTable.Create(
                incoming,
                pair => pair.Key,
                (index, pair) => values[index] = pair.Value);
        }

        private static KeyValuePair<int, TValue>[] MakeUniqueArray(IEnumerable<KeyValuePair<int, TValue>> pairs)
        {
            EqualityComparer<int> comp = EqualityComparer<int>.Default;
            if (!(pairs is Dictionary<int, TValue> dict && dict.Comparer == comp))
            {
                dict = new Dictionary<int, TValue>(comp);
                foreach (KeyValuePair<int, TValue> pair in pairs)
                {
                    dict[pair.Key] = pair.Value;
                }
            }

            if (dict.Count == 0)
            {
                return Array.Empty<KeyValuePair<int, TValue>>();
            }

            var result = new KeyValuePair<int, TValue>[dict.Count];
            ((ICollection<KeyValuePair<int, TValue>>)dict).CopyTo(result, 0);

            return result;
        }

        /// <inheritdoc />
        public FrozenList<int> Keys => new(_hashTable.HashCodes);

        /// <inheritdoc />
        public FrozenList<TValue> Values => new(_values);

        /// <inheritdoc />
        public FrozenPairEnumerator<int, TValue> GetEnumerator() => new(_hashTable.HashCodes, _values);

        /// <summary>
        /// Gets an enumeration of the dictionary's keys.
        /// </summary>
        IEnumerable<int> IReadOnlyDictionary<int, TValue>.Keys => Count > 0 ? _hashTable.HashCodes : Array.Empty<int>();

        /// <summary>
        /// Gets an enumeration of the dictionary's values.
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<int, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<KeyValuePair<int, TValue>> IEnumerable<KeyValuePair<int, TValue>>.GetEnumerator()
            => Count > 0 ? GetEnumerator() : ((IList<KeyValuePair<int, TValue>>)Array.Empty<KeyValuePair<int, TValue>>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<KeyValuePair<int, TValue>>)Array.Empty<KeyValuePair<int, TValue>>()).GetEnumerator();

        /// <summary>
        /// Gets the number of key/value pairs in the dictionary.
        /// </summary>
        public int Count => _hashTable.Count;

        /// <summary>
        /// Gets the value associated to the given key.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns>The associated value.</returns>
        /// <exception cref="KeyNotFoundException">If the key doesn't exist in the dictionary.</exception>
        public TValue this[int key]
        {
            get
            {
                _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (key == _hashTable.EntryHashCode(index))
                    {
                        return _values[index];
                    }

                    index++;
                }

                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// Checks whether a particular key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to probe for.</param>
        /// <returns><see langword="true"/> if the key is in the dictionary, otherwise <see langword="false"/>.</returns>
        public bool ContainsKey(int key)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (key == _hashTable.EntryHashCode(index))
                {
                    return true;
                }

                index++;
            }

            return false;
        }

        /// <summary>
        /// Tries to get a value associated with a specific key.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns><see langword="true"/> if the key was found, otherwise <see langword="false"/>.</returns>
        public bool TryGetValue(int key, [MaybeNullWhen(false)] out TValue value)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (key == _hashTable.EntryHashCode(index))
                {
                    value = _values[index];
                    return true;
                }

                index++;
            }

            value = default!;
            return false;
        }

        /// <inheritdoc />
        public ref readonly TValue GetByRef(int key)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (key == _hashTable.EntryHashCode(index))
                {
                    return ref _values[index];
                }

                index++;
            }

            throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public ref readonly TValue TryGetByRef(int key)
        {
            _hashTable.FindMatchingEntries(key, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (key == _hashTable.EntryHashCode(index))
                {
                    return ref _values[index];
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }

        /// <summary>
        /// Copies the content of the dictionary to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<KeyValuePair<int, TValue>> destination)
        {
            ThrowHelper.IfBufferTooSmall(destination.Length, Count);

            for (int i = 0; i < Count; i++)
            {
                destination[i] = new KeyValuePair<int, TValue>(_hashTable.HashCodes[i], _values[i]);
            }
        }

        /// <summary>
        /// Copies the content of the dictionary to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(KeyValuePair<int, TValue>[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Gets a value indicating whether this collection is a read-only collection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICollection<KeyValuePair<int, TValue>>.IsReadOnly => true;

        /// <summary>
        /// Gets a collection holding this dictionary's keys.
        /// </summary>
        ICollection<int> IDictionary<int, TValue>.Keys => Count > 0 ? _hashTable.HashCodes : Array.Empty<int>();

        /// <summary>
        /// Gets a collection holding this dictionary's values.
        /// </summary>
        ICollection<TValue> IDictionary<int, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Determines whether the dictionary contains the given key/value pair.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><see langword="true"/> if the item is in the dictionary, otherwise <see langword="false"/>. </returns>
        bool ICollection<KeyValuePair<int, TValue>>.Contains(KeyValuePair<int, TValue> item)
        {
            ref readonly TValue v = ref TryGetByRef(item.Key);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in v)))
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(v, item.Value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        TValue IDictionary<int, TValue>.this[int key]
        {
            get => this[key];
            set => throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<int, TValue>>.Add(KeyValuePair<int, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<int, TValue>>.Clear() => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<KeyValuePair<int, TValue>>.Remove(KeyValuePair<int, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void IDictionary<int, TValue>.Add(int key, TValue value) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDictionary<int, TValue>.Remove(int key) => throw new NotSupportedException();
    }
}
