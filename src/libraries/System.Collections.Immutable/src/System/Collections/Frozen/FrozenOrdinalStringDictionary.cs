// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Immutable
{
    /// <summary>
    /// A frozen dictionary with string keys compared with ordinal semantics.
    /// </summary>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// Frozen dictionaries are immutable and are optimized for situations where a dictionary
    /// is created infrequently, but used repeatedly at runtime. They have a relatively high
    /// cost to create, but provide excellent lookup performance. These are thus ideal for cases
    /// where a dictionary is created at startup of an application and used throughout the life
    /// of the application.
    /// </remarks>
    [DebuggerTypeProxy(typeof(IFrozenOrdinalStringDictionaryDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public readonly struct FrozenOrdinalStringDictionary<TValue> : IFrozenDictionary<string, TValue>, IDictionary<string, TValue>
    {
        private readonly FrozenHashTable _hashTable;
        private readonly string[] _keys;
        private readonly TValue[] _values;
        private readonly StringComparerBase _comparer;

        /// <summary>
        /// Gets an empty frozen string dictionary.
        /// </summary>
        public static FrozenOrdinalStringDictionary<TValue> Empty => new(Array.Empty<KeyValuePair<string, TValue>>());

        /// <summary>
        /// Initializes a new instance of the <see cref="FrozenOrdinalStringDictionary{TValue}"/> struct.
        /// </summary>
        /// <param name="pairs">The pairs to initialize the dictionary with.</param>
        /// <param name="ignoreCase">Whether to use case-insensitive semantics.</param>
        /// <exception cref="ArgumentException">If more than 64K pairs are added.</exception>
        /// <remarks>
        /// Tf the same key appears multiple times in the input, the latter one in the sequence takes precedence.
        /// </remarks>
        internal FrozenOrdinalStringDictionary(IEnumerable<KeyValuePair<string, TValue>> pairs, bool ignoreCase = false)
        {
            KeyValuePair<string, TValue>[] incoming = MakeUniqueArray(pairs, ignoreCase);

            _keys = incoming.Length == 0 ? Array.Empty<string>() : new string[incoming.Length];
            _values = incoming.Length == 0 ? Array.Empty<TValue>() : new TValue[incoming.Length];
            _comparer = ComparerPicker.Pick(SetSupport.ExtractStringKeysToArray(incoming), ignoreCase);

            string[] keys = _keys;
            TValue[] values = _values;
            StringComparerBase comparer = _comparer;
            _hashTable = FrozenHashTable.Create(
                incoming,
                pair => comparer.GetHashCode(pair.Key),
                (index, pair) =>
                {
                    keys[index] = pair.Key;
                    values[index] = pair.Value;
                });
        }

        private static KeyValuePair<string, TValue>[] MakeUniqueArray(IEnumerable<KeyValuePair<string, TValue>> pairs, bool ignoreCase)
        {
            StringComparer comp = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            if (!(pairs is Dictionary<string, TValue> dict && dict.Comparer.Equals(comp)))
            {
                dict = new Dictionary<string, TValue>(comp);
                foreach (KeyValuePair<string, TValue> pair in pairs)
                {
                    dict[pair.Key] = pair.Value;
                }
            }

            if (dict.Count == 0)
            {
                return Array.Empty<KeyValuePair<string, TValue>>();
            }

            var result = new KeyValuePair<string, TValue>[dict.Count];
            ((ICollection<KeyValuePair<string, TValue>>)dict).CopyTo(result, 0);

            return result;
        }

        /// <inheritdoc />
        public FrozenList<string> Keys => new(_keys);

        /// <inheritdoc />
        public FrozenList<TValue> Values => new(_values);

        /// <inheritdoc />
        public FrozenPairEnumerator<string, TValue> GetEnumerator() => new(_keys, _values);

        /// <summary>
        /// Gets an enumeration of the dictionary's keys.
        /// </summary>
        IEnumerable<string> IReadOnlyDictionary<string, TValue>.Keys => Count > 0 ? _keys : Array.Empty<string>();

        /// <summary>
        /// Gets an enumeration of the dictionary's values.
        /// </summary>
        IEnumerable<TValue> IReadOnlyDictionary<string, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator<KeyValuePair<string, TValue>> IEnumerable<KeyValuePair<string, TValue>>.GetEnumerator()
            => Count > 0 ? GetEnumerator() : ((IList<KeyValuePair<string, TValue>>)Array.Empty<KeyValuePair<string, TValue>>()).GetEnumerator();

        /// <summary>
        /// Gets an enumeration of the dictionary's key/value/pairs.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator() => Count > 0 ? GetEnumerator() : Array.Empty<KeyValuePair<string, TValue>>().GetEnumerator();

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
        public TValue this[string key]
        {
            get
            {
                if (!_comparer.TrivialReject(key))
                {
                    int hashCode = _comparer.GetHashCode(key);
                    _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                    while (index <= endIndex)
                    {
                        if (hashCode == _hashTable.EntryHashCode(index))
                        {
                            if (_comparer.Equals(key, _keys[index]))
                            {
                                return _values[index];
                            }
                        }

                        index++;
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        /// <summary>
        /// Checks whether a particular key exists in the dictionary.
        /// </summary>
        /// <param name="key">The key to probe for.</param>
        /// <returns><see langword="true"/> if the key is in the dictionary, otherwise <see langword="false"/>.</returns>
        public bool ContainsKey(string key)
        {
            if (!_comparer.TrivialReject(key))
            {
                int hashCode = _comparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.EntryHashCode(index))
                    {
                        if (_comparer.Equals(key, _keys[index]))
                        {
                            return true;
                        }
                    }

                    index++;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to get a value associated with a specific key.
        /// </summary>
        /// <param name="key">The key to lookup.</param>
        /// <param name="value">The value associated with the key.</param>
        /// <returns><see langword="true"/> if the key was found, otherwise <see langword="false"/>.</returns>
        public bool TryGetValue(string key, [MaybeNullWhen(false)] out TValue value)
        {
            if (!_comparer.TrivialReject(key))
            {
                int hashCode = _comparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.EntryHashCode(index))
                    {
                        if (_comparer.Equals(key, _keys[index]))
                        {
                            value = _values[index];
                            return true;
                        }
                    }

                    index++;
                }
            }

            value = default!;
            return false;
        }

        /// <inheritdoc />
        public ref readonly TValue GetByRef(string key)
        {
            if (!_comparer.TrivialReject(key))
            {
                int hashCode = _comparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.EntryHashCode(index))
                    {
                        if (_comparer.Equals(key, _keys[index]))
                        {
                            return ref _values[index];
                        }
                    }

                    index++;
                }
            }

            throw new KeyNotFoundException();
        }

        /// <inheritdoc />
        public ref readonly TValue TryGetByRef(string key)
        {
            if (!_comparer.TrivialReject(key))
            {
                int hashCode = _comparer.GetHashCode(key);
                _hashTable.FindMatchingEntries(hashCode, out int index, out int endIndex);

                while (index <= endIndex)
                {
                    if (hashCode == _hashTable.EntryHashCode(index))
                    {
                        if (_comparer.Equals(key, _keys[index]))
                        {
                            return ref _values[index];
                        }
                    }

                    index++;
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }

        /// <summary>
        /// Copies the content of the dictionary to a span.
        /// </summary>
        /// <param name="destination">The destination where to copy to.</param>
        public void CopyTo(Span<KeyValuePair<string, TValue>> destination)
        {
            ThrowHelper.IfBufferTooSmall(destination.Length, Count);

            for (int i = 0; i < Count; i++)
            {
                destination[i] = new KeyValuePair<string, TValue>(_keys[i], _values[i]);
            }
        }

        /// <summary>
        /// Copies the content of the dictionary to an array.
        /// </summary>
        /// <param name="array">The destination where to copy to.</param>
        /// <param name="arrayIndex">Index into the array where to start copying the data.</param>
        public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex) => CopyTo(array.AsSpan(arrayIndex));

        /// <summary>
        /// Gets a value indicating whether this collection is a read-only collection.
        /// </summary>
        /// <returns>Always returns true.</returns>
        bool ICollection<KeyValuePair<string, TValue>>.IsReadOnly => true;

        /// <summary>
        /// Gets a collection holding this dictionary's keys.
        /// </summary>
        ICollection<string> IDictionary<string, TValue>.Keys => Count > 0 ? _keys : Array.Empty<string>();

        /// <summary>
        /// Gets a collection holding this dictionary's values.
        /// </summary>
        ICollection<TValue> IDictionary<string, TValue>.Values => Count > 0 ? _values : Array.Empty<TValue>();

        /// <summary>
        /// Determines whether the dictionary contains the given key/value pair.
        /// </summary>
        /// <param name="item">The item to search for.</param>
        /// <returns><see langword="true"/> if the item is in the dictionary, otherwise <see langword="false"/>. </returns>
        bool ICollection<KeyValuePair<string, TValue>>.Contains(KeyValuePair<string, TValue> item)
        {
            ref readonly TValue v = ref TryGetByRef(item.Key);
            if (Unsafe.IsNullRef(ref Unsafe.AsRef(in v)))
            {
                return false;
            }

            return EqualityComparer<TValue>.Default.Equals(v, item.Value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        TValue IDictionary<string, TValue>.this[string key]
        {
            get => this[key];
            set => throw new NotSupportedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<string, TValue>>.Add(KeyValuePair<string, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void ICollection<KeyValuePair<string, TValue>>.Clear() => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool ICollection<KeyValuePair<string, TValue>>.Remove(KeyValuePair<string, TValue> item) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        void IDictionary<string, TValue>.Add(string key, TValue value) => throw new NotSupportedException();

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool IDictionary<string, TValue>.Remove(string key) => throw new NotSupportedException();
    }
}
