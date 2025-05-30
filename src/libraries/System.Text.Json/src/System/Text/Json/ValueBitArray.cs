// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;

using BackingType = System.UInt32;

#pragma warning disable SA1121

namespace System.Text.Json
{
    internal struct ValueBitArray
    {
        // Not 32 because it requires special casing like x<<32 is equal to x.
        private const int MaxInlineBitArraySize = sizeof(BackingType) * 8 - 1;
        private const BackingType One = 1;

        private BackingType _inlineBitArray;
        private readonly BitArray? _allocatedBitArray;
        private readonly byte _inlineBitArrayLength;

        private readonly bool IsInline => _allocatedBitArray == null;

        public readonly int Count =>
            IsInline ? _inlineBitArrayLength : _allocatedBitArray!.Length;

        internal ValueBitArray(int length)
        {
            if (length <= MaxInlineBitArraySize)
            {
                _inlineBitArray = 0;
                _inlineBitArrayLength = (byte)length;
                _allocatedBitArray = null;
            }
            else
            {
                _inlineBitArray = 0;
                _inlineBitArrayLength = 0;
                _allocatedBitArray = new BitArray(length);
            }
        }

        internal void Set(int index, bool value)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (IsInline)
            {
                _inlineBitArray = value
                    ? (_inlineBitArray | (One << index))
                    : (_inlineBitArray & ~(One << index));
            }
            else
            {
                _allocatedBitArray![index] = value;
            }
        }

        internal readonly bool Get(int index)
        {
            if ((uint)index >= (uint)Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return IsInline
                ? (_inlineBitArray & (One << index)) != 0
                : _allocatedBitArray![index];
        }

        internal bool this[int index]
        {
            readonly get => Get(index);
            set => Set(index, value);
        }

        internal readonly bool HasAllSet()
        {
            return IsInline
                ? (One << _inlineBitArrayLength) - 1 == _inlineBitArray
                : _allocatedBitArray!.HasAllSet();
        }

        internal readonly bool HasAllSet(ref readonly ValueBitArray mask)
        {
            if (IsInline != mask.IsInline)
            {
                // TODO resx
                throw new ArgumentException("Cannot compare bit arrays of different lengths.", nameof(mask));
            }

            if (IsInline)
            {
                if (_inlineBitArrayLength != mask._inlineBitArrayLength)
                {
                    // TODO resx
                    throw new ArgumentException("Cannot compare bit arrays of different lengths.", nameof(mask));
                }

                return (_inlineBitArray & mask._inlineBitArray) == mask._inlineBitArray;
            }
            else
            {
                BitArray negatedMask = new BitArray(mask._allocatedBitArray!).Not();
                negatedMask.Or(_allocatedBitArray!);
                return negatedMask.HasAllSet();
            }
        }
    }
}
