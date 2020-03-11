// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal enum CborReaderState
    {
        Unknown = 0,
        UnsignedInteger,
        NegativeInteger,
        ByteString,
        TextString,
        StartArray,
        StartMap,
        EndArray,
        EndMap,
        Tag,
        Special,
        Finished,
        EndOfData,
    }

    internal partial class CborReader
    {
        private ReadOnlyMemory<byte> _buffer;
        private int _bytesRead = 0;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be read.
        private ulong? _remainingDataItems = 1;
        private Stack<(CborMajorType type, ulong? remainingDataItems)>? _nestedDataItemStack;

        internal CborReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
        }

        public int BytesRead => _bytesRead;
        public int BytesRemaining => _buffer.Length;

        public CborReaderState Peek()
        {
            if (_remainingDataItems is null)
            {
                throw new NotImplementedException("indefinite length collections");
            }

            if (_remainingDataItems == 0)
            {
                if (_nestedDataItemStack?.Count > 0)
                {
                    return _nestedDataItemStack.Peek().type switch
                    {
                        CborMajorType.Array => CborReaderState.EndArray,
                        CborMajorType.Map => CborReaderState.EndMap,
                        _ => throw new Exception("CborReader internal error. Invalid CBOR major type pushed to stack."),
                    };
                }
                else
                {
                    return CborReaderState.Finished;
                }
            }

            if (_buffer.IsEmpty)
            {
                return CborReaderState.EndOfData;
            }

            CborInitialByte initialByte = new CborInitialByte(_buffer.Span[0]);

            return initialByte.MajorType switch
            {
                CborMajorType.UnsignedInteger => CborReaderState.UnsignedInteger,
                CborMajorType.NegativeInteger => CborReaderState.NegativeInteger,
                CborMajorType.ByteString => CborReaderState.ByteString,
                CborMajorType.TextString => CborReaderState.TextString,
                CborMajorType.Array => CborReaderState.StartArray,
                CborMajorType.Map => CborReaderState.StartMap,
                CborMajorType.Tag => CborReaderState.Tag,
                CborMajorType.Special => CborReaderState.Special,
                _ => throw new FormatException("Invalid CBOR major type"),
            };
        }

        private CborInitialByte PeekInitialByte()
        {
            if (_remainingDataItems == 0)
            {
                throw new InvalidOperationException("Reading a CBOR data item in the current context exceeds its definite length.");
            }

            if (_buffer.IsEmpty)
            {
                throw new FormatException("unexpected end of buffer.");
            }

            return new CborInitialByte(_buffer.Span[0]);
        }

        private CborInitialByte PeekInitialByte(CborMajorType expectedType)
        {
            CborInitialByte result = PeekInitialByte();

            if (expectedType != result.MajorType)
            {
                throw new InvalidOperationException("Data item major type mismatch.");
            }

            return result;
        }

        private void PushDataItem(CborMajorType type, ulong? expectedNestedItems)
        {
            _nestedDataItemStack ??= new Stack<(CborMajorType, ulong?)>();
            _nestedDataItemStack.Push((type, _remainingDataItems));
            _remainingDataItems = expectedNestedItems;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            if (_remainingDataItems == null)
            {
                throw new NotImplementedException("Indefinite-length data items");
            }

            if (_remainingDataItems > 0)
            {
                throw new InvalidOperationException("Definite-length nested CBOR data item is incomplete.");
            }

            if (_nestedDataItemStack is null || _nestedDataItemStack.Count == 0)
            {
                throw new InvalidOperationException("No active CBOR nested data item to pop");
            }

            (CborMajorType actualType, ulong? remainingItems) = _nestedDataItemStack.Peek();

            if (expectedType != actualType)
            {
                throw new InvalidOperationException("Unexpected major type in nested CBOR data item.");
            }

            _nestedDataItemStack.Pop();
            _remainingDataItems = remainingItems;
        }

        private void AdvanceBuffer(int length)
        {
            _buffer = _buffer.Slice(length);
            _bytesRead += length;
        }

        private void EnsureBuffer(int length)
        {
            if (_buffer.Length < length)
            {
                throw new FormatException("Unexpected end of buffer.");
            }
        }
    }
}
