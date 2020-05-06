// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

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
                    throw new FormatException("Indefinite-length items not supported under the current conformance level.");
                }

                AdvanceBuffer(1);
                PushDataItem(CborMajorType.Map, null);
                length = null;
            }
            else
            {
                ulong mapSize = ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes);

                if (mapSize > int.MaxValue || 2 * mapSize > (ulong)_buffer.Length)
                {
                    throw new FormatException("Insufficient buffer size for declared definite length in CBOR data item.");
                }

                AdvanceBuffer(1 + additionalBytes);
                PushDataItem(CborMajorType.Map, 2 * (int)mapSize);
                length = (int)mapSize;
            }

            _currentKeyOffset = _bytesRead;
            _currentItemIsKey = true;
            return length;
        }

        public void ReadEndMap()
        {
            if (_remainingDataItems == null)
            {
                CborInitialByte value = PeekInitialByte();

                if (value.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
                {
                    throw new InvalidOperationException("Not at end of indefinite-length map.");
                }

                if (!_currentItemIsKey)
                {
                    throw new FormatException("CBOR map key is missing a value.");
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
            Debug.Assert(_currentKeyOffset != null && _currentItemIsKey);

            (int Offset, int Length) currentKeyRange = (_currentKeyOffset.Value, _bytesRead - _currentKeyOffset.Value);

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

            _currentItemIsKey = false;
        }

        private void HandleMapValueRead()
        {
            Debug.Assert(_currentKeyOffset != null && !_currentItemIsKey);

            _currentKeyOffset = _bytesRead;
            _currentItemIsKey = true;
        }

        private void ValidateSortedKeyEncoding((int Offset, int Length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            if (_previousKeyRange != null)
            {
                (int Offset, int Length) previousKeyRange = _previousKeyRange.Value;

                ReadOnlySpan<byte> originalBuffer = _originalBuffer.Span;
                ReadOnlySpan<byte> previousKeyEncoding = originalBuffer.Slice(previousKeyRange.Offset, previousKeyRange.Length);
                ReadOnlySpan<byte> currentKeyEncoding = originalBuffer.Slice(currentKeyRange.Offset, currentKeyRange.Length);

                int cmp = CborConformanceLevelHelpers.CompareKeyEncodings(previousKeyEncoding, currentKeyEncoding, ConformanceLevel);
                if (cmp > 0)
                {
                    ResetBuffer(currentKeyRange.Offset);
                    throw new FormatException("CBOR map keys are not in sorted encoding order.");
                }
                else if (cmp == 0 && CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
                {
                    ResetBuffer(currentKeyRange.Offset);
                    throw new FormatException("CBOR map contains duplicate keys.");
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
                throw new FormatException("CBOR map contains duplicate keys.");
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
                return _reader._originalBuffer.Span.Slice(range.Offset, range.Length);
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
