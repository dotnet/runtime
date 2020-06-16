// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    /// <summary>
    ///   A stateful, forward-only reader for CBOR encoded data.
    /// </summary>
    public partial class CborReader
    {
        private readonly ReadOnlyMemory<byte> _data;
        private int _offset = 0;

        private Stack<StackFrame>? _nestedDataItems;
        private CborMajorType? _currentMajorType = null; // major type of the currently written data item. Null iff at the root context
        private int? _definiteLength; // predetermined definite-length of current data item context
        private int _itemsWritten = 0; // number of items written in the current context
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if reader is expecting a tagged value

        // Map-specific book-keeping
        private int? _currentKeyOffset = null; // offset for the current key encoding
        private (int Offset, int Length)? _previousKeyEncodingRange; // previous key encoding range
        private HashSet<(int Offset, int Length)>? _keyEncodingRanges; // all key encoding ranges up to encoding equality

        // flag used to temporarily disable conformance level checks,
        // e.g. during a skip operation over nonconforming encodings.
        private bool _isConformanceLevelCheckEnabled = true;

        // keeps a cached copy of the reader state; 'None' denotes uncomputed state
        private CborReaderState _cachedState = CborReaderState.None;

        /// <summary>
        ///   The conformance level used by this reader.
        /// </summary>
        public CborConformanceLevel ConformanceLevel { get; }

        /// <summary>
        ///   Declares whether this reader allows multiple root-level CBOR data items.
        /// </summary>
        public bool AllowMultipleRootLevelValues { get; }

        /// <summary>
        ///   Gets the reader's current level of nestedness in the CBOR document.
        /// </summary>
        public int CurrentDepth => _nestedDataItems is null ? 0 : _nestedDataItems.Count;

        /// <summary>
        ///   Gets the total number of bytes that have been consumed by the reader.
        /// </summary>
        public int BytesRead => _offset;

        /// <summary>
        ///   Indicates whether or not the reader has remaining data available to process.
        /// </summary>
        public bool HasData => _offset != _data.Length;

        /// <summary>
        ///   Construct a CborReader instance over <paramref name="data"/> with given configuration.
        /// </summary>
        /// <param name="data">The CBOR encoded data to read.</param>
        /// <param name="conformanceLevel">
        ///   Specifies a conformance level guiding the conformance checks performed on the encoded data.
        ///   Defaults to <see cref="CborConformanceLevel.Lax" /> conformance level.
        /// </param>
        /// <param name="allowMultipleRootLevelValues">
        ///   Specify if multiple root-level values are to be supported by the reader.
        ///   When set to <see langword="false" />, the reader will throw an <see cref="InvalidOperationException"/>
        ///   if trying to read beyond the scope of one root-level CBOR data item.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="conformanceLevel"/> is not defined.
        /// </exception>
        public CborReader(ReadOnlyMemory<byte> data, CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax, bool allowMultipleRootLevelValues = false)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            _data = data;
            ConformanceLevel = conformanceLevel;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _definiteLength = allowMultipleRootLevelValues ? null : (int?)1;
        }

        /// <summary>
        ///   Read the next CBOR token, without advancing the reader.
        /// </summary>
        public CborReaderState PeekState()
        {
            if (_cachedState == CborReaderState.None)
            {
                _cachedState = PeekStateCore(throwOnFormatErrors:false);
            }

            return _cachedState;
        }

        /// <summary>
        ///   Reads the next CBOR data item, returning a <see cref="ReadOnlySpan{T}"/> view
        ///   of the encoded value. For indefinite length encodings this includes the break byte.
        /// </summary>
        /// <returns>A <see cref="ReadOnlySpan{T}"/> view of the encoded value.</returns>
        /// <exception cref="FormatException">
        ///   The data item is not a valid CBOR data item encoding. -or-
        ///   The CBOR encoding is not valid under the current conformance level
        /// </exception>
        public ReadOnlySpan<byte> ReadEncodedValue()
        {
            // keep a snapshot of the current offset
            int initialOffset = _offset;

            // call skip to read and validate the next value
            SkipValue(disableConformanceLevelChecks: false);

            // return the slice corresponding to the consumed value
            return _data.Span.Slice(initialOffset, _offset - initialOffset);
        }

        private CborReaderState PeekStateCore(bool throwOnFormatErrors)
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                // is at the end of a definite-length context
                if (_currentMajorType is null)
                {
                    return CborReaderState.Finished;
                }
                else
                {
                    switch (_currentMajorType.Value)
                    {
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map: return CborReaderState.EndMap;
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception();
                    }
                }
            }

            if (_offset == _data.Length)
            {
                // is at the end of the read buffer
                if (_currentMajorType is null && _definiteLength is null)
                {
                    // is at the end of a well-defined sequence of root-level values
                    return CborReaderState.Finished;
                }
                else
                {
                    // incomplete CBOR document(s)
                    return CborReaderState.EndOfData;
                }
            }

            // peek the next initial byte
            var initialByte = new CborInitialByte(_data.Span[_offset]);

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
                    switch (_currentMajorType)
                    {
                        case null:
                            // found a break byte at the end of a root-level data item sequence
                            Debug.Assert(AllowMultipleRootLevelValues);
                            return throwOnFormatErrors ?
                                throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedBreakByte) :
                                CborReaderState.FormatError;

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
                // is at indefinite-length nested data item
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
                        return CborReaderState.SimpleValue;
                }
            }
        }

        private CborInitialByte PeekInitialByte()
        {
            if (_definiteLength - _itemsWritten == 0)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NoMoreDataItemsToRead);
            }

            if (_offset == _data.Length)
            {
                if (_currentMajorType is null && _definiteLength is null && _offset > 0)
                {
                    // we are at the end of a well-formed sequence of root-level CBOR values
                    throw new InvalidOperationException(SR.Cbor_Reader_NoMoreDataItemsToRead);
                }

                throw new FormatException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
            }

            var nextByte = new CborInitialByte(_data.Span[_offset]);

            switch (_currentMajorType)
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

                    throw new FormatException(SR.Format(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem, (int)nextByte.MajorType));
            }

            return nextByte;
        }

        private CborInitialByte PeekInitialByte(CborMajorType expectedType)
        {
            CborInitialByte result = PeekInitialByte();

            if (expectedType != result.MajorType)
            {
                throw new InvalidOperationException(SR.Format(SR.Cbor_Reader_MajorTypeMismatch, (int)result.MajorType));
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
                previousKeyEncodingRange: _previousKeyEncodingRange,
                keyEncodingRanges: _keyEncodingRanges
            );

            _nestedDataItems.Push(frame);

            _currentMajorType = majorType;
            _definiteLength = definiteLength;
            _itemsWritten = 0;
            _frameOffset = _offset;
            _isTagContext = false;
            _currentKeyOffset = null;
            _previousKeyEncodingRange = null;
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
                throw new InvalidOperationException(SR.Format(SR.Cbor_PopMajorTypeMismatch, (int)_currentMajorType.Value));
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
                ReturnKeyEncodingRangeAllocation(_keyEncodingRanges);
            }

            StackFrame frame = _nestedDataItems.Pop();
            RestoreStackFrame(in frame);
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

        private ReadOnlySpan<byte> GetRemainingBytes() => _data.Span.Slice(_offset);

        private void AdvanceBuffer(int length)
        {
            Debug.Assert(_offset + length <= _data.Length);

            _offset += length;
            // invalidate the state cache
            _cachedState = CborReaderState.None;
        }

        private void ResetBuffer(int position)
        {
            Debug.Assert(position <= _data.Length);

            _offset = position;
            // invalidate the state cache
            _cachedState = CborReaderState.None;
        }

        private void EnsureReadCapacity(int length)
        {
            if (_data.Length - _offset < length)
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
                (int Offset, int Length)? previousKeyEncodingRange,
                HashSet<(int Offset, int Length)>? keyEncodingRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsWritten = itemsWritten;

                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyEncodingRange = previousKeyEncodingRange;
                KeyEncodingRanges = keyEncodingRanges;
            }

            public CborMajorType? MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyEncodingRange { get; }
            public HashSet<(int Offset, int Length)>? KeyEncodingRanges { get; }
        }

        private void RestoreStackFrame(in StackFrame frame)
        {
            _currentMajorType = frame.MajorType;
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsWritten = frame.ItemsWritten;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _previousKeyEncodingRange = frame.PreviousKeyEncodingRange;
            _keyEncodingRanges = frame.KeyEncodingRanges;
            // Popping items from the stack can change the reader state
            // without necessarily needing to advance the buffer
            // (e.g. we're at the end of a definite-length collection).
            // We therefore need to invalidate the cache here.
            _cachedState = CborReaderState.None;
        }

        // Struct containing checkpoint data for rolling back reader state in the event of a failure
        // NB checkpoints do not contain stack information, so we can only roll back provided that the
        // reader is within the original context in which the checkpoint was created
        private readonly struct Checkpoint
        {
            public Checkpoint(
                int depth,
                int offset,
                int frameOffset,
                int itemsWritten,
                int? currentKeyOffset,
                (int Offset, int Length)? previousKeyEncodingRange)

            {
                Depth = depth;
                Offset = offset;
                FrameOffset = frameOffset;
                ItemsWritten = itemsWritten;
                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyEncodingRange = previousKeyEncodingRange;
            }

            public int Depth { get; }
            public int Offset { get; }
            public int FrameOffset { get; }
            public int ItemsWritten { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyEncodingRange { get; }
        }

        private Checkpoint CreateCheckpoint()
        {
            return new Checkpoint(
                depth: CurrentDepth,
                offset: _offset,
                frameOffset: _frameOffset,
                itemsWritten: _itemsWritten,
                currentKeyOffset: _currentKeyOffset,
                previousKeyEncodingRange: _previousKeyEncodingRange);
        }

        private void RestoreCheckpoint(in Checkpoint checkpoint)
        {
            int restoreHeight = CurrentDepth - checkpoint.Depth;
            Debug.Assert(restoreHeight >= 0, "Attempting to restore checkpoint outside of its original context.");

            if (restoreHeight > 0)
            {
                // pop any nested contexts added after the checkpoint

                Debug.Assert(_nestedDataItems != null);
                Debug.Assert(_nestedDataItems.ToArray()[restoreHeight - 1].FrameOffset == checkpoint.FrameOffset,
                                "Attempting to restore checkpoint outside of its original context.");

                StackFrame frame;
                for (int i = 0; i < restoreHeight - 1; i++)
                {
                    frame = _nestedDataItems.Pop();
                    ReturnKeyEncodingRangeAllocation(frame.KeyEncodingRanges);
                }

                frame = _nestedDataItems.Pop();
                RestoreStackFrame(in frame);
            }
            else
            {
                Debug.Assert(checkpoint.FrameOffset == _frameOffset, "Attempting to restore checkpoint outside of its original context.");
            }

            // Remove any key encodings added after the current checkpoint.
            // This is only needed when rolling back key reads in the Strict conformance level.
            if (_keyEncodingRanges != null && _itemsWritten > checkpoint.ItemsWritten)
            {
                int checkpointOffset = checkpoint.Offset;
                _keyEncodingRanges.RemoveWhere(key => key.Offset >= checkpointOffset);
            }

            _offset = checkpoint.Offset;
            _itemsWritten = checkpoint.ItemsWritten;
            _previousKeyEncodingRange = checkpoint.PreviousKeyEncodingRange;
            _currentKeyOffset = checkpoint.CurrentKeyOffset;
            _cachedState = CborReaderState.None;

            Debug.Assert(CurrentDepth == checkpoint.Depth);
        }
    }
}
