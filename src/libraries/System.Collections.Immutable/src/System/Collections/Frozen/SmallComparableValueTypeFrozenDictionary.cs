// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is a comparable value type, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// No hashing involved, just a linear scan through the keys.  This implementation is close in nature to that of <see cref="SmallComparableValueTypeFrozenDictionary{TKey, TValue}"/>,
    /// except that this implementation sorts the keys in order to a) extract a max that it can compare against at the beginning of each match in order to
    /// immediately rule out keys too large to be contained, and b) early-exits from the linear scan when a comparison determines the key is too
    /// small to be contained.
    /// </remarks>
    internal sealed class SmallComparableValueTypeFrozenDictionary<TKey, TValue> : FrozenDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly TKey[] _keys;
        private readonly TValue[] _values;
        private readonly TKey _max;

        internal SmallComparableValueTypeFrozenDictionary(Dictionary<TKey, TValue> source) : base(EqualityComparer<TKey>.Default)
        {
            // TKey is logically constrained to `where TKey : struct, IComparable<TKey>`, but we can't actually write that
            // constraint currently and still have this be used from the calling context that has an unconstrained TKey.
            // So, we assert it here instead. The implementation relies on {Equality}Comparer<TKey>.Default to sort things out.
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
