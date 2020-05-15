// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        private KeyEncodingComparer? _keyEncodingComparer;
        private Stack<HashSet<(int Offset, int Length)>>? _pooledKeyEncodingRangeSets;

        public int? ReadStartMap()
        {
            int? length;
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Map);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_RequiresDefiniteLengthItems, ConformanceLevel));
                }

                AdvanceBuffer(1);
                PushDataItem(CborMajorType.Map, null);
                length = null;
            }
            else
            {
                ReadOnlySpan<byte> buffer = GetRemainingBytes();

                ulong mapSize = ReadUnsignedInteger(buffer, header, out int additionalBytes);

                if (mapSize > int.MaxValue || 2 * mapSize > (ulong)buffer.Length)
                {
                    throw new FormatException(SR.Cbor_Reader_DefiniteLengthExceedsBufferSize);
                }

                AdvanceBuffer(1 + additionalBytes);
                PushDataItem(CborMajorType.Map, 2 * (int)mapSize);
                length = (int)mapSize;
            }

            _currentKeyOffset = _offset;
            return length;
        }

        public void ReadEndMap()
        {
            if (_definiteLength is null)
            {
                ValidateNextByteIsBreakByte();

                if (_itemsWritten % 2 != 0)
                {
                    throw new FormatException(SR.Cbor_Reader_InvalidCbor_KeyMissingValue);
                }

                PopDataItem(expectedType: CborMajorType.Map);
                AdvanceDataItemCounters();
                AdvanceBuffer(1);
            }
            else
            {
                PopDataItem(expectedType: CborMajorType.Map);
                AdvanceDataItemCounters();
            }
        }

        //
        // Map decoding conformance
        //

        private void HandleMapKeyRead()
        {
            Debug.Assert(_currentKeyOffset != null && _itemsWritten % 2 == 0);

            (int Offset, int Length) currentKeyRange = (_currentKeyOffset.Value, _offset - _currentKeyOffset.Value);

            if (_isConformanceLevelCheckEnabled)
            {
                if (CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
                {
                    ValidateSortedKeyEncoding(currentKeyRange);
                }
                else if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
                {
                    ValidateKeyUniqueness(currentKeyRange);
                }
            }
        }

        private void HandleMapValueRead()
        {
            Debug.Assert(_currentKeyOffset != null && _itemsWritten % 2 != 0);

            _currentKeyOffset = _offset;
        }

        private void ValidateSortedKeyEncoding((int Offset, int Length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            if (_previousKeyRange != null)
            {
                (int Offset, int Length) previousKeyRange = _previousKeyRange.Value;

                ReadOnlySpan<byte> buffer = _buffer.Span;
                ReadOnlySpan<byte> previousKeyEncoding = buffer.Slice(previousKeyRange.Offset, previousKeyRange.Length);
                ReadOnlySpan<byte> currentKeyEncoding = buffer.Slice(currentKeyRange.Offset, currentKeyRange.Length);

                int cmp = CborConformanceLevelHelpers.CompareKeyEncodings(previousKeyEncoding, currentKeyEncoding, ConformanceLevel);
                if (cmp > 0)
                {
                    ResetBuffer(currentKeyRange.Offset);
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_KeysNotInSortedOrder, ConformanceLevel));
                }
                else if (cmp == 0 && CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
                {
                    ResetBuffer(currentKeyRange.Offset);
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_ContainsDuplicateKeys, ConformanceLevel));
                }
            }

            _previousKeyRange = currentKeyRange;
        }

        private void ValidateKeyUniqueness((int Offset, int Length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            HashSet<(int Offset, int Length)> previousKeys = GetKeyEncodingRanges();

            if (!previousKeys.Add(currentKeyRange))
            {
                ResetBuffer(currentKeyRange.Offset);
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_ContainsDuplicateKeys, ConformanceLevel));
            }
        }

        private HashSet<(int Offset, int Length)> GetKeyEncodingRanges()
        {
            if (_keyEncodingRanges != null)
            {
                return _keyEncodingRanges;
            }

            if (_pooledKeyEncodingRangeSets != null &&
                _pooledKeyEncodingRangeSets.TryPop(out HashSet<(int Offset, int Length)>? result))
            {
                result.Clear();
                return _keyEncodingRanges = result;
            }

            _keyEncodingComparer ??= new KeyEncodingComparer(this);
            return _keyEncodingRanges = new HashSet<(int Offset, int Length)>(_keyEncodingComparer);
        }

        private void ReturnKeyEncodingRangeAllocation()
        {
            if (_keyEncodingRanges != null)
            {
                _pooledKeyEncodingRangeSets ??= new Stack<HashSet<(int Offset, int Length)>>();
                _pooledKeyEncodingRangeSets.Push(_keyEncodingRanges);
                _keyEncodingRanges = null;
            }
        }

        private class KeyEncodingComparer : IEqualityComparer<(int Offset, int Length)>
        {
            private readonly CborReader _reader;

            public KeyEncodingComparer(CborReader reader)
            {
                _reader = reader;
            }

            private ReadOnlySpan<byte> GetKeyEncoding((int Offset, int Length) range)
            {
                return _reader._buffer.Span.Slice(range.Offset, range.Length);
            }

            public int GetHashCode((int Offset, int Length) value)
            {
                return CborConformanceLevelHelpers.GetKeyEncodingHashCode(GetKeyEncoding(value));
            }

            public bool Equals((int Offset, int Length) x, (int Offset, int Length) y)
            {
                return CborConformanceLevelHelpers.AreEqualKeyEncodings(GetKeyEncoding(x), GetKeyEncoding(y));
            }
        }
    }
}
