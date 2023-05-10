// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is a value type, the default comparer is used, and the item count is small.</summary>
    internal sealed class SmallValueTypeDefaultComparerFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly TKey[] _keys;
        private readonly TValue[] _values;

        internal SmallValueTypeDefaultComparerFrozenDictionary(Dictionary<TKey, TValue> source) : base(EqualityComparer<TKey>.Default)
        {
            Debug.Assert(default(TKey) is not null);
            Debug.Assert(typeof(TKey).IsValueType);

            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<TKey>.Default));

            _keys = source.Keys.ToArray();
            _values = source.Values.ToArray();
        }

        private protected override TKey[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _keys.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
        {
            TKey[] keys = _keys;
            for (int i = 0; i < keys.Length; i++)
            {
                if (EqualityComparer<TKey>.Default.Equals(keys[i], key))
                {
                    return ref _values[i];
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
