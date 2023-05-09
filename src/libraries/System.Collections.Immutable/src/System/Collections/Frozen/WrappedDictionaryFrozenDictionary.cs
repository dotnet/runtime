// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Collections.Frozen
{
    /// <summary><see cref="FrozenDictionary{TKey, TValue}"/> implementation that just wraps a <see cref="Dictionary{TKey, TValue}"/>.</summary>
    internal sealed class WrappedDictionaryFrozenDictionary<TKey, TValue> :
        FrozenDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        // Note that while most of the FrozenDictionary implementations have an equivalent FrozenSet implementation,
        // there's no corresponding WrappedHashSetFrozenSet<T> because HashSet<T> doesn't provide a way to implement
        // FrozenSet<T>.FindItemIndex.

        private readonly Dictionary<TKey, TValue> _source;
        private TKey[]? _keys;
        private TValue[]? _values;

        internal WrappedDictionaryFrozenDictionary(Dictionary<TKey, TValue> source, bool sourceIsCopy) : base(source.Comparer) =>
            _source = sourceIsCopy ? source : new Dictionary<TKey, TValue>(source, source.Comparer);

        /// <inheritdoc />
        private protected sealed override TKey[] KeysCore =>
            _keys ??
            Interlocked.CompareExchange(ref _keys, _source.Keys.ToArray(), null) ??
            _keys;

        /// <inheritdoc />
        private protected sealed override TValue[] ValuesCore =>
            _values ??
            Interlocked.CompareExchange(ref _values, _source.Values.ToArray(), null) ??
            _values;

        /// <inheritdoc />
        private protected sealed override Enumerator GetEnumeratorCore() => new Enumerator(_keys ?? KeysCore, _values ?? ValuesCore);

        /// <inheritdoc />
        private protected sealed override int CountCore => _source.Count;

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key) =>
            ref CollectionsMarshal.GetValueRefOrNullRef(_source, key);
    }
}
