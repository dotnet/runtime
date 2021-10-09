// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        // Implements major type 5 encoding per https://tools.ietf.org/html/rfc7049#section-2.1

        private KeyEncodingComparer? _keyEncodingComparer;
        private Stack<HashSet<(int Offset, int Length)>>? _pooledKeyEncodingRangeSets;
        private Stack<List<KeyValuePairEncodingRange>>? _pooledKeyValuePairEncodingRangeLists;

        /// <summary>Writes the start of a definite or indefinite-length map (major type 5).</summary>
        /// <param name="definiteLength">The length of the definite-length map, or <see langword="null" /> for an indefinite-length map.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="definiteLength" /> parameter cannot be negative.</exception>
        /// <exception cref="InvalidOperationException">Writing a new value exceeds the definite length of the parent data item.
        /// -or-
        /// The major type of the encoded value is not permitted in the parent data item.
        /// -or-
        /// The written data is not accepted under the current conformance mode.</exception>
        /// <remarks>
        /// In canonical conformance modes, the writer will reject indefinite-length writes unless
        /// the <see cref="ConvertIndefiniteLengthEncodings" /> flag is enabled.
        /// Map contents are written as if arrays twice the length of the map's declared size.
        /// For instance, a map of size 1 containing a key of type <see cref="int" /> with a value of type string must be written
        /// by successive calls to <see cref="WriteInt32(int)" /> and <see cref="WriteTextString(System.ReadOnlySpan{char})" />.
        /// It is up to the caller to keep track of whether the next call is a key or a value.
        /// Fundamentally, this is a technical restriction stemming from the fact that CBOR allows keys of any type,
        /// for instance a map can contain keys that are maps themselves.
        /// </remarks>
        public void WriteStartMap(int? definiteLength)
        {
            if (definiteLength is null)
            {
                WriteStartMapIndefiniteLength();
            }
            else
            {
                WriteStartMapDefiniteLength(definiteLength.Value);
            }
        }

        /// <summary>Writes the end of a map (major type 5).</summary>
        /// <exception cref="InvalidOperationException">The written data is not accepted under the current conformance mode.
        /// -or-
        /// The definite-length map anticipates more data items.
        /// -or-
        /// The latest key/value pair is lacking a value.</exception>
        public void WriteEndMap()
        {
            if (_itemsWritten % 2 == 1)
            {
                throw new InvalidOperationException(SR.Cbor_Writer_MapIncompleteKeyValuePair);
            }

            PopDataItem(CborMajorType.Map);
            AdvanceDataItemCounters();
        }

        private void WriteStartMapDefiniteLength(int definiteLength)
        {
            if (definiteLength < 0 || definiteLength > int.MaxValue / 2)
            {
                throw new ArgumentOutOfRangeException(nameof(definiteLength));
            }

            WriteUnsignedInteger(CborMajorType.Map, (ulong)definiteLength);
            PushDataItem(CborMajorType.Map, definiteLength: 2 * definiteLength);
            _currentKeyOffset = _offset;
        }

        private void WriteStartMapIndefiniteLength()
        {
            if (!ConvertIndefiniteLengthEncodings && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            EnsureWriteCapacity(1);
            WriteInitialByte(new CborInitialByte(CborMajorType.Map, CborAdditionalInfo.IndefiniteLength));
            PushDataItem(CborMajorType.Map, definiteLength: null);
            _currentKeyOffset = _offset;
        }

        //
        // Map encoding conformance
        //

        private void HandleMapKeyWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset == null);

            if (CborConformanceModeHelpers.RequiresUniqueKeys(ConformanceMode))
            {
                HashSet<(int Offset, int Length)> keyEncodingRanges = GetKeyEncodingRanges();

                (int Offset, int Length) currentKey = (_currentKeyOffset.Value, _offset - _currentKeyOffset.Value);

                if (!keyEncodingRanges.Add(currentKey))
                {
                    // reset writer state to right before the offending key write
                    _buffer.AsSpan(currentKey.Offset, _offset).Clear();
                    _offset = currentKey.Offset;

                    throw new InvalidOperationException(SR.Format(SR.Cbor_ConformanceMode_ContainsDuplicateKeys, ConformanceMode));
                }
            }

            // record the value buffer offset
            _currentValueOffset = _offset;
        }

        private void HandleMapValueWritten()
        {
            Debug.Assert(_currentKeyOffset != null && _currentValueOffset != null);

            if (CborConformanceModeHelpers.RequiresSortedKeys(ConformanceMode))
            {
                List<KeyValuePairEncodingRange> keyValuePairEncodingRanges = GetKeyValueEncodingRanges();

                var currentKeyValueRange = new KeyValuePairEncodingRange(
                    offset: _currentKeyOffset.Value,
                    keyLength: _currentValueOffset.Value - _currentKeyOffset.Value,
                    totalLength: _offset - _currentKeyOffset.Value);

                // Check that the keys are written in sorted order.
                // Once invalidated, declare that the map requires sorting,
                // which will prompt a sorting of the encodings once map writes have completed.
                if (!_keysRequireSorting && keyValuePairEncodingRanges.Count > 0)
                {
                    KeyEncodingComparer comparer = GetKeyEncodingComparer();
                    KeyValuePairEncodingRange previousKeyValueRange = keyValuePairEncodingRanges[keyValuePairEncodingRanges.Count - 1];
                    _keysRequireSorting = comparer.Compare(previousKeyValueRange, currentKeyValueRange) > 0;
                }

                keyValuePairEncodingRanges.Add(currentKeyValueRange);
            }

            // update offset state to the next key
            _currentKeyOffset = _offset;
            _currentValueOffset = null;
        }

        private void CompleteMapWrite()
        {
            if (_keysRequireSorting)
            {
                Debug.Assert(_keyValuePairEncodingRanges != null);

                // sort the key/value ranges in-place
                _keyValuePairEncodingRanges.Sort(GetKeyEncodingComparer());

                // copy sorted ranges to temporary buffer
                int totalMapPayloadEncodingLength = _offset - _frameOffset;
                Span<byte> source = _buffer.AsSpan();

                byte[] tempBuffer = s_bufferPool.Rent(totalMapPayloadEncodingLength);
                Span<byte> tmpSpan = tempBuffer.AsSpan(0, totalMapPayloadEncodingLength);

                Span<byte> s = tmpSpan;
                foreach (KeyValuePairEncodingRange range in _keyValuePairEncodingRanges)
                {
                    ReadOnlySpan<byte> keyValuePairEncoding = source.Slice(range.Offset, range.TotalLength);
                    keyValuePairEncoding.CopyTo(s);
                    s = s.Slice(keyValuePairEncoding.Length);
                }
                Debug.Assert(s.IsEmpty);

                // now copy back to the original buffer segment & clean up
                tmpSpan.CopyTo(source.Slice(_frameOffset, totalMapPayloadEncodingLength));
                s_bufferPool.Return(tempBuffer);
            }

            ReturnKeyEncodingRangeAllocation();
            ReturnKeyValuePairEncodingRangeAllocation();
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

        private List<KeyValuePairEncodingRange> GetKeyValueEncodingRanges()
        {
            if (_keyValuePairEncodingRanges != null)
            {
                return _keyValuePairEncodingRanges;
            }

            if (_pooledKeyValuePairEncodingRangeLists != null &&
                _pooledKeyValuePairEncodingRangeLists.TryPop(out List<KeyValuePairEncodingRange>? result))
            {
                result.Clear();
                return _keyValuePairEncodingRanges = result;
            }

            return _keyValuePairEncodingRanges = new List<KeyValuePairEncodingRange>();
        }

        private void ReturnKeyValuePairEncodingRangeAllocation()
        {
            if (_keyValuePairEncodingRanges != null)
            {
                _pooledKeyValuePairEncodingRangeLists ??= new Stack<List<KeyValuePairEncodingRange>>();
                _pooledKeyValuePairEncodingRangeLists.Push(_keyValuePairEncodingRanges);
                _keyValuePairEncodingRanges = null;
            }
        }

        private KeyEncodingComparer GetKeyEncodingComparer()
        {
            return _keyEncodingComparer ??= new KeyEncodingComparer(this);
        }

        private readonly struct KeyValuePairEncodingRange
        {
            public KeyValuePairEncodingRange(int offset, int keyLength, int totalLength)
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
        private sealed class KeyEncodingComparer : IComparer<KeyValuePairEncodingRange>,
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

            private Span<byte> GetKeyEncoding(in KeyValuePairEncodingRange range)
            {
                return _writer._buffer.AsSpan(range.Offset, range.KeyLength);
            }

            public int GetHashCode((int Offset, int Length) range)
            {
                return CborConformanceModeHelpers.GetKeyEncodingHashCode(GetKeyEncoding(range));
            }

            public bool Equals((int Offset, int Length) x, (int Offset, int Length) y)
            {
                return CborConformanceModeHelpers.AreEqualKeyEncodings(GetKeyEncoding(x), GetKeyEncoding(y));
            }

            public int Compare(KeyValuePairEncodingRange x, KeyValuePairEncodingRange y)
            {
                return CborConformanceModeHelpers.CompareKeyEncodings(GetKeyEncoding(in x), GetKeyEncoding(in y), _writer.ConformanceMode);
            }
        }
    }
}
