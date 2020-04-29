// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        private static readonly ArrayPool<KeyValueEncodingRange> s_rangeBuffers = ArrayPool<KeyValueEncodingRange>.Create();

        private KeyEncodingComparer? _keyValueComparer;

        public void WriteStartMap(int definiteLength)
        {
            if (definiteLength < 0 || definiteLength > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength));
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            PushDataItem(CborMajorType.Map, definiteLength: checked(2 * definiteLength));
        }

        public void WriteStartMap()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Map, definiteLength: null);
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        public void WriteEndMap()
        {
            if (_itemsWritten % 2 == 1)
            {
                throw new InvalidOperationException("CBOR Map types require an even number of key/value combinations");
            }

            PopDataItem(CborMajorType.Map);
            AdvanceDataItemCounters();
        }

        //
        // Map encoding conformance
        //

        private void HandleKeyWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset == null);

            int currentValueOffset = _offset;

            if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
            {
                HashSet<KeyValueEncodingRange> ranges = GetKeyValueEncodingRanges();

                var currentKeyRange = new KeyValueEncodingRange(
                    offset: _currentKeyOffset.Value,
                    keyLength: currentValueOffset - _currentKeyOffset.Value,
                    totalLength: 0 // totalLength not known, but is not significant w.r.t. key equality semantics
                );

                if (ranges.Contains(currentKeyRange))
                {
                    // reset writer state to what existed before the offending key write
                    _buffer.AsSpan(currentKeyRange.Offset, _offset).Fill(0);
                    _offset = currentKeyRange.Offset;

                    throw new InvalidOperationException("Duplicate key encoding in CBOR map.");
                }
            }

            _currentValueOffset = currentValueOffset;
        }

        private void HandleValueWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset != null);

            if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel) || CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                HashSet<KeyValueEncodingRange> ranges = GetKeyValueEncodingRanges();

                var currentKeyRange = new KeyValueEncodingRange(
                    offset: _currentKeyOffset.Value,
                    keyLength: _currentValueOffset.Value - _currentKeyOffset.Value,
                    totalLength: _offset - _currentKeyOffset.Value);

                Debug.Assert(!(CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel) && ranges.Contains(currentKeyRange)));
                ranges.Add(currentKeyRange);
            }

            // reset state
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        private void SortKeyValuePairEncodings()
        {
            Debug.Assert(_keyValueEncodingRanges != null);
            Debug.Assert(_keyValueComparer != null);

            // copy the key/value ranges to a temporary buffer and sort in-place
            KeyValueEncodingRange[] rangeBuffer = s_rangeBuffers.Rent(_keyValueEncodingRanges.Count);
            _keyValueEncodingRanges.CopyTo(rangeBuffer);
            Span<KeyValueEncodingRange> rangeSpan = rangeBuffer.AsSpan(0, _keyValueEncodingRanges.Count);
            rangeSpan.Sort(_keyValueComparer);

            // copy sorted ranges to temporary buffer
            int totalMapPayloadEncodingLength = _offset - _frameOffset;
            byte[] tempBuffer = s_bufferPool.Rent(totalMapPayloadEncodingLength);
            Span<byte> tmpSpan = tempBuffer.AsSpan(0, totalMapPayloadEncodingLength);

            Span<byte> s = tmpSpan;
            foreach (KeyValueEncodingRange range in rangeSpan)
            {
                ReadOnlySpan<byte> kvEnc = GetKeyValueEncoding(in range);
                kvEnc.CopyTo(s);
                s = s.Slice(kvEnc.Length);
            }
            Debug.Assert(s.IsEmpty);

            // now copy back to the original buffer segment
            tmpSpan.CopyTo(_buffer.AsSpan(_frameOffset, totalMapPayloadEncodingLength));

            s_bufferPool.Return(tempBuffer, clearArray: true);
            s_rangeBuffers.Return(rangeBuffer, clearArray: false);
        }

        private ReadOnlySpan<byte> GetKeyEncoding(in KeyValueEncodingRange keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.Offset, keyValueRange.KeyLength);
        }

        private ReadOnlySpan<byte> GetKeyValueEncoding(in KeyValueEncodingRange keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.Offset, keyValueRange.TotalLength);
        }

        private HashSet<KeyValueEncodingRange> GetKeyValueEncodingRanges()
        {
            // TODO consider pooling set allocations?

            if (_keyValueEncodingRanges == null)
            {
                _keyValueComparer ??= new KeyEncodingComparer(this);
                return _keyValueEncodingRanges = new HashSet<KeyValueEncodingRange>(_keyValueComparer);
            }

            return _keyValueEncodingRanges;
        }

        private readonly struct KeyValueEncodingRange
        {
            public KeyValueEncodingRange(int offset, int keyLength, int totalLength)
            {
                Offset = offset;
                KeyLength = keyLength;
                TotalLength = totalLength;
            }

            public int Offset { get; }
            public int KeyLength { get; }
            public int TotalLength { get; }
        }

        private class KeyEncodingComparer : IComparer<KeyValueEncodingRange>,
                                            IEqualityComparer<KeyValueEncodingRange>
        {
            private readonly CborWriter _writer;

            public KeyEncodingComparer(CborWriter writer)
            {
                _writer = writer;
            }

            public int GetHashCode(KeyValueEncodingRange value)
            {
                return CborConformanceLevelHelpers.GetKeyEncodingHashCode(_writer.GetKeyEncoding(in value));
            }

            public bool Equals(KeyValueEncodingRange x, KeyValueEncodingRange y)
            {
                return CborConformanceLevelHelpers.AreEqualKeyEncodings(_writer.GetKeyEncoding(in x), _writer.GetKeyEncoding(in y));
            }

            public int Compare(KeyValueEncodingRange x, KeyValueEncodingRange y)
            {
                return CborConformanceLevelHelpers.CompareKeyEncodings(_writer.GetKeyEncoding(in x), _writer.GetKeyEncoding(in y), _writer.ConformanceLevel);
            }
        }
    }
}
