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
            if (definiteLength < 0 || definiteLength > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength));
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            PushDataItem(CborMajorType.Map, definiteLength: checked(2 * definiteLength));
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

        public void WriteStartMapIndefiniteLength()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Map, definiteLength: null);
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
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
                SortedSet<(int Offset, int KeyLength, int TotalLength)> ranges = GetKeyValueEncodingRanges();

                (int Offset, int KeyLength, int TotalLength) currentKeyRange =
                    (_currentKeyOffset.Value,
                     currentValueOffset - _currentKeyOffset.Value,
                     0); 

                if (ranges.Contains(currentKeyRange))
                {
                    // reset writer state to before the offending key write
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
                SortedSet<(int Offset, int KeyLength, int TotalLength)> ranges = GetKeyValueEncodingRanges();

                (int Offset, int KeyLength, int TotalLength) currentKeyRange =
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
            foreach((int Offset, int KeyLength, int TotalLength) range in _keyValueEncodingRanges)
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

        private ReadOnlySpan<byte> GetKeyEncoding((int Offset, int KeyLength, int TotalLength) keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.Offset, keyValueRange.KeyLength);
        }

        private ReadOnlySpan<byte> GetKeyValueEncoding((int Offset, int KeyLength, int TotalLength) keyValueRange)
        {
            return _buffer.AsSpan(keyValueRange.Offset, keyValueRange.TotalLength);
        }

        private class KeyEncodingComparer : IComparer<(int Offset, int KeyLength, int TotalLength)>
        {
            private readonly CborWriter _writer;

            public KeyEncodingComparer(CborWriter writer)
            {
                _writer = writer;
            }

            public int Compare((int Offset, int KeyLength, int TotalLength) x, (int Offset, int KeyLength, int TotalLength) y)
            {
                return CborConformanceLevelHelpers.CompareEncodings(_writer.GetKeyEncoding(x), _writer.GetKeyEncoding(y), _writer.ConformanceLevel);
            }
        }

        private SortedSet<(int Offset, int KeyLength, int TotalLength)> GetKeyValueEncodingRanges()
        {
            // TODO consider pooling set allocations?

            if (_keyValueEncodingRanges == null)
            {
                _comparer ??= new KeyEncodingComparer(this);
                return _keyValueEncodingRanges = new SortedSet<(int Offset, int KeyLength, int TotalLength)>(_comparer);
            }

            return _keyValueEncodingRanges;
        }
    }
}
