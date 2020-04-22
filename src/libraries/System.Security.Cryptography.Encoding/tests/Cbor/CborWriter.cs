// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Threading;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter : IDisposable
    {
        // TODO : determine if CryptoPool is more appropriate
        private static readonly ArrayPool<byte> s_bufferPool = ArrayPool<byte>.Create();

        private byte[] _buffer = null!;
        private int _offset = 0;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be written.
        private uint? _remainingDataItems = 1;
        private bool _isEvenNumberOfDataItemsWritten = true; // required for indefinite-length map writes
        private Stack<(CborMajorType type, bool isEvenNumberOfDataItemsWritten, uint? remainingDataItems)>? _nestedDataItemStack;
        private bool _isTagContext = false; // true if writer is expecting a tagged value

        public CborWriter()
        {

        }

        public int BytesWritten => _offset;
        // Returns true iff a complete CBOR document has been written to buffer
        public bool IsWriteCompleted => _remainingDataItems == 0 && (_nestedDataItemStack?.Count ?? 0) == 0;

        public void WriteEncodedValue(ReadOnlyMemory<byte> encodedValue)
        {
            ValidateEncoding(encodedValue);
            ReadOnlySpan<byte> encodedValueSpan = encodedValue.Span;
            EnsureWriteCapacity(encodedValueSpan.Length);

            // even though the encoding might be valid CBOR, it might not be valid within the current writer context.
            // E.g. we're at the end of a definite-length collection or writing integers in an indefinite-length string.
            // For this reason we write the initial byte separately and perform the usual validation.
            CborInitialByte initialByte = new CborInitialByte(encodedValueSpan[0]);
            WriteInitialByte(initialByte);

            // now copy any remaining bytes
            encodedValueSpan = encodedValueSpan.Slice(1);

            if (!encodedValueSpan.IsEmpty)
            {
                encodedValueSpan.CopyTo(_buffer.AsSpan(_offset));
                _offset += encodedValueSpan.Length;
            }

            AdvanceDataItemCounters();

            static void ValidateEncoding(ReadOnlyMemory<byte> encodedValue)
            {
                var reader = new CborReader(encodedValue);

                try
                {
                    reader.SkipValue();
                }
                catch (FormatException e)
                {
                    throw new ArgumentException("Payload is not a valid CBOR value.", e);
                }

                if (reader.BytesRemaining != 0)
                {
                    throw new ArgumentException("Payload is not a valid CBOR value.");
                }
            }
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            CheckDisposed();

            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            if (_buffer == null || _buffer.Length - _offset < pendingCount)
            {
                const int BlockSize = 1024;
                // While the ArrayPool may have similar logic, make sure we don't run into a lot of
                // "grow a little" by asking in 1k steps.
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                byte[]? oldBytes = _buffer;
                _buffer = s_bufferPool.Rent(BlockSize * blocks);

                if (oldBytes != null)
                {
                    Buffer.BlockCopy(oldBytes, 0, _buffer, 0, _offset);
                    s_bufferPool.Return(oldBytes, clearArray: true);
                }
            }
        }

        private void PushDataItem(CborMajorType type, uint? expectedNestedItems)
        {
            _nestedDataItemStack ??= new Stack<(CborMajorType, bool, uint?)>();
            _nestedDataItemStack.Push((type, _isEvenNumberOfDataItemsWritten, _remainingDataItems));
            _remainingDataItems = expectedNestedItems;
            _isEvenNumberOfDataItemsWritten = true;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            if (_nestedDataItemStack is null || _nestedDataItemStack.Count == 0)
            {
                throw new InvalidOperationException("No active CBOR nested data item to pop");
            }

            (CborMajorType actualType, bool isEvenNumberOfDataItemsWritten, uint? remainingItems) = _nestedDataItemStack.Peek();

            if (expectedType != actualType)
            {
                throw new InvalidOperationException("Unexpected major type in nested CBOR data item.");
            }

            if (_isTagContext)
            {
                throw new InvalidOperationException("Tagged CBOR value context is incomplete.");
            }

            if (_remainingDataItems > 0)
            {
                throw new InvalidOperationException("Definite-length nested CBOR data item is incomplete.");
            }

            _nestedDataItemStack.Pop();
            _remainingDataItems = remainingItems;
            _isEvenNumberOfDataItemsWritten = isEvenNumberOfDataItemsWritten;
        }

        private void AdvanceDataItemCounters()
        {
            _remainingDataItems--;
            _isTagContext = false;
            _isEvenNumberOfDataItemsWritten = !_isEvenNumberOfDataItemsWritten;
        }

        private void WriteInitialByte(CborInitialByte initialByte)
        {
            if (_remainingDataItems == 0)
            {
                throw new InvalidOperationException("Adding a CBOR data item to the current context exceeds its definite length.");
            }

            if (_nestedDataItemStack != null && _nestedDataItemStack.Count > 0)
            {
                CborMajorType parentType = _nestedDataItemStack.Peek().type;

                switch (parentType)
                {
                    // indefinite-length string contexts do not permit nesting
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        if (initialByte.MajorType == parentType &&
                            initialByte.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
                        {
                            break;
                        }

                        throw new InvalidOperationException("Cannot nest data items in indefinite-length CBOR string contexts.");
                }
            }

            _buffer[_offset++] = initialByte.InitialByte;
        }

        private void CheckDisposed()
        {
            if (_offset < 0)
            {
                throw new ObjectDisposedException(nameof(CborWriter));
            }
        }

        public void Dispose()
        {
            byte[]? buffer = Interlocked.Exchange(ref _buffer, null!);

            if (buffer != null)
            {
                s_bufferPool.Return(buffer, clearArray: true);
                _offset = -1;
            }
        }

        public byte[] ToArray()
        {
            CheckDisposed();

            if (!IsWriteCompleted)
            {
                throw new InvalidOperationException("Buffer contains incomplete CBOR document.");
            }

            return (_offset == 0) ? Array.Empty<byte>() : _buffer.AsSpan(0, _offset).ToArray();
        }
    }
}
