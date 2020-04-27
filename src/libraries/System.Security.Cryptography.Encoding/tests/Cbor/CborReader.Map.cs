// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
                if (CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException("Indefinite-length items not support under the current conformance level.");
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

            (int Offset, int Length) currentKeyRange = (_currentKeyOffset.Value, _bytesRead - _currentKeyOffset.Value);

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

        private void ValidateSortedKeyEncoding((int Offset, int Length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            if (_previousKeyRange != null)
            {
                (int Offset, int Length) previousKeyRange = _previousKeyRange.Value;

                ReadOnlySpan<byte> originalBuffer = _originalBuffer.Span;
                ReadOnlySpan<byte> previousKeyEncoding = originalBuffer.Slice(previousKeyRange.Offset, previousKeyRange.Length);
                ReadOnlySpan<byte> currentKeyEncoding = originalBuffer.Slice(currentKeyRange.Offset, currentKeyRange.Length);

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

        private void ValidateKeyUniqueness((int Offset, int Length) currentKeyRange)
        {
            Debug.Assert(_currentKeyOffset != null);

            HashSet<(int Offset, int Length)> previousKeys = GetPreviousKeyRanges();

            if (!previousKeys.Add(currentKeyRange))
            {
                throw new FormatException("CBOR map contains duplicate keys.");
            }
        }

        private HashSet<(int Offset, int Length)> GetPreviousKeyRanges()
        {
            if (_previousKeyRanges == null)
            {
                _keyComparer ??= new KeyEncodingComparer(this);
                _previousKeyRanges = new HashSet<(int Offset, int Length)>(_keyComparer);
            }

            return _previousKeyRanges;
        }

        private ReadOnlySpan<byte> GetKeyEncoding((int Offset, int Length) range)
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
                return System.Marvin.ComputeHash32(_reader.GetKeyEncoding(value), System.Marvin.DefaultSeed);
            }

            public bool Equals((int Offset, int Length) x, (int Offset, int Length) y)
            {
                return _reader.GetKeyEncoding(x).SequenceEqual(_reader.GetKeyEncoding(y));
            }
        }
    }
}
