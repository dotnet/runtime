// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is a value type, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// While not constrained in this manner, the <typeparamref name="TKey"/> must be an <see cref="IComparable{T}"/>.
    /// This implementation is only used for a set of types that have a known-good <see cref="IComparable{T}"/> implementation; it's not
    /// used for an <see cref="IComparable{T}"/> as we can't know for sure whether it's valid, e.g. if the TKey is a ValueTuple`2, it itself
    /// is comparable, but its items might not be such that trying to compare it will result in exception.
    /// </remarks>
    internal sealed class SmallValueTypeComparableFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly TKey[] _keys;
        private readonly TValue[] _values;
        private readonly TKey _max;

        internal SmallValueTypeComparableFrozenDictionary(Dictionary<TKey, TValue> source) : base(EqualityComparer<TKey>.Default)
        {
            Debug.Assert(default(TKey) is IComparable<TKey>);
            Debug.Assert(default(TKey) is not null);
            Debug.Assert(typeof(TKey).IsValueType);

            Debug.Assert(source.Count != 0);
            Debug.Assert(ReferenceEquals(source.Comparer, EqualityComparer<TKey>.Default));

            _keys = source.Keys.ToArray();
            _values = source.Values.ToArray();

            Array.Sort(_keys, _values);
            _max = _keys[_keys.Length - 1];
        }

        private protected override TKey[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _keys.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(TKey key)
        {
            if (Comparer<TKey>.Default.Compare(key, _max) <= 0)
            {
                TKey[] keys = _keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    int c = Comparer<TKey>.Default.Compare(key, keys[i]);
                    if (c <= 0)
                    {
                        if (c == 0)
                        {
                            return ref _values[i];
                        }

                        break;
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
