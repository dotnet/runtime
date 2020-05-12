// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        private KeyEncodingComparer? _keyEncodingComparer;
        private Stack<HashSet<(int Offset, int Length)>>? _pooledKeyEncodingRangeSets;
        private Stack<List<KeyValueEncodingRange>>? _pooledKeyValueEncodingRangeLists;

        public void WriteStartMap(int definiteLength)
        {
            if (definiteLength < 0 || definiteLength > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength));
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            PushDataItem(CborMajorType.Map, definiteLength: checked(2 * definiteLength));
            _currentKeyOffset = _offset;
        }

        public void WriteStartMap()
        {
            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Map, definiteLength: null);
            _currentKeyOffset = _offset;
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

            if (CborConformanceLevelHelpers.RequiresUniqueKeys(ConformanceLevel))
            {
                HashSet<(int Offset, int Length)> keyEncodingRanges = GetKeyEncodingRanges();

                (int Offset, int Length) currentKey = (_currentKeyOffset.Value, _offset - _currentKeyOffset.Value);

                if (!keyEncodingRanges.Add(currentKey))
                {
                    // reset writer state to right before the offending key write
                    _buffer.AsSpan(currentKey.Offset, _offset).Fill(0);
                    _offset = currentKey.Offset;

                    throw new InvalidOperationException("Duplicate key encoding in CBOR map.");
                }
            }

            // record the value buffer offset
            _currentValueOffset = _offset;
        }

        private void HandleValueWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset != null);

            if (CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                List<KeyValueEncodingRange> keyValueRanges = GetKeyValueEncodingRanges();

                var currentKeyValueRange = new KeyValueEncodingRange(
                    offset: _currentKeyOffset.Value,
                    keyLength: _currentValueOffset.Value - _currentKeyOffset.Value,
                    totalLength: _offset - _currentKeyOffset.Value);

                // Check that the keys are written in sorted order.
                // Once invalidated, declare that the map requires sorting,
                // which will prompt a sorting of the encodings once map writes have completed.
                if (!_keysRequireSorting && keyValueRanges.Count > 0)
                {
                    KeyEncodingComparer comparer = GetKeyEncodingComparer();
                    KeyValueEncodingRange previousKeyValueRange = keyValueRanges[keyValueRanges.Count - 1];
                    _keysRequireSorting = comparer.Compare(previousKeyValueRange, currentKeyValueRange) > 0;
                }

                keyValueRanges.Add(currentKeyValueRange);
            }

            // update offset state to the next key
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        private void CompleteMapWrite()
        {
            if (_keysRequireSorting)
            {
                Debug.Assert(_keyValueEncodingRanges != null);

                // sort the key/value ranges in-place
                _keyValueEncodingRanges.Sort(GetKeyEncodingComparer());

                // copy sorted ranges to temporary buffer
                int totalMapPayloadEncodingLength = _offset - _frameOffset;
                Span<byte> source = _buffer.AsSpan();

                byte[] tempBuffer = s_bufferPool.Rent(totalMapPayloadEncodingLength);
                Span<byte> tmpSpan = tempBuffer.AsSpan(0, totalMapPayloadEncodingLength);

                Span<byte> s = tmpSpan;
                foreach (KeyValueEncodingRange range in _keyValueEncodingRanges)
                {
                    ReadOnlySpan<byte> keyValueEncoding = source.Slice(range.Offset, range.TotalLength);
                    keyValueEncoding.CopyTo(s);
                    s = s.Slice(keyValueEncoding.Length);
                }
                Debug.Assert(s.IsEmpty);

                // now copy back to the original buffer segment & clean up
                tmpSpan.CopyTo(source.Slice(_frameOffset, totalMapPayloadEncodingLength));
                s_bufferPool.Return(tempBuffer, clearArray: true);
            }

            ReturnKeyEncodingRangeAllocation();
            ReturnKeyValueEncodingRangeAllocation();
        }

        // Gets or initializes a hashset containing all key encoding ranges for the current CBOR map context
        // Equality of the HashSet is determined up to key encoding equality.
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

            return _keyEncodingRanges = new HashSet<(int Offset, int Length)>(GetKeyEncodingComparer());
        }


        // Gets or initializes a list containing all key/value encoding ranges for the current CBOR map context
        private void ReturnKeyEncodingRangeAllocation()
        {
            if (_keyEncodingRanges != null)
            {
                _pooledKeyEncodingRangeSets ??= new Stack<HashSet<(int Offset, int Length)>>();
                _pooledKeyEncodingRangeSets.Push(_keyEncodingRanges);
                _keyEncodingRanges = null;
            }
        }

        private List<KeyValueEncodingRange> GetKeyValueEncodingRanges()
        {
            if (_keyValueEncodingRanges != null)
            {
                return _keyValueEncodingRanges;
            }

            if (_pooledKeyValueEncodingRangeLists != null &&
                _pooledKeyValueEncodingRangeLists.TryPop(out List<KeyValueEncodingRange>? result))
            {
                result.Clear();
                return _keyValueEncodingRanges = result;
            }

            return _keyValueEncodingRanges = new List<KeyValueEncodingRange>();
        }

        private void ReturnKeyValueEncodingRangeAllocation()
        {
            if (_keyValueEncodingRanges != null)
            {
                _pooledKeyValueEncodingRangeLists ??= new Stack<List<KeyValueEncodingRange>>();
                _pooledKeyValueEncodingRangeLists.Push(_keyValueEncodingRanges);
                _keyValueEncodingRanges = null;
            }
        }

        private KeyEncodingComparer GetKeyEncodingComparer()
        {
            return _keyEncodingComparer ??= new KeyEncodingComparer(this);
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

        // Defines order and equality semantics for a key/value encoding range pair up to key encoding
        private class KeyEncodingComparer : IComparer<KeyValueEncodingRange>,
                                            IEqualityComparer<(int Offset, int Length)>
        {
            private readonly CborWriter _writer;

            public KeyEncodingComparer(CborWriter writer)
            {
                _writer = writer;
            }

            private Span<byte> GetKeyEncoding((int Offset, int Length) range)
            {
                return _writer._buffer.AsSpan(range.Offset, range.Length);
            }

            private Span<byte> GetKeyEncoding(in KeyValueEncodingRange range)
            {
                return _writer._buffer.AsSpan(range.Offset, range.KeyLength);
            }

            public int GetHashCode((int Offset, int Length) range)
            {
                return CborConformanceLevelHelpers.GetKeyEncodingHashCode(GetKeyEncoding(range));
            }

            public bool Equals((int Offset, int Length) x, (int Offset, int Length) y)
            {
                return CborConformanceLevelHelpers.AreEqualKeyEncodings(GetKeyEncoding(x), GetKeyEncoding(y));
            }

            public int Compare(KeyValueEncodingRange x, KeyValueEncodingRange y)
            {
                return CborConformanceLevelHelpers.CompareKeyEncodings(GetKeyEncoding(in x), GetKeyEncoding(in y), _writer.ConformanceLevel);
            }
        }
    }
}
