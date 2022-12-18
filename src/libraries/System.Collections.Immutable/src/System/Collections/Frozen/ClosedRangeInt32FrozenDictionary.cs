// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is an <see cref="int"/> and the default comparer is used and the item count is small.</summary>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan through the items.
    /// </remarks>
    internal sealed class ClosedRangeInt32FrozenDictionary<TValue> : FrozenDictionary<int, TValue>
    {
        private readonly int[] _keys;
        private readonly TValue[] _values;
        private readonly int _min;

        // assumes the keys are sorted
        internal ClosedRangeInt32FrozenDictionary(int[] keys, TValue[] values)
            : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(keys.Length != 0);

            _keys = keys;
            _values = values;
            _min = keys[0];
        }

        private protected override int[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _keys.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(int key)
        {
            if ((uint)(key - _min) < (uint)_values.Length)
            {
                return ref _values[key - _min];
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
