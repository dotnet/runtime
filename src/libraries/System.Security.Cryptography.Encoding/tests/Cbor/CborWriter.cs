// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace System.Formats.Cbor
{
    public partial class CborWriter
    {
        private static readonly ArrayPool<byte> s_bufferPool = ArrayPool<byte>.Create();

        private byte[] _buffer = null!;
        private int _offset = 0;

        private Stack<StackFrame>? _nestedDataItems;
        private CborMajorType? _currentMajorType = null; // major type of the current data item context
        private int? _definiteLength; // predetermined definite-length of current data item context
        private int _itemsWritten = 0; // number of items written in the current context
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if writer is expecting a tagged value

        // Map-specific book-keeping
        private int? _currentKeyOffset = null; // offset for the current key encoding
        private int? _currentValueOffset = null; // offset for the current value encoding
        private bool _keysRequireSorting = false; // tracks whether key/value pair encodings need to be sorted
        private List<KeyValuePairEncodingRange>? _keyValuePairEncodingRanges = null; // all key/value pair encoding ranges
        private HashSet<(int Offset, int Length)>? _keyEncodingRanges = null; // all key encoding ranges up to encoding equality

        public CborWriter(CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax, bool encodeIndefiniteLengths = false, bool allowMultipleRootLevelValues = false)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            if (encodeIndefiniteLengths && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(conformanceLevel))
            {
                throw new ArgumentException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, conformanceLevel), nameof(encodeIndefiniteLengths));
            }

            ConformanceLevel = conformanceLevel;
            EncodeIndefiniteLengths = encodeIndefiniteLengths;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _definiteLength = allowMultipleRootLevelValues ? null : (int?)1;
        }

        public CborConformanceLevel ConformanceLevel { get; }
        public bool EncodeIndefiniteLengths { get; }
        public bool AllowMultipleRootLevelValues { get; }
        public int Depth => _nestedDataItems is null ? 0 : _nestedDataItems.Count;
        public int BytesWritten => _offset;
        // Returns true iff a complete CBOR document has been written to buffer
        public bool IsWriteCompleted => _currentMajorType is null && _itemsWritten > 0;

        public void Reset()
        {
            if (_offset > 0)
            {
                Array.Clear(_buffer, 0, _offset);

                _offset = 0;
                _nestedDataItems?.Clear();
                _currentMajorType = null;
                _definiteLength = null;
                _itemsWritten = 0;
                _frameOffset = 0;
                _isTagContext = false;

                _currentKeyOffset = null;
                _currentValueOffset = null;
                _keysRequireSorting = false;
                _keyValuePairEncodingRanges?.Clear();
                _keyEncodingRanges?.Clear();
            }
        }

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
                var reader = new CborReader(encodedValue, conformanceLevel: conformanceLevel, allowMultipleRootLevelValues: false);

                try
                {
                    reader.SkipValue(disableConformanceLevelChecks: false);
                }
                catch (FormatException e)
                {
                    throw new ArgumentException(SR.Cbor_Writer_PayloadIsNotValidCbor, e);
                }

                if (reader.HasData)
                {
                    throw new ArgumentException(SR.Cbor_Writer_PayloadIsNotValidCbor);
                }
            }
        }

        public byte[] Encode() => GetSpanEncoding().ToArray();

        public bool TryEncode(Span<byte> destination, out int bytesWritten)
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
            if (!IsWriteCompleted)
            {
                throw new InvalidOperationException(SR.Cbor_Writer_IncompleteCborDocument);
            }

            return new ReadOnlySpan<byte>(_buffer, 0, _offset);
        }

        private void EnsureWriteCapacity(int pendingCount)
        {
            if (pendingCount < 0)
            {
                throw new OverflowException();
            }

            if (_buffer is null || _buffer.Length - _offset < pendingCount)
            {
                const int BlockSize = 1024;
                int blocks = checked(_offset + pendingCount + (BlockSize - 1)) / BlockSize;
                Array.Resize(ref _buffer, BlockSize * blocks);
            }
        }

        private void PushDataItem(CborMajorType majorType, int? definiteLength)
        {
            _nestedDataItems ??= new Stack<StackFrame>();

            var frame = new StackFrame(
                type: _currentMajorType,
                frameOffset: _frameOffset,
                definiteLength: _definiteLength,
                itemsWritten: _itemsWritten,
                currentKeyOffset: _currentKeyOffset,
                currentValueOffset: _currentValueOffset,
                keysRequireSorting: _keysRequireSorting,
                keyValuePairEncodingRanges: _keyValuePairEncodingRanges,
                keyEncodingRanges: _keyEncodingRanges
            );

            _nestedDataItems.Push(frame);

            _currentMajorType = majorType;
            _frameOffset = _offset;
            _definiteLength = definiteLength;
            _itemsWritten = 0;
            _currentKeyOffset = null;
            _currentValueOffset = null;
            _keysRequireSorting = false;
            _keyEncodingRanges = null;
            _keyValuePairEncodingRanges = null;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            // Validate that the pop operation can be performed

            if (expectedType != _currentMajorType)
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, _currentMajorType));
            }

            Debug.Assert(_nestedDataItems?.Count > 0);

            if (_isTagContext)
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, CborMajorType.Tag));
            }

            if (_definiteLength - _itemsWritten > 0)
            {
                throw new InvalidOperationException(SR.Cbor_NotAtEndOfDefiniteLengthDataItem);
            }

            // Perform encoding fixups that require the current context and must be done before popping
            // NB map key sorting must happen before indefinite-length patching

            if (expectedType == CborMajorType.Map)
            {
                CompleteMapWrite();
            }

            if (_definiteLength == null)
            {
                CompleteIndefiniteLengthWrite(expectedType);
            }

            // pop writer state
            StackFrame frame = _nestedDataItems.Pop();
            _currentMajorType = frame.MajorType;
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsWritten = frame.ItemsWritten;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _currentValueOffset = frame.CurrentValueOffset;
            _keysRequireSorting = frame.KeysRequireSorting;
            _keyValuePairEncodingRanges = frame.KeyValuePairEncodingRanges;
            _keyEncodingRanges = frame.KeyEncodingRanges;
        }

        private void AdvanceDataItemCounters()
        {
            if (_currentMajorType == CborMajorType.Map)
            {
                if (_itemsWritten % 2 == 0)
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
                throw new InvalidOperationException(SR.Cbor_Writer_DefiniteLengthExceeded);
            }

            if (_currentMajorType != null)
            {
                switch (_currentMajorType.Value)
                {
                    // indefinite-length string contexts do not permit nesting
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        if (initialByte.MajorType == _currentMajorType &&
                            initialByte.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
                        {
                            break;
                        }

                        throw new InvalidOperationException(SR.Cbor_Writer_CannotNestDataItemsInIndefiniteLengthStrings);
                }
            }

            _buffer[_offset++] = initialByte.InitialByte;
        }

        private void CompleteIndefiniteLengthWrite(CborMajorType type)
        {
            Debug.Assert(_definiteLength == null);

            if (EncodeIndefiniteLengths)
            {
                // using indefinite-length encoding, append a break byte to the existing encoding
                EnsureWriteCapacity(1);
                _buffer[_offset++] = CborInitialByte.IndefiniteLengthBreakByte;
            }
            else
            {
                // indefinite-length not allowed, convert the encoding into definite-length
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
                        throw new Exception();
                }
            }
        }

        private readonly struct StackFrame
        {
            public StackFrame(
                CborMajorType? type,
                int frameOffset,
                int? definiteLength,
                int itemsWritten,
                int? currentKeyOffset,
                int? currentValueOffset,
                bool keysRequireSorting,
                List<KeyValuePairEncodingRange>? keyValuePairEncodingRanges,
                HashSet<(int Offset, int Length)>? keyEncodingRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;
                CurrentKeyOffset = currentKeyOffset;
                CurrentValueOffset = currentValueOffset;
                KeysRequireSorting = keysRequireSorting;
                KeyValuePairEncodingRanges = keyValuePairEncodingRanges;
                KeyEncodingRanges = keyEncodingRanges;
            }

            public CborMajorType? MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public int? CurrentValueOffset { get; }
            public bool KeysRequireSorting { get; }
            public List<KeyValuePairEncodingRange>? KeyValuePairEncodingRanges { get; }
            public HashSet<(int Offset, int Length)>? KeyEncodingRanges { get; }
        }
    }
}
