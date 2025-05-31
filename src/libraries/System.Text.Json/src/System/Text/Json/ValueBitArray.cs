// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Runtime.CompilerServices;
using BackingType = System.UInt32;

#pragma warning disable SA1121

namespace System.Text.Json
{
    internal struct ValueBitArray
    {
        // Not 32 because it requires special casing like, for example, x<<32 is equal to x.
        private const int MaxInlineBitArraySize = sizeof(BackingType) * 8 - 1;
        private const BackingType One = 1;

        private readonly BitArray? _allocatedBitArray;
        private BackingType _inlineBitArray;
        private readonly byte _inlineBitArrayLength;

        private readonly bool IsInline => _allocatedBitArray == null;

        internal readonly int Count => IsInline ? _inlineBitArrayLength : _allocatedBitArray!.Length;

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
            if (IsInline)
            {
                if ((uint)index >= _inlineBitArrayLength)
                {
                    ThrowHelper.ThrowArgumentException(nameof(index));
                }

                _inlineBitArray = value
                    ? (_inlineBitArray | (One << index))
                    : (_inlineBitArray & ~(One << index));
            }
            else
            {
                AllocatedBitArraySet(index, value);
            }
        }

        internal readonly bool Get(int index)
        {
            if (IsInline)
            {
                if ((uint)index >= _inlineBitArrayLength)
                {
                    ThrowHelper.ThrowArgumentException(nameof(index));
                }

                return (_inlineBitArray & (One << index)) != 0;
            }
            else
            {
                return AllocatedBitArrayGet(index);
            }
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
                : AllocatedBitArrayHasAllSet();
        }

        internal readonly bool HasAllSet(ref readonly ValueBitArray mask)
        {
            if (IsInline != mask.IsInline)
            {
                // TODO resx
                ThrowHelper.ThrowArgumentException(nameof(mask));
            }

            if (IsInline)
            {
                if (_inlineBitArrayLength != mask._inlineBitArrayLength)
                {
                    // TODO resx
                    ThrowHelper.ThrowArgumentException(nameof(mask));
                }

                return (_inlineBitArray & mask._inlineBitArray) == mask._inlineBitArray;
            }
            else
            {
                return AllocatedBitArrayHasAllSet(mask._allocatedBitArray!);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AllocatedBitArraySet(int index, bool value)
        {
            _allocatedBitArray!.Set(index, value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool AllocatedBitArrayGet(int index)
        {
            return _allocatedBitArray![index];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool AllocatedBitArrayHasAllSet()
        {
            return _allocatedBitArray!.HasAllSet();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private readonly bool AllocatedBitArrayHasAllSet(BitArray mask)
        {
            BitArray negatedMask = new BitArray(mask).Not();
            negatedMask.Or(_allocatedBitArray!);
            return negatedMask.HasAllSet();
        }
    }
}
