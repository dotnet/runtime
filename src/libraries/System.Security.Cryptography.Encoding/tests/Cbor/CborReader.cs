// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal enum CborReaderState
    {
        Unknown = 0,
        UnsignedInteger,
        NegativeInteger,
        ByteString,
        TextString,
        StartTextString,
        StartByteString,
        StartArray,
        StartMap,
        EndTextString,
        EndByteString,
        EndArray,
        EndMap,
        Tag,
        Null,
        Boolean,
        HalfPrecisionFloat,
        SinglePrecisionFloat,
        DoublePrecisionFloat,
        SpecialValue,
        Finished,
        EndOfData,
        FormatError,
    }

    internal partial class CborReader
    {
        private ReadOnlyMemory<byte> _buffer;
        private int _bytesRead = 0;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be read.
        private ulong? _remainingDataItems = 1;
        private bool _isEvenNumberOfDataItemsRead = true; // required for indefinite-length map writes
        private Stack<(CborMajorType type, bool isEvenNumberOfDataItemsWritten, ulong? remainingDataItems)>? _nestedDataItemStack;
        private bool _isTagContext = false; // true if reader is expecting a tagged value

        // stores a reusable List allocation for keeping ranges in the buffer
        private List<(int offset, int length)>? _rangeListAllocation = null;

        internal CborReader(ReadOnlyMemory<byte> buffer)
        {
            _buffer = buffer;
        }

        public int BytesRead => _bytesRead;
        public int BytesRemaining => _buffer.Length;

        public CborReaderState Peek()
        {
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

            var initialByte = new CborInitialByte(_buffer.Span[0]);

            if (initialByte.InitialByte == CborInitialByte.IndefiniteLengthBreakByte)
            {
                if (_isTagContext)
                {
                    // indefinite-length collection has ended without providing value for tag
                    return CborReaderState.FormatError;
                }

                if (_remainingDataItems == null)
                {
                    // stack guaranteed to be populated since root context cannot be indefinite-length
                    Debug.Assert(_nestedDataItemStack != null && _nestedDataItemStack.Count > 0);

                    return _nestedDataItemStack.Peek().type switch
                    {
                        CborMajorType.ByteString => CborReaderState.EndByteString,
                        CborMajorType.TextString => CborReaderState.EndTextString,
                        CborMajorType.Array => CborReaderState.EndArray,
                        CborMajorType.Map when !_isEvenNumberOfDataItemsRead => CborReaderState.FormatError,
                        CborMajorType.Map => CborReaderState.EndMap,
                        _ => throw new Exception("CborReader internal error. Invalid CBOR major type pushed to stack."),
                    };
                }
                else
                {
                    return CborReaderState.FormatError;
                }
            }

            if (_remainingDataItems == null)
            {
                // stack guaranteed to be populated since root context cannot be indefinite-length
                Debug.Assert(_nestedDataItemStack != null && _nestedDataItemStack.Count > 0);

                CborMajorType parentType = _nestedDataItemStack.Peek().type;

                switch (parentType)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        // indefinite length string contexts can only contain data items of same major type
                        if (initialByte.MajorType != parentType)
                        {
                            return CborReaderState.FormatError;
                        }

                        break;
                }
            }

            return initialByte.MajorType switch
            {
                CborMajorType.UnsignedInteger => CborReaderState.UnsignedInteger,
                CborMajorType.NegativeInteger => CborReaderState.NegativeInteger,
                CborMajorType.ByteString when initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength => CborReaderState.StartByteString,
                CborMajorType.ByteString => CborReaderState.ByteString,
                CborMajorType.TextString when initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength => CborReaderState.StartTextString,
                CborMajorType.TextString => CborReaderState.TextString,
                CborMajorType.Array => CborReaderState.StartArray,
                CborMajorType.Map => CborReaderState.StartMap,
                CborMajorType.Tag => CborReaderState.Tag,
                CborMajorType.Special => MapSpecialValueTagToReaderState(initialByte.AdditionalInfo),
                _ => throw new Exception("CborReader internal error. Invalid major type."),
            };

            static CborReaderState MapSpecialValueTagToReaderState (CborAdditionalInfo value)
            {
                // https://tools.ietf.org/html/rfc7049#section-2.3

                switch (value)
                {
                    case CborAdditionalInfo.SpecialValueNull:
                        return CborReaderState.Null;
                    case CborAdditionalInfo.SpecialValueFalse:
                    case CborAdditionalInfo.SpecialValueTrue:
                        return CborReaderState.Boolean;
                    case CborAdditionalInfo.Additional16BitData:
                        return CborReaderState.HalfPrecisionFloat;
                    case CborAdditionalInfo.Additional32BitData:
                        return CborReaderState.SinglePrecisionFloat;
                    case CborAdditionalInfo.Additional64BitData:
                        return CborReaderState.DoublePrecisionFloat;
                    default:
                        return CborReaderState.SpecialValue;
                }
            }
        }

        public ReadOnlyMemory<byte> ReadEncodedValue()
        {
            // keep a snapshot of the initial buffer state
            ReadOnlyMemory<byte> initialBuffer = _buffer;
            int initialBytesRead = _bytesRead;

            // call skip to read and validate the next value
            SkipValue();

            // return the slice corresponding to the consumed value
            return initialBuffer.Slice(0, _bytesRead - initialBytesRead);
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

            var result = new CborInitialByte(_buffer.Span[0]);

            // TODO check for tag state

            if (_nestedDataItemStack != null && _nestedDataItemStack.Count > 0)
            {
                CborMajorType parentType = _nestedDataItemStack.Peek().type;

                switch (parentType)
                {
                    // indefinite-length string contexts do not permit nesting
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        if (result.InitialByte == CborInitialByte.IndefiniteLengthBreakByte ||
                            result.MajorType == parentType &&
                            result.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
                        {
                            break;
                        }

                        throw new FormatException("Indefinite-length CBOR string containing invalid data item.");
                }
            }

            return result;
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

        private void ReadNextIndefiniteLengthBreakByte()
        {
            CborInitialByte result = PeekInitialByte();

            if (result.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
            {
                throw new InvalidOperationException("Next data item is not indefinite-length break byte.");
            }
        }

        private void PushDataItem(CborMajorType type, ulong? expectedNestedItems)
        {
            if (expectedNestedItems > (ulong)_buffer.Length)
            {
                throw new FormatException("Insufficient buffer size for declared definite length in CBOR data item.");
            }

            _nestedDataItemStack ??= new Stack<(CborMajorType, bool, ulong?)>();
            _nestedDataItemStack.Push((type, _isEvenNumberOfDataItemsRead, _remainingDataItems));
            _remainingDataItems = expectedNestedItems;
            _isEvenNumberOfDataItemsRead = true;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            if (_nestedDataItemStack is null || _nestedDataItemStack.Count == 0)
            {
                throw new InvalidOperationException("No active CBOR nested data item to pop");
            }

            (CborMajorType actualType, bool isEvenNumberOfDataItemsWritten, ulong? remainingItems) = _nestedDataItemStack.Peek();

            if (expectedType != actualType)
            {
                throw new InvalidOperationException("Unexpected major type in nested CBOR data item.");
            }

            if (_remainingDataItems > 0)
            {
                throw new InvalidOperationException("Definite-length nested CBOR data item is incomplete.");
            }

            if (_isTagContext)
            {
                throw new FormatException("CBOR tag should be followed by a data item.");
            }

            _nestedDataItemStack.Pop();
            _remainingDataItems = remainingItems;
            _isEvenNumberOfDataItemsRead = isEvenNumberOfDataItemsWritten;
        }

        private void AdvanceDataItemCounters()
        {
            _remainingDataItems--;
            _isEvenNumberOfDataItemsRead = !_isEvenNumberOfDataItemsRead;
            _isTagContext = false;
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

        private static void EnsureBuffer(ReadOnlySpan<byte> buffer, int requiredLength)
        {
            if (buffer.Length < requiredLength)
            {
                throw new FormatException("Unexpected end of buffer.");
            }
        }

        private List<(int offset, int length)> AcquireRangeList()
        {
            List<(int offset, int length)>? ranges = Interlocked.Exchange(ref _rangeListAllocation, null);

            if (ranges != null)
            {
                ranges.Clear();
                return ranges;
            }

            return new List<(int, int)>();
        }

        private void ReturnRangeList(List<(int offset, int length)> ranges)
        {
            _rangeListAllocation = ranges;
        }
    }
}
