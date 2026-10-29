// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;

namespace System.Text.Json.Serialization
{
    internal readonly ref struct ValueBitArray
    {
        public const int ScratchBufferSize = 4;

        private readonly int _bitCount;
        private readonly Span<ulong> _buffer;

        public readonly bool IsEmpty
        {
            get
            {
                bool isEmpty = true;
                foreach (ulong word in _buffer)
                {
                    isEmpty &= word is 0;
                }

                return isEmpty;
            }
        }

        public bool this[int index]
        {
            readonly get
            {
                Debug.Assert((uint)index < (uint)_bitCount);

                ulong mask = 1UL << (index % (sizeof(ulong) * 8));
                return (_buffer[index / (sizeof(ulong) * 8)] & mask) is not 0;
            }
            set
            {
                Debug.Assert((uint)index < (uint)_bitCount);

                ulong mask = 1UL << (index % (sizeof(ulong) * 8));
                ref ulong word = ref _buffer[index / (sizeof(ulong) * 8)];
                word = value ? word | mask : word & ~mask;
            }
        }

        /// <summary>Initializes a bit array using the supplied scratch buffer when possible.</summary>
        /// <param name="bitCount">The number of addressable bits.</param>
        /// <param name="stackBuffer">
        /// A scratch buffer whose length must equal <see cref="ScratchBufferSize"/>.
        /// </param>
        /// <param name="initialWordValue">The initial value assigned to each backing word.</param>
        public ValueBitArray(
            int bitCount,
            Span<ulong> stackBuffer,
            ulong initialWordValue = 0)
        {
            Debug.Assert(bitCount >= 0);
            Debug.Assert(stackBuffer.Length == ScratchBufferSize);
            _bitCount = bitCount;

            int requiredUInt64Count =
                bitCount is 0 ? 0 : ((bitCount - 1) / (sizeof(ulong) * 8)) + 1;
            _buffer = requiredUInt64Count <= stackBuffer.Length
                ? stackBuffer.Slice(0, requiredUInt64Count)
                : new ulong[requiredUInt64Count];

            _buffer.Fill(initialWordValue);
        }

        public void Clear() => _buffer.Clear();

        public void IntersectWith(scoped ValueBitArray other)
        {
            Debug.Assert(_bitCount == other._bitCount);

            if (_buffer.Length is 1)
            {
                _buffer[0] &= other._buffer[0];
                return;
            }

            for (int i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] &= other._buffer[i];
            }
        }
    }
}
