// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    /// <summary>A stateful, forward-only reader for Concise Binary Object Representation (CBOR) encoded data.</summary>
    public partial class CborReader
    {
        private readonly ReadOnlyMemory<byte> _data;
        private int _offset;

        private Stack<StackFrame>? _nestedDataItems;
        private CborMajorType? _currentMajorType; // major type of the currently written data item. Null iff at the root context
        private int? _definiteLength; // predetermined definite-length of current data item context
        private int _itemsRead; // number of items read in the current context
        private int _frameOffset; // buffer offset particular to the current data item context
        private bool _isTagContext; // true if reader is expecting a tagged value

        // Map-specific book-keeping
        private int? _currentKeyOffset; // offset for the current key encoding
        private (int Offset, int Length)? _previousKeyEncodingRange; // previous key encoding range
        private HashSet<(int Offset, int Length)>? _keyEncodingRanges; // all key encoding ranges up to encoding equality

        // flag used to temporarily disable conformance mode checks,
        // e.g. during a skip operation over nonconforming encodings.
        private bool _isConformanceModeCheckEnabled = true;

        // keeps a cached copy of the reader state; 'None' denotes uncomputed state
        private CborReaderState _cachedState = CborReaderState.Undefined;

        /// <summary>Gets the conformance mode used by this reader.</summary>
        /// <value>One of the enumeration values that represents the conformance mode used by this reader.</value>
        public CborConformanceMode ConformanceMode { get; }

        /// <summary>Gets a value that indicates whether this reader allows multiple root-level CBOR data items.</summary>
        /// <value><see langword="true" /> if this reader allows multiple root-level CBOR data items; <see langword="false" /> otherwise.</value>
        public bool AllowMultipleRootLevelValues { get; }

        /// <summary>Gets the reader's current level of nestedness in the CBOR document.</summary>
        /// <value>A number that represents the current level of nestedness in the CBOR document.</value>
        public int CurrentDepth => _nestedDataItems is null ? 0 : _nestedDataItems.Count;

        /// <summary>Gets the total number of unread bytes in the buffer.</summary>
        /// <value>The total number of unread bytes in the buffer.</value>
        public int BytesRemaining => _data.Length - _offset;

        /// <summary>Initializes a <see cref="CborReader" /> instance over the specified <paramref name="data" /> with the given configuration.</summary>
        /// <param name="data">The CBOR encoded data to read.</param>
        /// <param name="conformanceMode">One of the enumeration values to specify a conformance mode guiding the checks performed on the encoded data.
        /// Defaults to <see cref="CborConformanceMode.Strict" /> conformance mode.</param>
        /// <param name="allowMultipleRootLevelValues"><see langword="true" /> to indicate that multiple root-level values are supported by the reader; otherwise, <see langword="false" />.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="conformanceMode" /> is not defined.</exception>
        public CborReader(ReadOnlyMemory<byte> data, CborConformanceMode conformanceMode = CborConformanceMode.Strict, bool allowMultipleRootLevelValues = false)
        {
            CborConformanceModeHelpers.Validate(conformanceMode);

            _data = data;
            ConformanceMode = conformanceMode;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _definiteLength = allowMultipleRootLevelValues ? null : (int?)1;
        }

        /// <summary>Reads the next CBOR data item, returning a <see cref="ReadOnlyMemory{T}" /> view of the encoded value. For indefinite length encodings this includes the break byte.</summary>
        /// <param name="disableConformanceModeChecks"><see langword="true" /> to disable conformance mode validation for the read value, equivalent to using <see cref="CborConformanceMode.Lax" />; otherwise, <see langword="false" />.</param>
        /// <returns>A view of the encoded value as a contiguous region of memory.</returns>
        /// <exception cref="CborContentException">The data item is not a valid CBOR data item encoding.
        /// -or-
        /// The CBOR encoding is not valid under the current conformance mode.</exception>
        public ReadOnlyMemory<byte> ReadEncodedValue(bool disableConformanceModeChecks = false)
        {
            // keep a snapshot of the current offset
            int initialOffset = _offset;

            // call skip to read and validate the next value
            SkipValue(disableConformanceModeChecks);

            // return the slice corresponding to the consumed value
            return _data.Slice(initialOffset, _offset - initialOffset);
        }

        private CborInitialByte PeekInitialByte()
        {
            if (_definiteLength - _itemsRead == 0)
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

                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
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

                    throw new CborContentException(SR.Format(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem, (int)nextByte.MajorType));
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
                itemsRead: _itemsRead,
                currentKeyOffset: _currentKeyOffset,
                previousKeyEncodingRange: _previousKeyEncodingRange,
                keyEncodingRanges: _keyEncodingRanges
            );

            _nestedDataItems.Push(frame);

            _currentMajorType = majorType;
            _definiteLength = definiteLength;
            _itemsRead = 0;
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

            if (_definiteLength - _itemsRead > 0)
            {
                throw new InvalidOperationException(SR.Cbor_NotAtEndOfDefiniteLengthDataItem);
            }

            if (_isTagContext)
            {
                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_TagNotFollowedByValue);
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
            Debug.Assert(_definiteLength is null || _definiteLength - _itemsRead > 0);

            if (_currentMajorType == CborMajorType.Map)
            {
                if (_itemsRead % 2 == 0)
                {
                    HandleMapKeyRead();
                }
                else
                {
                    HandleMapValueRead();
                }
            }

            _itemsRead++;
            _isTagContext = false;
        }

        private ReadOnlySpan<byte> GetRemainingBytes() => _data.Span.Slice(_offset);

        private void AdvanceBuffer(int length)
        {
            Debug.Assert(_offset + length <= _data.Length);

            _offset += length;
            // invalidate the state cache
            _cachedState = CborReaderState.Undefined;
        }

        private void ResetBuffer(int position)
        {
            Debug.Assert(position <= _data.Length);

            _offset = position;
            // invalidate the state cache
            _cachedState = CborReaderState.Undefined;
        }

        private void EnsureReadCapacity(int length)
        {
            if (_data.Length - _offset < length)
            {
                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
            }
        }

        private static void EnsureReadCapacity(ReadOnlySpan<byte> buffer, int requiredLength)
        {
            if (buffer.Length < requiredLength)
            {
                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
            }
        }

        private readonly struct StackFrame
        {
            public StackFrame(
                CborMajorType? type,
                int frameOffset,
                int? definiteLength,
                int itemsRead,
                int? currentKeyOffset,
                (int Offset, int Length)? previousKeyEncodingRange,
                HashSet<(int Offset, int Length)>? keyEncodingRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                DefiniteLength = definiteLength;
                ItemsRead = itemsRead;

                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyEncodingRange = previousKeyEncodingRange;
                KeyEncodingRanges = keyEncodingRanges;
            }

            public CborMajorType? MajorType { get; }
            public int FrameOffset { get; }
            public int? DefiniteLength { get; }
            public int ItemsRead { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyEncodingRange { get; }
            public HashSet<(int Offset, int Length)>? KeyEncodingRanges { get; }
        }

        private void RestoreStackFrame(in StackFrame frame)
        {
            _currentMajorType = frame.MajorType;
            _frameOffset = frame.FrameOffset;
            _definiteLength = frame.DefiniteLength;
            _itemsRead = frame.ItemsRead;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _previousKeyEncodingRange = frame.PreviousKeyEncodingRange;
            _keyEncodingRanges = frame.KeyEncodingRanges;
            // Popping items from the stack can change the reader state
            // without necessarily needing to advance the buffer
            // (e.g. we're at the end of a definite-length collection).
            // We therefore need to invalidate the cache here.
            _cachedState = CborReaderState.Undefined;
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
                int itemsRead,
                int? currentKeyOffset,
                (int Offset, int Length)? previousKeyEncodingRange)

            {
                Depth = depth;
                Offset = offset;
                FrameOffset = frameOffset;
                ItemsRead = itemsRead;
                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyEncodingRange = previousKeyEncodingRange;
            }

            public int Depth { get; }
            public int Offset { get; }
            public int FrameOffset { get; }
            public int ItemsRead { get; }

            public int? CurrentKeyOffset { get; }
            public (int Offset, int Length)? PreviousKeyEncodingRange { get; }
        }

        private Checkpoint CreateCheckpoint()
        {
            return new Checkpoint(
                depth: CurrentDepth,
                offset: _offset,
                frameOffset: _frameOffset,
                itemsRead: _itemsRead,
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
            // This is only needed when rolling back key reads in the Strict conformance mode.
            if (_keyEncodingRanges != null && _itemsRead > checkpoint.ItemsRead)
            {
                int checkpointOffset = checkpoint.Offset;
                _keyEncodingRanges.RemoveWhere(key => key.Offset >= checkpointOffset);
            }

            _offset = checkpoint.Offset;
            _itemsRead = checkpoint.ItemsRead;
            _previousKeyEncodingRange = checkpoint.PreviousKeyEncodingRange;
            _currentKeyOffset = checkpoint.CurrentKeyOffset;
            _cachedState = CborReaderState.Undefined;

            Debug.Assert(CurrentDepth == checkpoint.Depth);
        }
    }
}
