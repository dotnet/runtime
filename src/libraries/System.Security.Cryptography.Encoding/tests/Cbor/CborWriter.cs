// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

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
        private Stack<(CborMajorType type, uint? remainingDataItems)>? _nestedDataItemStack;

        public CborWriter()
        {

        }

        public int BytesWritten => _offset;
        // Returns true iff a complete CBOR document has been written to buffer
        public bool IsWriteCompleted => _remainingDataItems == 0 && (_nestedDataItemStack?.Count ?? 0) == 0;

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

        private void EnsureCanWriteNewDataItem()
        {
            if (_remainingDataItems == 0)
            {
                throw new InvalidOperationException("Adding a CBOR data item to the current context exceeds its definite length.");
            }
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

        private void CheckDisposed()
        {
            if (_offset < 0)
            {
                throw new ObjectDisposedException(nameof(CborWriter));
            }
        }

        public void Dispose()
        {
            if (_buffer != null)
            {
                s_bufferPool.Return(_buffer, clearArray: true);
                _buffer = null!;
            }

            _offset = -1;
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
