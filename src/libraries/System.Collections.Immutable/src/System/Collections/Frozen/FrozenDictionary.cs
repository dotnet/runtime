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
    /// A frozen dictionary.
    /// </summary>
    /// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// Frozen dictionaries are immutable and are optimized for situations where a dictionary
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a dictionary is created at startup of an application and used throughout the life
    /// of the application.
    ///
    /// This is the general-purpose frozen dictionary which can be used with any key type. If you need
    /// a dictionary that has a string or integer as key, you will get better performance by using
    /// <see cref="FrozenOrdinalStringDictionary{TValue}"/> or <see cref="FrozenIntDictionary{TValue}"/>
    /// respectively.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IFrozenDictionaryDebugView<,>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenDictionary<TKey, TValue> : IFrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly FrozenHashTable _hashTable;
        private readonly TKey[] _keys;
        private readonly TValue[] _values;

        /// <summary>
        /// Gets an empty frozen dictionary.
        /// </summary>
        public static FrozenDictionary<TKey, TValue> Empty => new(Array.Empty<KeyValuePair<TKey, TValue>>(), EqualityComparer<TKey>.Default);

        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenDictionary{TKey, TValue}"/> struct.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <param name="comparer">The comparer used to compare and hash keys.</param>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        internal FrozenDictionary(IEnumerable<KeyValuePair<TKey, TValue>> pairs, IEqualityComparer<TKey> comparer)
        {
            KeyValuePair<TKey, TValue>[] incoming = MakeUniqueArray(pairs, comparer);

            if (ReferenceEquals(comparer, StringComparer.Ordinal) ||
                ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase))
            {
                comparer = (IEqualityComparer<TKey>)ComparerPicker.Pick(SetSupport.ExtractStringKeysToArray(incoming), ignoreCase: ReferenceEquals(comparer, StringComparer.OrdinalIgnoreCase));
            }

            _keys = incoming.Length == 0 ? Array.Empty<TKey>() : new TKey[incoming.Length];
            _values = incoming.Length == 0 ? Array.Empty<TValue>() : new TValue[incoming.Length];
            Comparer = comparer;

            TKey[] keys = _keys;
            TValue[] values = _values;
            _hashTable = FrozenHashTable.Create(
                incoming,
                pair => comparer.GetHashCode(pair.Key),
                (index, pair) =>
                {
                    keys[index] = pair.Key;
                    values[index] = pair.Value;
                });
        }

        private static KeyValuePair<TKey, TValue>[] MakeUniqueArray(IEnumerable<KeyValuePair<TKey, TValue>> pairs, IEqualityComparer<TKey> comp)
        {
            if (!(pairs is Dictionary<TKey, TValue> dict && dict.Comparer.Equals(comp)))
            {
                dict = new Dictionary<TKey, TValue>(comp);
                foreach (KeyValuePair<TKey, TValue> pair in pairs)
                {
                    dict[pair.Key] = pair.Value;
                }
            }

            if (dict.Count == 0)
            {
                return Array.Empty<KeyValuePair<TKey, TValue>>();
            }

            var result = new KeyValuePair<TKey, TValue>[dict.Count];
            ((ICollection<KeyValuePair<TKey, TValue>>)dict).CopyTo(result, 0);

            return result;
        }

        /// <inheritdoc />
        public FrozenList<TKey> Keys => new(_keys);

        /// <inheritdoc />
        public FrozenList<TValue> Values => new(_values);

        /// <inheritdoc />
        public FrozenPairEnumerator<TKey, TValue> GetEnumerator() => new(_keys, _values);

        /// <summary>
        /// Gets an enumeration of the dictionary's keys.
        /// </summary>
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Count > 0 ? _keys : Array.Empty<TKey>();

        /// <summary>
        /// Gets an enumeration of the dictionary's values.
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => Count > 0 ? GetEnumerator() : ((IList<KeyValuePair<TKey, TValue>>)Array.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : ((IList<KeyValuePair<TKey, TValue>>)Array.Empty<KeyValuePair<TKey, TValue>>()).GetEnumerator();

        /// <summary>
        /// Gets the number of key/value pairs in the dictionary.
        /// </summary>
        public int Count => _hashTable.Count;

        /// <summary>
        /// Gets the comparer used by this dictionary.
        /// </summary>
        public IEqualityComparer<TKey> Comparer { get; }

        /// <summary>
        /// Gets the value associated to the given key.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <returns>The associated value.</returns>
        /// <exception cref="KeyNotFoundException">If the key doesn't exist in the dictionary.</exception>
        public TValue this[TKey key]
        {
            get
            {
                int hashCode = Comparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.EntryHashCode(index))
                    {
                        if (Comparer.Equals(key, _keys[index]))
                        {
                            return _values[index];
                        }
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
        public bool ContainsKey(TKey key)
        {
            int hashCode = Comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(key, _keys[index]))
                    {
                        return true;
                    }
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
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            int hashCode = Comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(key, _keys[index]))
                    {
                        value = _values[index];
                        return true;
                    }
                }

                index++;
            }

            value = default!;
            return false;
        }

        /// <inheritdoc />
        public ref readonly TValue GetByRef(TKey key)
        {
            int hashCode = Comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(key, _keys[index]))
                    {
                        return ref _values[index];
                    }
                }

                index++;
            }

            throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public ref readonly TValue TryGetByRef(TKey key)
        {
            int hashCode = Comparer.GetHashCode(key);
            _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

            while (index <= endIndex)
            {
                if (hashCode == _hashTable.EntryHashCode(index))
                {
                    if (Comparer.Equals(key, _keys[index]))
                    {
                        return ref _values[index];
                    }
                }

                index++;
            }

            return ref Unsafe.NullRef<TValue>();
        }

        /// <summary>
        /// Copies the content of the dictionary to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<KeyValuePair<TKey, TValue>> destination)
        {
            ThrowHelper.IfBufferTooSmall(destination.Length, Count);

            for (int i = 0; i < Count; i++)
            {
                destination[i] = new KeyValuePair<TKey, TValue>(_keys[i], _values[i]);
            }
        }

        /// <summary>
        /// Copies the content of the dictionary to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Gets a value indicating whether this collection is a read-only collection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

        /// <summary>
        /// Gets a collection holding this dictionary's keys.
        /// </summary>
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Count > 0 ? _keys : Array.Empty<TKey>();

        /// <summary>
        /// Gets a collection holding this dictionary's values.
        /// </summary>
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Determines whether the dictionary contains the given key/value pair.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><see langword="true"/> if the item is in the dictionary, otherwise <see langword="false"/>. </returns>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            ref readonly TValue v = ref TryGetByRef(item.Key);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in v)))
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(v, item.Value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get => this[key];
            set => throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();
    }
}
