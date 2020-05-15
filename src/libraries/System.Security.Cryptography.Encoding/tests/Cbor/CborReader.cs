// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SqlServer.Server;

namespace System.Formats.Cbor
{
    public enum CborReaderState
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
        FinishedWithTrailingBytes,
        EndOfData,
        FormatError,
    }

    public partial class CborReader
    {
        private readonly ReadOnlyMemory<byte> _buffer;
        private int _offset = 0;

        private Stack<StackFrame>? _nestedDataItems;
        private CborMajorType? _currentMajorType = null; // major type of the currently written data item. Null iff at the root context
        private int? _definiteLength; // predetermined definite-length of current data item context
        private int _itemsWritten = 0; // number of items written in the current context
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if reader is expecting a tagged value

        // Map-specific book-keeping
        private int? _currentKeyOffset = null; // offset for the current key encoding
        private (int Offset, int Length)? _previousKeyRange; // previous key encoding range
        private HashSet<(int Offset, int Length)>? _keyEncodingRanges; // all key encoding ranges up to encoding equality

        // flag used to temporarily disable conformance level checks,
        // e.g. during a skip operation over nonconforming encodings.
        private bool _isConformanceLevelCheckEnabled = true;

        // keeps a cached copy of the reader state; 'Unknown' denotes uncomputed state
        private CborReaderState _cachedState = CborReaderState.Unknown;

        public CborReader(ReadOnlyMemory<byte> buffer, CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax, bool allowMultipleRootLevelValues = false)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            _buffer = buffer;
            ConformanceLevel = conformanceLevel;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _definiteLength = allowMultipleRootLevelValues ? null : (int?)1;
        }

        public CborConformanceLevel ConformanceLevel { get; }
        public bool AllowMultipleRootLevelValues { get; }
        public int Depth => _nestedDataItems is null ? 0 : _nestedDataItems.Count;
        public int BytesRead => _offset;
        public int BytesRemaining => _buffer.Length - _offset;

        public CborReaderState PeekState()
        {
            if (_cachedState == CborReaderState.Unknown)
            {
                _cachedState = PeekStateCore(false);
            }

            return _cachedState;
        }

        private CborReaderState PeekStateCore(bool throwOnFormatErrors)
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                if (_currentMajorType != null)
                {
                    // is at the end of a definite-length collection
                    switch (_currentMajorType.Value)
                    {
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map: return CborReaderState.EndMap;
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception();
                    };
                }
                else
                {
                    // is at the end of the root value
                    return _offset == _buffer.Length ? CborReaderState.Finished : CborReaderState.FinishedWithTrailingBytes;
                }
            }

            if (_offset == _buffer.Length)
            {
                if (_currentMajorType is null && _definiteLength is null)
                {
                    // is at the end of a well-defined sequence of root-level values
                    return CborReaderState.Finished;
                }

                return CborReaderState.EndOfData;
            }

            var initialByte = new CborInitialByte(_buffer.Span[_offset]);

            if (initialByte.InitialByte == CborInitialByte.IndefiniteLengthBreakByte)
            {
                if (_isTagContext)
                {
                    return throwOnFormatErrors ?
                        throw new FormatException(SR.Cbor_Reader_InvalidCbor_TagNotFollowedByValue) :
                        CborReaderState.FormatError;
                }

                if (_definiteLength is null)
                {
                    // root context cannot be indefinite-length
                    Debug.Assert(_currentMajorType != null);

                    switch (_currentMajorType.Value)
                    {
                        case CborMajorType.ByteString: return CborReaderState.EndByteString;
                        case CborMajorType.TextString: return CborReaderState.EndTextString;
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map when _itemsWritten % 2 == 0: return CborReaderState.EndMap;
                        case CborMajorType.Map:
                            return throwOnFormatErrors ?
                                throw new FormatException(SR.Cbor_Reader_InvalidCbor_KeyMissingValue) :
                                CborReaderState.FormatError;
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception();
                    };
                }
                else
                {
                    return throwOnFormatErrors ?
                        throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedBreakByte) :
                        CborReaderState.FormatError;
                }
            }

            if (_definiteLength is null && _currentMajorType != null)
            {
                switch (_currentMajorType.Value)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        if (initialByte.MajorType != _currentMajorType.Value)
                        {
                            return throwOnFormatErrors ?
                                throw new FormatException(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem) :
                                CborReaderState.FormatError;
                        }

                        break;
                }
            }

            switch (initialByte.MajorType)
            {
                case CborMajorType.UnsignedInteger: return CborReaderState.UnsignedInteger;
                case CborMajorType.NegativeInteger: return CborReaderState.NegativeInteger;
                case CborMajorType.ByteString:
                    return (initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength) ?
                            CborReaderState.StartByteString :
                            CborReaderState.ByteString;

                case CborMajorType.TextString:
                    return (initialByte.AdditionalInfo == CborAdditionalInfo.IndefiniteLength) ?
                            CborReaderState.StartTextString :
                            CborReaderState.TextString;

                case CborMajorType.Array: return CborReaderState.StartArray;
                case CborMajorType.Map: return CborReaderState.StartMap;
                case CborMajorType.Tag: return CborReaderState.Tag;
                case CborMajorType.Simple: return MapSpecialValueTagToReaderState(initialByte.AdditionalInfo);
                default:
                    Debug.Fail("CborReader internal error. Invalid CBOR major type.");
                    throw new Exception();
            };

            static CborReaderState MapSpecialValueTagToReaderState (CborAdditionalInfo value)
            {
                // https://tools.ietf.org/html/rfc7049#section-2.3

                switch (value)
                {
                    case (CborAdditionalInfo)CborSimpleValue.Null:
                        return CborReaderState.Null;
                    case (CborAdditionalInfo)CborSimpleValue.True:
                    case (CborAdditionalInfo)CborSimpleValue.False:
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

        // TODO pass a `validateConformance` parameter
        // TODO change return type to ReadOnSpan<byte>
        public ReadOnlyMemory<byte> ReadEncodedValue()
        {
            // keep a snapshot of the current offset
            int initialOffset = _offset;

            // call skip to read and validate the next value
            SkipValue(validateConformance: true);

            // return the slice corresponding to the consumed value
            return _buffer.Slice(initialOffset, _offset - initialOffset);
        }

        private CborInitialByte PeekInitialByte()
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NoMoreDataItemsToRead);
            }

            if (_offset == _buffer.Length)
            {
                if (_currentMajorType is null && _definiteLength is null && _offset > 0)
                {
                    // we are at the end of a well-formed sequence of root-level CBOR values
                    throw new InvalidOperationException(SR.Cbor_Reader_NoMoreDataItemsToRead);
                }

                throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
            }

            var nextByte = new CborInitialByte(_buffer.Span[_offset]);

            if (_currentMajorType != null)
            {
                switch (_currentMajorType.Value)
                {
                    case CborMajorType.ByteString:
                    case CborMajorType.TextString:
                        // Indefinite-length string contexts allow two possible data items:
                        // 1) Definite-length string chunks of the same major type OR
                        // 2) a break byte denoting the end of the indefinite-length string context.
                        if (nextByte.InitialByte == CborInitialByte.IndefiniteLengthBreakByte ||
                            nextByte.MajorType == _currentMajorType.Value &&
                            nextByte.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
                        {
                            break;
                        }

                        throw new FormatException(SR.Format(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem, nextByte.MajorType));
                }
            }

            return nextByte;
        }

        private CborInitialByte PeekInitialByte(CborMajorType expectedType)
        {
            CborInitialByte result = PeekInitialByte();

            if (expectedType != result.MajorType)
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, result.MajorType));
            }

            return result;
        }

        private void ValidateNextByteIsBreakByte()
        {
            CborInitialByte result = PeekInitialByte();

            if (result.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
            {
                throw new InvalidOperationException(SR.Cbor_NotAtEndOfIndefiniteLengthDataItem);
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
                previousKeyRange: _previousKeyRange,
                previousKeyRanges: _keyEncodingRanges
            );

            _nestedDataItems.Push(frame);

            _currentMajorType = majorType;
            _definiteLength = definiteLength;
            _itemsWritten = 0;
            _frameOffset = _offset;
            _isTagContext = false;
            _currentKeyOffset = null;
            _previousKeyRange = null;
            _keyEncodingRanges = null;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            if (_currentMajorType is null)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_IsAtRootContext);
            }

            Debug.Assert(_nestedDataItems?.Count > 0);

            if (expectedType != _currentMajorType)
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, _currentMajorType.Value));
            }

            if (_definiteLength - _itemsWritten > 0)
            {
                throw new InvalidOperationException(SR.Cbor_NotAtEndOfDefiniteLengthDataItem);
            }

            if (_isTagContext)
            {
                throw new FormatException(SR.Cbor_Reader_InvalidCbor_TagNotFollowedByValue);
            }

            if (_currentMajorType == CborMajorType.Map)
            {
                ReturnKeyEncodingRangeAllocation();
            }

            StackFrame frame = _nestedDataItems.Pop();

            _currentMajorType = frame.MajorType;
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsWritten = frame.ItemsWritten;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _previousKeyRange = frame.PreviousKeyRange;
            _keyEncodingRanges = frame.PreviousKeyRanges;
            // Popping items from the stack can change the reader state
            // without necessarily needing to advance the buffer
            // (e.g. we're at the end of a definite-length collection).
            // We therefore need to invalidate the cache here.
            _cachedState = CborReaderState.Unknown;
        }

        private void AdvanceDataItemCounters()
        {
            Debug.Assert(_definiteLength is null || _definiteLength - _itemsWritten > 0);

            if (_currentMajorType == CborMajorType.Map)
            {
                if (_itemsWritten % 2 == 0)
                {
                    HandleMapKeyRead();
                }
                else
                {
                    HandleMapValueRead();
                }
            }

            _itemsWritten++;
            _isTagContext = false;
        }

        private ReadOnlySpan<byte> GetRemainingBytes() => _buffer.Span.Slice(_offset);

        private void AdvanceBuffer(int length)
        {
            Debug.Assert(_offset + length <= _buffer.Length);

            _offset += length;
            // invalidate the state cache
            _cachedState = CborReaderState.Unknown;
        }

        private void ResetBuffer(int position)
        {
            Debug.Assert(position <= _buffer.Length);

            _offset = position;
            // invalidate the state cache
            _cachedState = CborReaderState.Unknown;
        }

        private void EnsureReadCapacity(int length)
        {
            if (_buffer.Length - _offset < length)
            {
                throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
            }
        }

        private static void EnsureReadCapacity(ReadOnlySpan<byte> buffer, int requiredLength)
        {
            if (buffer.Length < requiredLength)
            {
                throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
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
                (int Offset, int Length)? previousKeyRange,
                HashSet<(int Offset, int Length)>? previousKeyRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;

                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyRange = previousKeyRange;
                PreviousKeyRanges = previousKeyRanges;
            }

            public CborMajorType? MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyRange { get; }
            public HashSet<(int Offset, int Length)>? PreviousKeyRanges { get; }
        }

        // Struct containing checkpoint data for rolling back reader state in the event of a failure
        // NB checkpoints do not contain stack information, so we can only roll back provided that the
        // reader is within the original context in which the checkpoint was created
        private readonly struct Checkpoint
        {
            public Checkpoint(
                int offset,
                int stackDepth,
                int frameOffset,
                int? definiteLength,
                int itemsWritten,
                int? currentKeyOffset,
                (int Offset, int Length)? previousKeyRange,
                HashSet<(int Offset, int Length)>? previousKeyRanges)
            {
                Offset = offset;
                StackDepth = stackDepth;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;

                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyRange = previousKeyRange;
                PreviousKeyRanges = previousKeyRanges;
            }

            public int Offset { get; }
            public int StackDepth { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyRange { get; }
            public HashSet<(int Offset, int Length)>? PreviousKeyRanges { get; }
        }

        private Checkpoint CreateCheckpoint()
        {
            return new Checkpoint(
                offset: _offset,
                stackDepth: Depth,
                frameOffset: _frameOffset,
                definiteLength: _definiteLength,
                itemsWritten: _itemsWritten,
                currentKeyOffset: _currentKeyOffset,
                previousKeyRange: _previousKeyRange,
                previousKeyRanges: _keyEncodingRanges);
        }

        private void RestoreCheckpoint(in Checkpoint checkpoint)
        {
            if (_nestedDataItems != null)
            {
                int stackOffset = _nestedDataItems.Count - checkpoint.StackDepth;

                Debug.Assert(stackOffset >= 0, "Attempting to restore checkpoint outside of its original context.");
                Debug.Assert(
                    (stackOffset == 0) ?
                    _frameOffset == checkpoint.FrameOffset :
                    _nestedDataItems.ToArray()[stackOffset - 1].FrameOffset == checkpoint.FrameOffset,
                    "Attempting to restore checkpoint outside of its original context.");

                // pop any nested data items introduced after the checkpoint
                for (int i = 0; i < stackOffset; i++)
                {
                    _nestedDataItems.Pop();
                }
            }

            _offset = checkpoint.Offset;
            _frameOffset = checkpoint.FrameOffset;
            _definiteLength = checkpoint.DefiniteLength;
            _itemsWritten = checkpoint.ItemsWritten;
            _currentKeyOffset = checkpoint.CurrentKeyOffset;
            _previousKeyRange = checkpoint.PreviousKeyRange;
            _keyEncodingRanges = checkpoint.PreviousKeyRanges;

            // invalidate the state cache
            _cachedState = CborReaderState.Unknown;
        }
    }
}
