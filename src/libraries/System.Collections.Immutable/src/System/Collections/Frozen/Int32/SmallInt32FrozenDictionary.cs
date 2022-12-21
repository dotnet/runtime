// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections.Frozen
{
    /// <summary>Provides a frozen dictionary to use when the key is an <see cref="int"/>, the default comparer is used, and the item count is small.</summary>
    /// <remarks>
    /// No hashing here, just a straight-up linear scan through the items.
    /// </remarks>
    internal sealed class SmallInt32FrozenDictionary<TValue> : FrozenDictionary<int, TValue>
    {
        private readonly int[] _keys;
        private readonly TValue[] _values;
        private readonly int _max;

        // assumes the keys are sorted
        internal SmallInt32FrozenDictionary(int[] keys, TValue[] values)
            : base(EqualityComparer<int>.Default)
        {
            Debug.Assert(keys.Length != 0);

            _keys = keys;
            _values = values;
            _max = _keys[keys.Length - 1];
        }

        private protected override int[] KeysCore => _keys;
        private protected override TValue[] ValuesCore => _values;
        private protected override Enumerator GetEnumeratorCore() => new Enumerator(_keys, _values);
        private protected override int CountCore => _keys.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected override ref readonly TValue GetValueRefOrNullRefCore(int key)
        {
            if (key <= _max)
            {
                int[] keys = _keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (key <= keys[i])
                    {
                        if (key < keys[i])
                        {
                            break;
                        }

                        return ref _values[i];
                    }
                }
            }

            return ref Unsafe.NullRef<TValue>();
        }
    }
}
