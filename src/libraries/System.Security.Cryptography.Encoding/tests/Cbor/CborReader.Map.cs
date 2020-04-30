// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        private KeyEncodingComparer? _keyEncodingComparer;
        private Stack<HashSet<(int Offset, int Length)>>? _pooledKeyEncodingRangeSets;

        public int? ReadStartMap()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Map);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException("Indefinite-length items not supported under the current conformance level.");
                }

                AdvanceBuffer(1);
                PushDataItem(CborMajorType.Map, null);
                return null;
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
                return (int)mapSize;
            }
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

                if (!_curentItemIsKey)
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

        private void HandleMapKeyAdded()
        {
            Debug.Assert(_currentKeyOffset != null && _curentItemIsKey);

            (int Offset, int Length) currentKeyRange = (_currentKeyOffset.Value, _bytesRead - _currentKeyOffset.Value);

            if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                ValidateSortedKeyEncoding(currentKeyRange);
            }
            else if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
            {
                ValidateKeyUniqueness(currentKeyRange);
            }

            _curentItemIsKey = false;
        }

        private void HandleMapValueAdded()
        {
            Debug.Assert(_currentKeyOffset != null && !_curentItemIsKey);

            _currentKeyOffset = _bytesRead;
            _curentItemIsKey = true;
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

            HashSet<(int Offset, int Length)> previousKeys = GetPreviousKeyRanges();

            if (!previousKeys.Add(currentKeyRange))
            {
                ResetBuffer(currentKeyRange.Offset);
                throw new FormatException("CBOR map contains duplicate keys.");
            }
        }

        private HashSet<(int Offset, int Length)> GetPreviousKeyRanges()
        {
            return _previousKeyRanges ??= RequestKeyEncodingRangeSet();

            HashSet<(int Offset, int Length)> RequestKeyEncodingRangeSet()
            {
                if (_pooledKeyEncodingRangeSets != null)
                {
                    HashSet<(int Offset, int Length)>? result;

                    lock (_pooledKeyEncodingRangeSets)
                    {
                        _pooledKeyEncodingRangeSets.TryPop(out result);
                    }

                    if (result != null)
                    {
                        result.Clear();
                        return result;
                    }
                }

                _keyEncodingComparer ??= new KeyEncodingComparer(this);
                return new HashSet<(int Offset, int Length)>(_keyEncodingComparer);
            }
        }

        private void ReturnKeyEncodingRangeSet()
        {
            HashSet<(int Offset, int Length)>? set = Interlocked.Exchange(ref _previousKeyRanges, null);

            if (set != null)
            {
                _pooledKeyEncodingRangeSets ??= new Stack<HashSet<(int Offset, int Length)>>();

                lock (_pooledKeyEncodingRangeSets)
                {
                    _pooledKeyEncodingRangeSets.Push(set);
                }
            }
        }

        private ReadOnlySpan<byte> GetKeyEncoding(in (int Offset, int Length) range)
        {
            return _originalBuffer.Span.Slice(range.Offset, range.Length);
        }

        private class KeyEncodingComparer : IEqualityComparer<(int Offset, int Length)>
        {
            private readonly CborReader _reader;

            public KeyEncodingComparer(CborReader reader)
            {
                _reader = reader;
            }

            public int GetHashCode((int Offset, int Length) value)
            {
                return CborConformanceLevelHelpers.GetKeyEncodingHashCode(_reader.GetKeyEncoding(in value));
            }

            public bool Equals((int Offset, int Length) x, (int Offset, int Length) y)
            {
                return CborConformanceLevelHelpers.AreEqualKeyEncodings(_reader.GetKeyEncoding(in x), _reader.GetKeyEncoding(in y));
            }
        }
    }
}
