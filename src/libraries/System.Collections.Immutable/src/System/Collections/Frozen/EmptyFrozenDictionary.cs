// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides an empty <see cref="FrozenDictionary{TKey, TValue}"/> to use when there are zero key/value pairs to be stored.</summary>
    internal sealed class EmptyFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>
        where TKey : notnull
    {
        internal EmptyFrozenDictionary(IEqualityComparer<TKey> comparer) : base(comparer) { }

        /// <inheritdoc />
        private protected override TKey[] KeysCore => Array.Empty<TKey>();

        /// <inheritdoc />
        private protected override TValue[] ValuesCore => Array.Empty<TValue>();

        /// <inheritdoc />
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(Array.Empty<TKey>(), Array.Empty<TValue>());

        /// <inheritdoc />
        private protected override int CountCore => 0;

        /// <inheritdoc />
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key) => ref Unsafe.NullRef<TValue>();
    }
}
