// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text.Json.Serialization
{
    /// <summary>
    /// A bit array that uses an inline <see cref="long"/> for lengths &lt;= 64,
    /// falling back to a heap-allocated <see cref="BitArray"/> for larger sizes.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    internal struct ValueBitArray
    {
        private readonly int _length;
        private long _bits;
        private BitArray? _bitArray;

        public ValueBitArray(int length)
        {
            Debug.Assert(length >= 0);
            _length = length;

            if (length > 64)
            {
                _bitArray = new BitArray(length);
            }
        }

        public int Length => _length;

        public bool this[int index]
        {
            get
            {
                Debug.Assert(index >= 0 && index < _length);

                if (_bitArray is not null)
                {
                    return _bitArray[index];
                }

                return (_bits & (1L << index)) != 0;
            }
            set
            {
                Debug.Assert(index >= 0 && index < _length);
                Debug.Assert(value, "Only setting bits to true is supported.");

                if (_bitArray is not null)
                {
                    _bitArray[index] = true;
                }
                else
                {
                    _bits |= 1L << index;
                }
            }
        }

        public bool HasAllSet()
        {
            if (_bitArray is not null)
            {
                return _bitArray.HasAllSet();
            }

            long allSetMask = _length == 64 ? ~0L : (1L << _length) - 1;

            return (_bits & allSetMask) == allSetMask;
        }
    }
}
