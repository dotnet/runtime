// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborWriter : IDisposable
    {
        // TODO : determine if CryptoPool is more appropriate
        private static readonly ArrayPool<byte> s_bufferPool = ArrayPool<byte>.Create();

        private byte[] _buffer = null!;
        private int _offset = 0;

        private Stack<StackFrame>? _nestedDataItems;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be written.
        private int? _definiteLength = 1;
        private int _itemsWritten = 0; // number of items written in the current context
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if writer is expecting a tagged value
        // Map-specific bookkeeping
        private int? _currentKeyOffset = null;
        private int? _currentValueOffset = null;
        private SortedSet<(int Offset, int KeyLength, int TotalLength)>? _keyValueEncodingRanges = null;

        public CborWriter(CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax, bool patchIndefiniteLengthItems = false)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            ConformanceLevel = conformanceLevel;
            PatchIndefiniteLengthItems = patchIndefiniteLengthItems;
        }

        public bool PatchIndefiniteLengthItems { get; }
        public CborConformanceLevel ConformanceLevel { get; }
        public int BytesWritten => _offset;
        // Returns true iff a complete CBOR document has been written to buffer
        public bool IsWriteCompleted => (_nestedDataItems?.Count ?? 0) == 0 && _itemsWritten == _definiteLength;

        public void WriteEncodedValue(ReadOnlyMemory<byte> encodedValue)
        {
            ValidateEncoding(encodedValue, ConformanceLevel);
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

            static void ValidateEncoding(ReadOnlyMemory<byte> encodedValue, CborConformanceLevel conformanceLevel)
            {
                var reader = new CborReader(encodedValue, conformanceLevel: conformanceLevel);

                try
                {
                    reader.SkipValue(validateConformance: true);
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

        public byte[] GetEncoding() => GetSpanEncoding().ToArray();

        public bool TryWriteEncoding(Span<byte> destination, out int bytesWritten)
        {
            ReadOnlySpan<byte> encoding = GetSpanEncoding();

            if (encoding.Length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            encoding.CopyTo(destination);
            bytesWritten = encoding.Length;
            return true;
        }

        private ReadOnlySpan<byte> GetSpanEncoding()
        {
            CheckDisposed();

            if (!IsWriteCompleted)
            {
                throw new InvalidOperationException("Buffer contains incomplete CBOR document.");
            }

            return (_offset == 0) ? ReadOnlySpan<byte>.Empty : new ReadOnlySpan<byte>(_buffer, 0, _offset);
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

        private void PushDataItem(CborMajorType type, int? definiteLength)
        {
            _nestedDataItems ??= new Stack<StackFrame>();
            _nestedDataItems.Push(new StackFrame(type, _frameOffset, _definiteLength, _itemsWritten, _currentKeyOffset, _currentValueOffset, _keyValueEncodingRanges));
            _frameOffset = _offset;
            _definiteLength = definiteLength;
            _itemsWritten = 0;
            _currentKeyOffset = (type == CborMajorType.Map) ? (int?)_offset : null;
            _currentValueOffset = null;
            _keyValueEncodingRanges = null;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            // Validate that the pop operation can be performed

            if (_nestedDataItems is null || _nestedDataItems.Count == 0)
            {
                throw new InvalidOperationException("No active CBOR nested data item to pop.");
            }

            StackFrame frame = _nestedDataItems.Peek();

            if (expectedType != frame.MajorType)
            {
                throw new InvalidOperationException("Unexpected major type in nested CBOR data item.");
            }

            if (_isTagContext)
            {
                throw new InvalidOperationException("Tagged CBOR value context is incomplete.");
            }

            if (_definiteLength - _itemsWritten > 0)
            {
                throw new InvalidOperationException("Definite-length nested CBOR data item is incomplete.");
            }

            // Perform encoding fixups that require the current context and must be done before popping
            // NB key sorting must happen before indefinite-length patching

            if (expectedType == CborMajorType.Map && CborConformanceLevelHelpers.RequiresSortedKeys(ConformanceLevel))
            {
                SortKeyValuePairEncodings();
            }

            if (_definiteLength == null)
            {
                CompleteIndefiniteLengthCollection(expectedType);
            }

            // pop writer state
            _nestedDataItems.Pop();
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsWritten = frame.ItemsWritten;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _currentValueOffset = frame.CurrentValueOffset;
            _keyValueEncodingRanges = frame.KeyValueEncodingRanges;
        }

        private bool IsMajorTypeContext(CborMajorType type)
        {
            return _nestedDataItems?.Count > 0 && _nestedDataItems.Peek().MajorType == type;
        }

        private void AdvanceDataItemCounters()
        {
            if (_currentKeyOffset != null) // this is a map context
            {
                if (_currentValueOffset == null)
                {
                    HandleKeyWritten();
                }
                else
                {
                    HandleValueWritten();
                }
            }

            _itemsWritten++;
            _isTagContext = false;
        }

        private void WriteInitialByte(CborInitialByte initialByte)
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                throw new InvalidOperationException("Adding a CBOR data item to the current context exceeds its definite length.");
            }

            if (_nestedDataItems != null && _nestedDataItems.Count > 0)
            {
                CborMajorType parentType = _nestedDataItems.Peek().MajorType;

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

        private void CompleteIndefiniteLengthCollection(CborMajorType type)
        {
            Debug.Assert(_definiteLength == null);

            if (PatchIndefiniteLengthItems)
            {
                switch (type)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        PatchIndefiniteLengthString(type);
                        break;
                    case CborMajorType.Array:
                        PatchIndefiniteLengthCollection(CborMajorType.Array, _itemsWritten);
                        break;
                    case CborMajorType.Map:
                        Debug.Assert(_itemsWritten % 2 == 0);
                        PatchIndefiniteLengthCollection(CborMajorType.Map, _itemsWritten / 2);
                        break;
                    default:
                        Debug.Fail("Invalid CBOR major type pushed to stack.");
                        throw new Exception("CborReader internal error. Invalid CBOR major type pushed to stack.");
                }
            }
            else
            {
                // no patching, so just append the break byte at the end of the existing encoding
                EnsureWriteCapacity(1);
                _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
            }
        }

        private readonly struct StackFrame
        {
            public StackFrame(CborMajorType type, int frameOffset, int? definiteLength, int itemsWritten,
                              int? currentKeyOffset, int? currentValueOffset, SortedSet<(int, int, int)>? keyValueEncodingRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;
                CurrentKeyOffset = currentKeyOffset;
                CurrentValueOffset = currentValueOffset;
                KeyValueEncodingRanges = keyValueEncodingRanges;
            }

            public CborMajorType MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public int? CurrentValueOffset { get; }
            public SortedSet<(int, int, int)>? KeyValueEncodingRanges { get; }
        }
    }
}
