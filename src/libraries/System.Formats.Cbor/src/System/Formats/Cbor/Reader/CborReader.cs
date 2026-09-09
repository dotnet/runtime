// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Formats.Cbor
{
    /// <summary>A stateful, forward-only reader for Concise Binary Object Representation (CBOR) encoded data.</summary>
    public partial class CborReader
    {
        private const int DefaultMaxDepth = 64;

        private ReadOnlyMemory<byte> _data;
        private int _offset;
        private bool _isFinalBlock = true; // false iff the caller has declared that more data may follow via SlideData

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

        /// <summary>Gets the maximum depth allowed when reading CBOR data.</summary>
        /// <value>The maximum depth allowed when reading CBOR data. The default is 64.</value>
        public int MaxDepth { get; }

        /// <summary>Gets the total number of unread bytes in the buffer.</summary>
        /// <value>The total number of unread bytes in the buffer.</value>
        public int BytesRemaining => _data.Length - _offset;

        /// <summary>Initializes a <see cref="CborReader" /> instance over the specified <paramref name="data" /> with the given options.</summary>
        /// <param name="data">The CBOR-encoded data to read.</param>
        /// <param name="options">The options that control reading behavior.</param>
        public CborReader(ReadOnlyMemory<byte> data, CborReaderOptions? options)
            : this(data, options, isFinalBlock: true)
        {
        }

        /// <summary>Initializes a <see cref="CborReader" /> instance over the specified <paramref name="data" /> with the given options.</summary>
        /// <param name="data">The CBOR-encoded data to read.</param>
        /// <param name="options">The options that control reading behavior.</param>
        /// <param name="isFinalBlock"><see langword="true" /> to indicate that <paramref name="data" /> contains the complete remainder of the document(s) to read;
        /// <see langword="false" /> if more data may be supplied using <see cref="SlideData" />.</param>
        /// <exception cref="ArgumentException"><paramref name="isFinalBlock" /> is <see langword="false" /> and the conformance mode is not <see cref="CborConformanceMode.Lax" />.</exception>
        public CborReader(ReadOnlyMemory<byte> data, CborReaderOptions? options, bool isFinalBlock)
        {
            CborConformanceMode conformanceMode = CborConformanceMode.Strict;
            bool allowMultipleRootLevelValues = false;
            int maxDepth = DefaultMaxDepth;

            if (options is not null)
            {
                conformanceMode = options.ConformanceMode;
                allowMultipleRootLevelValues = options.AllowMultipleRootLevelValues;
                maxDepth = options.MaxDepth;

                Debug.Assert(maxDepth >= -1);
            }

            ValidateIsFinalBlock(isFinalBlock, conformanceMode);

            _data = data;
            _isFinalBlock = isFinalBlock;
            ConformanceMode = conformanceMode;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            MaxDepth = maxDepth < 0 ? DefaultMaxDepth : maxDepth;
            _definiteLength = allowMultipleRootLevelValues ? null : 1;
        }

        /// <summary>Initializes a <see cref="CborReader" /> instance over the specified <paramref name="data" /> with the given configuration.</summary>
        /// <param name="data">The CBOR-encoded data to read.</param>
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
            MaxDepth = DefaultMaxDepth;
            _definiteLength = allowMultipleRootLevelValues ? null : 1;
        }

        /// <summary>Reads the next CBOR data item, returning a <see cref="ReadOnlyMemory{T}" /> view of the encoded value. For indefinite length encodings this includes the break byte.</summary>
        /// <param name="disableConformanceModeChecks"><see langword="true" /> to disable conformance mode validation for the read value, equivalent to using <see cref="CborConformanceMode.Lax" />; otherwise, <see langword="false" />.</param>
        /// <returns>A view of the encoded value as a contiguous region of memory.</returns>
        /// <exception cref="CborContentException"><para>The data item is not a valid CBOR data item encoding.</para>
        /// <para>-or-</para>
        /// <para>The CBOR encoding is not valid under the current conformance mode.</para></exception>
        /// <remarks>The returned memory is a view over the buffer supplied to the reader. If the caller reuses that buffer,
        /// for example when supplying new data with <see cref="SlideData" />, the contents of the returned memory may be overwritten.</remarks>
        public ReadOnlyMemory<byte> ReadEncodedValue(bool disableConformanceModeChecks = false)
        {
            // keep a snapshot of the current offset
            int initialOffset = _offset;

            // call skip to read and validate the next value
            SkipValue(disableConformanceModeChecks);

            // return the slice corresponding to the consumed value
            return _data.Slice(initialOffset, _offset - initialOffset);
        }

        /// <summary>
        /// Resets the <see cref="CborReader"/> instance over the specified <paramref name="data"/> with unchanged configuration.
        /// <see cref="ConformanceMode"/> and <see cref="AllowMultipleRootLevelValues"/> are unchanged.
        /// </summary>
        /// <param name="data">The CBOR-encoded data to read.</param>
        /// <remarks><paramref name="data" /> is treated as the final block: subsequent calls to <see cref="SlideData" /> throw.
        /// Use <see cref="Reset(ReadOnlyMemory{byte}, bool)" /> to start reading a new document incrementally.</remarks>
        public void Reset(ReadOnlyMemory<byte> data)
        {
            Reset(data, isFinalBlock: true);
        }

        /// <summary>
        /// Resets the <see cref="CborReader"/> instance over the specified <paramref name="data"/> with unchanged configuration.
        /// <see cref="ConformanceMode"/> and <see cref="AllowMultipleRootLevelValues"/> are unchanged.
        /// </summary>
        /// <param name="data">The CBOR-encoded data to read.</param>
        /// <param name="isFinalBlock"><see langword="true" /> to indicate that <paramref name="data" /> contains the complete remainder of the document(s) to read;
        /// <see langword="false" /> if more data may be supplied using <see cref="SlideData" />.</param>
        /// <exception cref="ArgumentException"><paramref name="isFinalBlock" /> is <see langword="false" /> and the conformance mode is not <see cref="CborConformanceMode.Lax" />.</exception>
        public void Reset(ReadOnlyMemory<byte> data, bool isFinalBlock)
        {
            // ConformanceMode and AllowMultipleRootLevelValues are set in ctor, they remain unchanged.

            ValidateIsFinalBlock(isFinalBlock, ConformanceMode);

            _data = data;
            _offset = 0;
            _isFinalBlock = isFinalBlock;

            _nestedDataItems?.Clear();
            _currentMajorType = default;
            _definiteLength = AllowMultipleRootLevelValues ? null : 1;
            _itemsRead = default;
            _frameOffset = default;
            _isTagContext = default;
            _currentKeyOffset = default;
            _previousKeyEncodingRange = default;
            _keyEncodingRanges?.Clear();
            _isConformanceModeCheckEnabled = true;
            _cachedState = CborReaderState.Undefined;

            // We don't need to clear the reusable instances in _pooledKeyEncodingRangeAllocations
            // or _indefiniteLengthStringRangeAllocation.
        }

        /// <summary>
        /// Replaces the buffer with data that continues from the reader's current position, preserving the nesting context.
        /// </summary>
        /// <param name="data">The CBOR-encoded data to continue reading from. It must start with the unconsumed bytes
        /// of the previous buffer (see <see cref="BytesRemaining" />), followed by any newly available data.</param>
        /// <param name="isFinalBlock"><see langword="true" /> to indicate that <paramref name="data" /> contains the complete remainder of the document(s) to read;
        /// <see langword="false" /> if more data may be supplied by a subsequent call to this method.</param>
        /// <exception cref="InvalidOperationException">The reader's current data was supplied as the final block.</exception>
        /// <exception cref="ArgumentException"><paramref name="data" /> is shorter than the unconsumed bytes of the current buffer (see <see cref="BytesRemaining" />).</exception>
        /// <remarks>
        /// <para>The caller is responsible for preserving all unread bytes, in order, at the beginning of <paramref name="data" />.
        /// Only the length of the new buffer is validated, not its contents.</para>
        /// <para><see cref="ReadOnlyMemory{T}" /> values previously returned by methods such as <see cref="ReadEncodedValue" /> are views
        /// over the reader's previous buffer; if the caller reuses that buffer, their contents may be overwritten.</para>
        /// <para>Calling this method after a complete document has been read does not resume reading; the reader continues to report
        /// <see cref="CborReaderState.Finished" />. Use <see cref="Reset(ReadOnlyMemory{byte}, bool)" /> to begin reading a new document.</para>
        /// </remarks>
        public void SlideData(ReadOnlyMemory<byte> data, bool isFinalBlock)
        {
            if (_isFinalBlock)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_CannotSlideDataOnFinalBlock);
            }

            if (data.Length < BytesRemaining)
            {
                throw new ArgumentException(SR.Cbor_Reader_SlideDataBufferTooSmall, nameof(data));
            }

            // Conformance bookkeeping (frame offsets, key encoding ranges) is buffer-relative
            // and becomes stale after a slide. This is safe because non-final mode requires
            // Lax conformance, which never dereferences it; enforced in the constructor
            // and Reset, asserted below. A conformance mode that reads this bookkeeping
            // must rebase it here before supporting non-final blocks.
            Debug.Assert(ConformanceMode == CborConformanceMode.Lax);
            Debug.Assert(_keyEncodingRanges is null);

            _data = data;
            _offset = 0;
            _isFinalBlock = isFinalBlock;
            _cachedState = CborReaderState.Undefined;
        }

        private CborInitialByte PeekInitialByte()
        {
            if (_definiteLength - _itemsRead == 0)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NoMoreDataItemsToRead);
            }

            if (_offset == _data.Length)
            {
                if (!_isFinalBlock)
                {
                    // more data may follow; PeekState reports this position as NeedsMoreData
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_UnexpectedEndOfBuffer);
                }

                // check _itemsRead in addition to _offset since SlideData resets the offset to 0
                if (_currentMajorType is null && _definiteLength is null && (_offset > 0 || _itemsRead > 0))
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

        private void EnsureMaxDepthNotExceeded()
        {
            if (CurrentDepth >= MaxDepth)
            {
                throw new CborContentException(SR.Format(SR.Cbor_Reader_MaximumDepthExceeded, MaxDepth));
            }
        }

        private void PushDataItem(CborMajorType majorType, int? definiteLength)
        {
            Debug.Assert(CurrentDepth < MaxDepth);

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

        private static void ValidateIsFinalBlock(bool isFinalBlock, CborConformanceMode conformanceMode)
        {
            // Non-Lax modes currently track map key encodings as offsets into the buffer, which
            // become stale when SlideData discards consumed bytes. Supporting them requires reader-owned
            // key copies; so we are currently restricting incremental reading to Lax.
            if (!isFinalBlock && conformanceMode != CborConformanceMode.Lax)
            {
                throw new ArgumentException(SR.Cbor_Reader_NotFinalBlockRequiresLaxConformance, nameof(isFinalBlock));
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
                bool isTagContext,
                int? currentKeyOffset,
                (int Offset, int Length)? previousKeyEncodingRange)

            {
                Depth = depth;
                Offset = offset;
                FrameOffset = frameOffset;
                ItemsRead = itemsRead;
                IsTagContext = isTagContext;
                CurrentKeyOffset = currentKeyOffset;
                PreviousKeyEncodingRange = previousKeyEncodingRange;
            }

            public int Depth { get; }
            public int Offset { get; }
            public int FrameOffset { get; }
            public int ItemsRead { get; }
            public bool IsTagContext { get; }

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
                isTagContext: _isTagContext,
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
            _isTagContext = checkpoint.IsTagContext;
            _previousKeyEncodingRange = checkpoint.PreviousKeyEncodingRange;
            _currentKeyOffset = checkpoint.CurrentKeyOffset;
            _cachedState = CborReaderState.Undefined;

            Debug.Assert(CurrentDepth == checkpoint.Depth);
        }
    }
}
