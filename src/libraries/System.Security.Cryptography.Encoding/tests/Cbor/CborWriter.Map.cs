// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter
    {
        private KeyEncodingComparer? _comparer;

        public void WriteStartMap(int definiteLength)
        {
            if (definiteLength < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength), "must be non-negative integer.");
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            PushDataItem(CborMajorType.Map, 2 * (uint)definiteLength);
        }

        public void WriteEndMap()
        {
            if (_currentValueOffset != null)
            {
                throw new InvalidOperationException("CBOR Map types require an even number of key/value combinations");
            }

            bool isDefiniteLengthMap = _remainingDataItems.HasValue;

            PopDataItem(CborMajorType.Map);

            if (!isDefiniteLengthMap)
            {
                // append break byte
                EnsureWriteCapacity(1);
                _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
            }

            AdvanceDataItemCounters();
        }

        public void WriteStartMapIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Map, expectedNestedItems: null);
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        //
        // Map encoding conformance
        //

        private void HandleKeyWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset == null);

            _currentValueOffset = _offset;

            if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
            {
                SortedSet<(int offset, int keyLength, int keyValueLength)> ranges = GetKeyValueEncodingRanges();

                (int offset, int keyLength, int valueLength) currentKeyRange =
                    (_currentKeyOffset.Value,
                     _currentValueOffset.Value - _currentKeyOffset.Value,
                     0);

                if (ranges.Contains(currentKeyRange))
                {
                    // TODO: check if rollback is necessary here
                    throw new InvalidOperationException("Duplicate key encoding in CBOR map.");
                }
            }
        }

        private void HandleValueWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset != null);

            if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel) || CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                SortedSet<(int offset, int keyLength, int keyValueLength)> ranges = GetKeyValueEncodingRanges();

                (int offset, int keyLength, int keyValueLength) currentKeyRange =
                    (_currentKeyOffset.Value,
                     _currentValueOffset.Value - _currentKeyOffset.Value,
                     _offset - _currentKeyOffset.Value);

                ranges.Add(currentKeyRange);
            }

            // reset state
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        private void SortKeyValuePairEncodings()
        {
            if (_keyValueEncodingRanges == null)
            {
                return;
            }

            int totalMapPayloadEncodingLength = _offset - _frameOffset;
            byte[] tempBuffer = s_bufferPool.Rent(totalMapPayloadEncodingLength);
            Span<byte> tmpSpan = tempBuffer.AsSpan(0, totalMapPayloadEncodingLength);

            // copy sorted ranges to temporary buffer
            Span<byte> s = tmpSpan;
            foreach((int, int, int) range in _keyValueEncodingRanges)
            {
                ReadOnlySpan<byte> kvEnc = GetKeyValueEncoding(range);
                kvEnc.CopyTo(s);
                s = s.Slice(kvEnc.Length);
            }
            Debug.Assert(s.IsEmpty);

            // now copy back to the original buffer segment
            tmpSpan.CopyTo(_buffer.AsSpan(_frameOffset, totalMapPayloadEncodingLength));

            s_bufferPool.Return(tempBuffer, clearArray: true);
        }

        private ReadOnlySpan<byte> GetKeyEncoding((int offset, int keyLength, int valueLength) keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.offset, keyValueRange.keyLength);
        }

        private ReadOnlySpan<byte> GetKeyValueEncoding((int offset, int keyLength, int keyValueLength) keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.offset, keyValueRange.keyValueLength);
        }

        private class KeyEncodingComparer : IComparer<(int, int, int)>
        {
            private readonly CborWriter _writer;

            public KeyEncodingComparer(CborWriter writer)
            {
                _writer = writer;
            }

            public int Compare((int, int, int) x, (int, int, int) y)
            {
                return CborConformanceLevelHelpers.CompareEncodings(_writer.GetKeyEncoding(x), _writer.GetKeyEncoding(y), _writer.ConformanceLevel);
            }
        }

        private SortedSet<(int offset, int keyLength, int keyValueLength)> GetKeyValueEncodingRanges()
        {
            // TODO consider pooling set allocations?

            if (_keyValueEncodingRanges == null)
            {
                _comparer ??= new KeyEncodingComparer(this);
                return _keyValueEncodingRanges = new SortedSet<(int offset, int keyLength, int keyValueLength)>(_comparer);
            }

            return _keyValueEncodingRanges;
        }
    }
}
