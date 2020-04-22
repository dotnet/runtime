// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        private KeyEncodingComparer? _keyComparer;

        public uint? ReadStartMap()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.Map);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
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
                PushDataItem(CborMajorType.Map, 2 * (uint)mapSize);
                return (uint)mapSize;
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

            (int offset, int length) currentKeyRange = (_currentKeyOffset.Value, _bytesRead - _currentKeyOffset.Value);

            if (CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                ValidateSortedKeyEncoding(currentKeyRange);
            }
            else if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
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

        private void ValidateSortedKeyEncoding((int offset, int length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            if (_previousKeyRange != null)
            {
                (int offset, int length) previousKeyRange = _previousKeyRange.Value;

                ReadOnlySpan<byte> originalBuffer = _originalBuffer.Span;
                ReadOnlySpan<byte> previousKeyEncoding = originalBuffer.Slice(previousKeyRange.offset, previousKeyRange.length);
                ReadOnlySpan<byte> currentKeyEncoding = originalBuffer.Slice(currentKeyRange.offset, currentKeyRange.length);

                int cmp = CborConformanceLevelHelpers.CompareEncodings(previousKeyEncoding, currentKeyEncoding, ConformanceLevel);
                if (cmp > 0)
                {
                    throw new FormatException("CBOR map keys are not in sorted encoding order.");
                }
                else if (cmp == 0 && CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
                {
                    throw new FormatException("CBOR map contains duplicate keys.");
                }
            }

            _previousKeyRange = currentKeyRange;
        }

        private void ValidateKeyUniqueness((int offset, int length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            SortedSet<(int offset, int length)> previousKeys = GetPreviousKeyRanges();

            if (!previousKeys.Add(currentKeyRange))
            {
                throw new FormatException("CBOR map contain duplicate keys.");
            }
        }

        private SortedSet<(int offset, int length)> GetPreviousKeyRanges()
        {
            if (_previousKeyRanges == null)
            {
                _keyComparer ??= new KeyEncodingComparer(this);
                _previousKeyRanges = new SortedSet<(int offset, int length)>(_keyComparer);
            }

            return _previousKeyRanges;
        }

        private ReadOnlySpan<byte> GetKeyEncoding((int offset, int length) range)
        {
            return _originalBuffer.Span.Slice(range.offset, range.length);
        }

        private class KeyEncodingComparer : IComparer<(int, int)>
        {
            private readonly CborReader _reader;

            public KeyEncodingComparer(CborReader reader)
            {
                _reader = reader;
            }

            public int Compare((int, int) x, (int, int) y)
            {
                return CborConformanceLevelHelpers.CompareEncodings(_reader.GetKeyEncoding(x), _reader.GetKeyEncoding(y), _reader.ConformanceLevel);
            }
        }
    }
}
