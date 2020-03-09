// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal enum CborReaderState
    {
        UnsignedInteger = 0,
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
        EOF
    }

    internal partial class CborReader
    {
        private ReadOnlyMemory<byte> _buffer;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be read.
        private uint? _remainingDataItems = 1;
        private Stack<(CborMajorType type, uint? remainingDataItems)>? _nestedDataItemStack;

        internal CborReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
        }

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
                    switch (_nestedDataItemStack.Peek().type)
                    {
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map: return CborReaderState.EndMap;
                        default: throw new Exception("CborReader internal error. Invalid CBOR major type pushed in stack.");
                    }
                }
                else
                {
                    return CborReaderState.Finished;
                }
            }

            if (_buffer.IsEmpty)
            {
                return CborReaderState.EOF;
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
            if (_buffer.IsEmpty)
            {
                throw new InvalidOperationException("end of buffer");
            }

            return new CborInitialByte(_buffer.Span[0]);
        }

        private CborInitialByte PeekInitialByte(CborMajorType expectedType)
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

        private void PushDataItem(CborMajorType type, uint? expectedNestedItems)
        {
            _nestedDataItemStack ??= new Stack<(CborMajorType, uint?)>();
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

            (CborMajorType actualType, uint? remainingItems) = _nestedDataItemStack.Peek();

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
        }

        private void EnsureBuffer(int length)
        {
            if (_buffer.Length < length)
            {
                throw new FormatException("Unexpected end of buffer.");
            }
        }

        private void EnsureCanReadNewDataItem()
        {
            if (_remainingDataItems == 0)
            {
                throw new InvalidOperationException("Adding a CBOR data item to the current context exceeds its definite length.");
            }
        }
    }
}
