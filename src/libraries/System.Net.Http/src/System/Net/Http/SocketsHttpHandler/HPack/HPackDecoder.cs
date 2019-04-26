// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Net.Http.HPack
{
    internal class HPackDecoder
    {
        // Can't use Action<,> because of Span
        public delegate void HeaderCallback(object state, ReadOnlySpan<byte> headerName, ReadOnlySpan<byte> headerValue);

        private enum State
        {
            Ready,
            HeaderFieldIndex,
            HeaderNameIndex,
            HeaderNameLength,
            HeaderNameLengthContinue,
            HeaderName,
            HeaderValueLength,
            HeaderValueLengthContinue,
            HeaderValue,
            DynamicTableSizeUpdate
        }

        public const int DefaultHeaderTableSize = 4096;

        // TODO: add new configurable limit
        public const int MaxStringOctets = 4096;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.1
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 1 |        Index (7+)         |
        // +---+---------------------------+
        private const byte IndexedHeaderFieldMask = 0x80;
        private const byte IndexedHeaderFieldRepresentation = 0x80;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.1
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 1 |      Index (6+)       |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithIncrementalIndexingMask = 0xc0;
        private const byte LiteralHeaderFieldWithIncrementalIndexingRepresentation = 0x40;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 0 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldWithoutIndexingMask = 0xf0;
        private const byte LiteralHeaderFieldWithoutIndexingRepresentation = 0x00;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.2.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 0 | 1 |  Index (4+)   |
        // +---+---+-----------------------+
        private const byte LiteralHeaderFieldNeverIndexedMask = 0xf0;
        private const byte LiteralHeaderFieldNeverIndexedRepresentation = 0x10;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.6.3
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | 0 | 0 | 1 |   Max size (5+)   |
        // +---+---------------------------+
        private const byte DynamicTableSizeUpdateMask = 0xe0;
        private const byte DynamicTableSizeUpdateRepresentation = 0x20;

        // http://httpwg.org/specs/rfc7541.html#rfc.section.5.2
        //   0   1   2   3   4   5   6   7
        // +---+---+---+---+---+---+---+---+
        // | H |    String Length (7+)     |
        // +---+---------------------------+
        private const byte HuffmanMask = 0x80;

        private const int IndexedHeaderFieldPrefix = 7;
        private const int LiteralHeaderFieldWithIncrementalIndexingPrefix = 6;
        private const int LiteralHeaderFieldWithoutIndexingPrefix = 4;
        private const int LiteralHeaderFieldNeverIndexedPrefix = 4;
        private const int DynamicTableSizeUpdatePrefix = 5;
        private const int StringLengthPrefix = 7;

        private readonly int _maxDynamicTableSize;
        private readonly DynamicTable _dynamicTable;
        private readonly IntegerDecoder _integerDecoder = new IntegerDecoder();
        private readonly byte[] _stringOctets = new byte[MaxStringOctets];
        private readonly byte[] _headerNameOctets = new byte[MaxStringOctets];
        private readonly byte[] _headerValueOctets = new byte[MaxStringOctets];

        private State _state = State.Ready;
        private byte[] _headerName;
        private int _stringIndex;
        private int _stringLength;
        private int _headerNameLength;
        private int _headerValueLength;
        private bool _index;
        private bool _huffman;

        public HPackDecoder(int maxDynamicTableSize = DefaultHeaderTableSize)
            : this(maxDynamicTableSize, new DynamicTable(maxDynamicTableSize))
        {
            _maxDynamicTableSize = maxDynamicTableSize;
        }

        // For testing.
        internal HPackDecoder(int maxDynamicTableSize, DynamicTable dynamicTable)
        {
            _maxDynamicTableSize = maxDynamicTableSize;
            _dynamicTable = dynamicTable;
        }

        public void Decode(ReadOnlySpan<byte> data, HeaderCallback onHeader, object onHeaderState)
        {
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];

                switch (_state)
                {
                    case State.Ready:
                        // TODO: Instead of masking and comparing each prefix value,
                        // consider doing a 16-way switch on the first four bits (which is the max prefix size).
                        // Look at this once we have more concrete perf data.
                        if ((b & IndexedHeaderFieldMask) == IndexedHeaderFieldRepresentation)
                        {
                            int val = b & ~IndexedHeaderFieldMask;

                            if (_integerDecoder.StartDecode((byte)val, IndexedHeaderFieldPrefix))
                            {
                                OnIndexedHeaderField(_integerDecoder.Value, onHeader, onHeaderState);
                            }
                            else
                            {
                                _state = State.HeaderFieldIndex;
                            }
                        }
                        else if ((b & LiteralHeaderFieldWithIncrementalIndexingMask) == LiteralHeaderFieldWithIncrementalIndexingRepresentation)
                        {
                            _index = true;
                            int val = b & ~LiteralHeaderFieldWithIncrementalIndexingMask;

                            if (val == 0)
                            {
                                _state = State.HeaderNameLength;
                            }
                            else if (_integerDecoder.StartDecode((byte)val, LiteralHeaderFieldWithIncrementalIndexingPrefix))
                            {
                                OnIndexedHeaderName(_integerDecoder.Value);
                            }
                            else
                            {
                                _state = State.HeaderNameIndex;
                            }
                        }
                        else if ((b & LiteralHeaderFieldWithoutIndexingMask) == LiteralHeaderFieldWithoutIndexingRepresentation)
                        {
                            _index = false;
                            int val = b & ~LiteralHeaderFieldWithoutIndexingMask;

                            if (val == 0)
                            {
                                _state = State.HeaderNameLength;
                            }
                            else if (_integerDecoder.StartDecode((byte)val, LiteralHeaderFieldWithoutIndexingPrefix))
                            {
                                OnIndexedHeaderName(_integerDecoder.Value);
                            }
                            else
                            {
                                _state = State.HeaderNameIndex;
                            }
                        }
                        else if ((b & LiteralHeaderFieldNeverIndexedMask) == LiteralHeaderFieldNeverIndexedRepresentation)
                        {
                            _index = false;
                            int val = b & ~LiteralHeaderFieldNeverIndexedMask;

                            if (val == 0)
                            {
                                _state = State.HeaderNameLength;
                            }
                            else if (_integerDecoder.StartDecode((byte)val, LiteralHeaderFieldNeverIndexedPrefix))
                            {
                                OnIndexedHeaderName(_integerDecoder.Value);
                            }
                            else
                            {
                                _state = State.HeaderNameIndex;
                            }
                        }
                        else if ((b & DynamicTableSizeUpdateMask) == DynamicTableSizeUpdateRepresentation)
                        {
                            if (_integerDecoder.StartDecode((byte)(b & ~DynamicTableSizeUpdateMask), DynamicTableSizeUpdatePrefix))
                            {
                                // TODO: validate that it's less than what's defined via SETTINGS
                                _dynamicTable.Resize(_integerDecoder.Value);
                            }
                            else
                            {
                                _state = State.DynamicTableSizeUpdate;
                            }
                        }
                        else
                        {
                            // Can't happen
                            Debug.Fail("Unreachable code");
                            throw new InternalException();
                        }

                        break;
                    case State.HeaderFieldIndex:
                        if (_integerDecoder.Decode(b))
                        {
                            OnIndexedHeaderField(_integerDecoder.Value, onHeader, onHeaderState);
                        }

                        break;
                    case State.HeaderNameIndex:
                        if (_integerDecoder.Decode(b))
                        {
                            OnIndexedHeaderName(_integerDecoder.Value);
                        }

                        break;
                    case State.HeaderNameLength:
                        _huffman = (b & HuffmanMask) != 0;

                        if (_integerDecoder.StartDecode((byte)(b & ~HuffmanMask), StringLengthPrefix))
                        {
                            OnStringLength(_integerDecoder.Value, nextState: State.HeaderName);
                        }
                        else
                        {
                            _state = State.HeaderNameLengthContinue;
                        }

                        break;
                    case State.HeaderNameLengthContinue:
                        if (_integerDecoder.Decode(b))
                        {
                            OnStringLength(_integerDecoder.Value, nextState: State.HeaderName);
                        }

                        break;
                    case State.HeaderName:
                        _stringOctets[_stringIndex++] = b;

                        if (_stringIndex == _stringLength)
                        {
                            OnString(nextState: State.HeaderValueLength);
                        }

                        break;
                    case State.HeaderValueLength:
                        _huffman = (b & HuffmanMask) != 0;

                        if (_integerDecoder.StartDecode((byte)(b & ~HuffmanMask), StringLengthPrefix))
                        {
                            if (_integerDecoder.Value > 0)
                            {
                                OnStringLength(_integerDecoder.Value, nextState: State.HeaderValue);
                            }
                            else
                            {
                                OnStringLength(_integerDecoder.Value, nextState: State.Ready);
                                OnHeaderComplete(onHeader, onHeaderState, new ReadOnlySpan<byte>(_headerName, 0, _headerNameLength), new ReadOnlySpan<byte>());
                            }
                        }
                        else
                        {
                            _state = State.HeaderValueLengthContinue;
                        }

                        break;
                    case State.HeaderValueLengthContinue:
                        if (_integerDecoder.Decode(b))
                        {
                            OnStringLength(_integerDecoder.Value, nextState: State.HeaderValue);
                        }

                        break;
                    case State.HeaderValue:
                        _stringOctets[_stringIndex++] = b;

                        if (_stringIndex == _stringLength)
                        {
                            OnString(nextState: State.Ready);

                            var headerNameSpan = new ReadOnlySpan<byte>(_headerName, 0, _headerNameLength);
                            var headerValueSpan = new ReadOnlySpan<byte>(_headerValueOctets, 0, _headerValueLength);

                            OnHeaderComplete(onHeader, onHeaderState, headerNameSpan, headerValueSpan);
                        }

                        break;
                    case State.DynamicTableSizeUpdate:
                        if (_integerDecoder.Decode(b))
                        {
                            if (_integerDecoder.Value > _maxDynamicTableSize)
                            {
                                // Dynamic table size update is too large.
                                throw new HPackDecodingException();
                            }

                            _dynamicTable.Resize(_integerDecoder.Value);
                            _state = State.Ready;
                        }

                        break;
                    default:
                        // Can't happen
                        Debug.Fail("HPACK decoder reach an invalid state");
                        throw new InternalException(_state);
                }
            }
        }

        public void CompleteDecode()
        {
            if (_state != State.Ready)
            {
                // Incomplete header block
                throw new HPackDecodingException();
            }
        }

        private void OnIndexedHeaderField(int index, HeaderCallback onHeader, object onHeaderState)
        {
            HeaderField header = GetHeader(index);
            onHeader(onHeaderState, new ReadOnlySpan<byte>(header.Name), new ReadOnlySpan<byte>(header.Value));
            _state = State.Ready;
        }

        private void OnIndexedHeaderName(int index)
        {
            HeaderField header = GetHeader(index);
            _headerName = header.Name;
            _headerNameLength = header.Name.Length;
            _state = State.HeaderValueLength;
        }

        private void OnStringLength(int length, State nextState)
        {
            if (length > _stringOctets.Length)
            {
                // String length too large.
                throw new HPackDecodingException();
            }

            _stringLength = length;
            _stringIndex = 0;
            _state = nextState;
        }

        private void OnString(State nextState)
        {
            int Decode(byte[] dst)
            {
                if (_huffman)
                {
                    return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), dst);
                }
                else
                {
                    Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
                    return _stringLength;
                }
            }

            try
            {
                if (_state == State.HeaderName)
                {
                    _headerName = _headerNameOctets;
                    _headerNameLength = Decode(_headerNameOctets);
                }
                else
                {
                    _headerValueLength = Decode(_headerValueOctets);
                }
            }
            catch (HuffmanDecodingException)
            {
                // Error in huffman encoding.
                throw new HPackDecodingException();
            }

            _state = nextState;
        }

        // Called when we have complete header with name and value.
        private void OnHeaderComplete(HeaderCallback onHeader, object onHeaderState, ReadOnlySpan<byte> headerName, ReadOnlySpan<byte> headerValue)
        {
            // Call provided callback.
            onHeader(onHeaderState, headerName, headerValue);

            if (_index)
            {
                _dynamicTable.Insert(headerName, headerValue);
            }
        }

        private HeaderField GetHeader(int index)
        {
            try
            {
                return index <= StaticTable.Count
                    ? StaticTable.Get(index - 1)
                    : _dynamicTable[index - StaticTable.Count - 1];
            }
            catch (IndexOutOfRangeException)
            {
                // Header index out of range.
                throw new HPackDecodingException();
            }
        }
    }
}
