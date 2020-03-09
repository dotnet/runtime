// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal enum CborReaderState
    {
        Start = 0,
        UnsignedInteger,
        NegativeInteger,
        ByteString,
        TextString,
        StartArray,
        EndArray,
        StartMap,
        EndMap,
        Tag,
        Special,
        Finished,
        EOF
    }

    internal partial class CborReader
    {
        private ReadOnlyMemory<byte> _buffer;

        private CborReaderState _currentState;
        private uint? _remainingDataItems;
        private readonly Stack<(CborMajorType type, uint? remainingDataItems)> _nestedDataItemStack;

        internal CborReader(ReadOnlyMemory<byte> buffer, uint? remainingDataItems, Stack<(CborMajorType type, uint? remainingDataItems)> nestedDataItemStack)
        {
            _buffer = buffer;
            _remainingDataItems = remainingDataItems;
            _nestedDataItemStack = nestedDataItemStack;
            _currentState = CborReaderState.Start;
        }

        internal CborReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
            _remainingDataItems = 1;
            _nestedDataItemStack = new Stack<(CborMajorType, uint?)>();
            _currentState = CborReaderState.Start;
        }

        public CborReaderState CurrentState => _currentState;

        public CborReaderState PeekState()
        {
            if(_buffer.IsEmpty)
            {
                return CborReaderState.EOF;
            }

            CborInitialByte initialByte = PeekInitialByte();

            return MapMajorTypeToReaderState(initialByte.MajorType);
        }

        internal CborInitialByte PeekInitialByte()
        {
            if (_buffer.IsEmpty)
            {
                throw new InvalidOperationException("end of buffer");
            }

            return new CborInitialByte(_buffer.Span[0]);
        }

        internal CborInitialByte PeekInitialByte(CborMajorType expectedType)
        {
            if (_buffer.IsEmpty)
            {
                throw new InvalidOperationException("end of buffer");
            }

            var result = new CborInitialByte(_buffer.Span[0]);

            if (expectedType != result.MajorType)
            {
                throw new InvalidOperationException("Data item major type mismatch.");
            }

            return result;
        }

        public bool TryPeek(out CborInitialByte result)
        {
            if (!_buffer.IsEmpty)
            {
                result = new CborInitialByte(_buffer.Span[0]);
                return true;
            }

            result = default;
            return false;
        }

        private void AdvanceBuffer(int length)
        {
            _buffer = _buffer.Slice(length);
        }

        private void EnsureBuffer(int length)
        {
            if (_buffer.Length < length)
            {
                throw new FormatException("Unexpected end of buffer.");
            }
        }

        private static CborReaderState MapMajorTypeToReaderState(CborMajorType type)
        {
            return type switch
            {
                CborMajorType.UnsignedInteger => CborReaderState.UnsignedInteger,
                CborMajorType.NegativeInteger => CborReaderState.NegativeInteger,
                CborMajorType.ByteString => CborReaderState.ByteString,
                CborMajorType.TextString => CborReaderState.TextString,
                CborMajorType.Array => CborReaderState.StartArray,
                CborMajorType.Map => CborReaderState.StartMap,
                CborMajorType.Tag => CborReaderState.Tag,
                CborMajorType.Special => CborReaderState.Special,
                _ => throw new ArgumentException("Invalid CBOR major type", nameof(type)),
            };
        }
    }
}
