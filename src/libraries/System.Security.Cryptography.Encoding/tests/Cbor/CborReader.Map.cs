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
        private Stack<HashSet<(int Offset, int Length)>>? _pooledKeyEncodingRangeAllocations;

        /// <summary>
        ///   Reads the next data item as the start of a map (major type 5).
        /// </summary>
        /// <returns>
        ///   The number of key-value pairs in a definite-length map, or <see langword="null" /> if the map is indefinite-length.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        ///   the next data item does not have the correct major type.
        /// </exception>
        /// <exception cref="FormatException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data. -or-
        ///   the next value uses a CBOR encoding that is not valid under the current conformance level.
        /// </exception>
        /// <remarks>
        ///   Map contents are consumed as if they were arrays twice the length of the map's declared size.
        ///   For instance, a map of size <c>1</c> containing a key of type int with a value of type string
        ///   must be consumed by successive calls to <see cref="ReadInt32"/> and <see cref="ReadTextString"/>.
        ///   It is up to the caller to keep track of whether the next value is a key or a value.
        ///
        ///   Fundamentally, this is a technical restriction stemming from the fact that CBOR allows keys of arbitrary type,
        ///   for instance a map can contain keys that are maps themselves.
        /// </remarks>
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

                int mapSize = DecodeDefiniteLength(header, buffer, out int bytesRead);

                if (2 * (ulong)mapSize > (ulong)(buffer.Length - bytesRead))
                {
                    throw new FormatException(SR.Cbor_Reader_DefiniteLengthExceedsBufferSize);
                }

                AdvanceBuffer(bytesRead);
                PushDataItem(CborMajorType.Map, 2 * mapSize);
                length = (int)mapSize;
            }

            _currentKeyOffset = _offset;
            return length;
        }


        /// <summary>
        ///   Reads the end of a map (major type 5).
        /// </summary>
        /// <exception cref="InvalidOperationException">
        ///   the current context is not a map. -or-
        ///   the reader is not at the end of the map
        /// </exception>
        /// <exception cref="FormatException">
        ///   the next value has an invalid CBOR encoding. -or-
        ///   there was an unexpected end of CBOR encoding data. -or-
        ///   the next value uses a CBOR encoding that is not valid under the current conformance level.
        /// </exception>
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

        // conformance book-keeping after a key data item has been read
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
                    // NB uniquess is validated separately in conformance levels requiring sorted keys
                    ValidateKeyUniqueness(currentKeyRange);
                }
            }
        }

        // conformance book-keeping after a value data item has been read
        private void HandleMapValueRead()
        {
            Debug.Assert(_currentKeyOffset != null && _itemsWritten % 2 != 0);

            _currentKeyOffset = _offset;
        }

        private void ValidateSortedKeyEncoding((int Offset, int Length) currentKeyEncodingRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            if (_previousKeyEncodingRange != null)
            {
                (int Offset, int Length) previousKeyEncodingRange = _previousKeyEncodingRange.Value;

                ReadOnlySpan<byte> buffer = _data.Span;
                ReadOnlySpan<byte> previousKeyEncoding = buffer.Slice(previousKeyEncodingRange.Offset, previousKeyEncodingRange.Length);
                ReadOnlySpan<byte> currentKeyEncoding = buffer.Slice(currentKeyEncodingRange.Offset, currentKeyEncodingRange.Length);

                int cmp = CborConformanceLevelHelpers.CompareKeyEncodings(previousKeyEncoding, currentKeyEncoding, ConformanceLevel);
                if (cmp > 0)
                {
                    ResetBuffer(currentKeyEncodingRange.Offset);
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_KeysNotInSortedOrder, ConformanceLevel));
                }
                else if (cmp == 0 && CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
                {
                    ResetBuffer(currentKeyEncodingRange.Offset);
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_ContainsDuplicateKeys, ConformanceLevel));
                }
            }

            _previousKeyEncodingRange = currentKeyEncodingRange;
        }

        private void ValidateKeyUniqueness((int Offset, int Length) currentKeyEncodingRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            HashSet<(int Offset, int Length)> keyEncodingRanges = GetKeyEncodingRanges();

            if (!keyEncodingRanges.Add(currentKeyEncodingRange))
            {
                ResetBuffer(currentKeyEncodingRange.Offset);
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_ContainsDuplicateKeys, ConformanceLevel));
            }
        }

        private HashSet<(int Offset, int Length)> GetKeyEncodingRanges()
        {
            if (_keyEncodingRanges != null)
            {
                return _keyEncodingRanges;
            }

            if (_pooledKeyEncodingRangeAllocations != null &&
                _pooledKeyEncodingRangeAllocations.TryPop(out HashSet<(int Offset, int Length)>? result))
            {
                result.Clear();
                return _keyEncodingRanges = result;
            }

            _keyEncodingComparer ??= new KeyEncodingComparer(this);
            return _keyEncodingRanges = new HashSet<(int Offset, int Length)>(_keyEncodingComparer);
        }

        private void ReturnKeyEncodingRangeAllocation(HashSet<(int Offset, int Length)>? allocation)
        {
            if (allocation != null)
            {
                _pooledKeyEncodingRangeAllocations ??= new Stack<HashSet<(int Offset, int Length)>>();
                _pooledKeyEncodingRangeAllocations.Push(allocation);
            }
        }

        // Comparing buffer slices up to their binary content
        private class KeyEncodingComparer : IEqualityComparer<(int Offset, int Length)>
        {
            private readonly CborReader _reader;

            public KeyEncodingComparer(CborReader reader)
            {
                _reader = reader;
            }

            private ReadOnlySpan<byte> GetKeyEncoding((int Offset, int Length) range)
            {
                return _reader._data.Span.Slice(range.Offset, range.Length);
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
