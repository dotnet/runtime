// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
        private readonly ReadOnlyMemory<byte> _originalBuffer;
        private ReadOnlyMemory<byte> _buffer;
        private int _bytesRead = 0;

        // remaining number of data items in current cbor context
        // with null representing indefinite length data items.
        // The root context ony permits one data item to be read.
        private uint? _remainingDataItems = 1;
        private Stack<StackFrame>? _nestedDataItems;
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if reader is expecting a tagged value

        // Map-specific bookkeeping
        private int? _currentKeyOffset = null;
        private bool _curentItemIsKey = false;
        private (int offset, int length)? _previousKeyRange;
        private HashSet<(int offset, int length)>? _previousKeyRanges;

        // stores a reusable List allocation for keeping ranges in the buffer
        private List<(int offset, int length)>? _rangeListAllocation = null;

        // keeps a cached copy of the reader state; 'Unknown' denotes uncomputed state
        private CborReaderState _cachedState = CborReaderState.Unknown;

        internal CborReader(ReadOnlyMemory<byte> buffer, CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            _originalBuffer = buffer;
            _buffer = buffer;
            ConformanceLevel = conformanceLevel;
        }

        public CborConformanceLevel ConformanceLevel { get; }
        public int BytesRead => _bytesRead;
        public int BytesRemaining => _buffer.Length;

        public CborReaderState PeekState()
        {
            if (_cachedState == CborReaderState.Unknown)
            {
                _cachedState = PeekStateCore();
            }

            return _cachedState;
        }

        private CborReaderState PeekStateCore()
        {
            if (_remainingDataItems == 0)
            {
                if (_nestedDataItems?.Count > 0)
                {
                    return _nestedDataItems.Peek().MajorType switch
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
                    Debug.Assert(_nestedDataItems != null && _nestedDataItems.Count > 0);

                    return _nestedDataItems.Peek().MajorType switch
                    {
                        CborMajorType.ByteString => CborReaderState.EndByteString,
                        CborMajorType.TextString => CborReaderState.EndTextString,
                        CborMajorType.Array => CborReaderState.EndArray,
                        CborMajorType.Map when !_curentItemIsKey => CborReaderState.FormatError,
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
                Debug.Assert(_nestedDataItems != null && _nestedDataItems.Count > 0);

                CborMajorType parentType = _nestedDataItems.Peek().MajorType;

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
                CborMajorType.Simple => MapSpecialValueTagToReaderState(initialByte.AdditionalInfo),
                _ => throw new Exception("CborReader internal error. Invalid major type."),
            };

            static CborReaderState MapSpecialValueTagToReaderState (CborAdditionalInfo value)
            {
                // https://tools.ietf.org/html/rfc7049#section-2.3

                switch (value)
                {
                    case CborAdditionalInfo.SimpleValueNull:
                        return CborReaderState.Null;
                    case CborAdditionalInfo.SimpleValueFalse:
                    case CborAdditionalInfo.SimpleValueTrue:
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

            if (_nestedDataItems != null && _nestedDataItems.Count > 0)
            {
                CborMajorType parentType = _nestedDataItems.Peek().MajorType;

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

        private void PushDataItem(CborMajorType type, uint? expectedNestedItems)
        {
            var frame = new StackFrame(
                type: type,
                frameOffset: _frameOffset,
                remainingDataItems: _remainingDataItems,
                currentKeyOffset: _currentKeyOffset,
                currentItemIsKey: _curentItemIsKey,
                previousKeyRange: _previousKeyRange,
                previousKeyRanges: _previousKeyRanges
            );

            _nestedDataItems ??= new Stack<StackFrame>();
            _nestedDataItems.Push(frame);

            _remainingDataItems = checked((uint?)expectedNestedItems);
            _frameOffset = _bytesRead;
            _isTagContext = false;
            _currentKeyOffset = (type == CborMajorType.Map) ? (int?)_bytesRead : null;
            _curentItemIsKey = type == CborMajorType.Map;
            _previousKeyRange = null;
            _previousKeyRanges = null;
        }

        private void PopDataItem(CborMajorType expectedType)
        {
            if (_nestedDataItems is null || _nestedDataItems.Count == 0)
            {
                throw new InvalidOperationException("No active CBOR nested data item to pop");
            }

            StackFrame frame = _nestedDataItems.Peek();

            if (expectedType != frame.MajorType)
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

            _nestedDataItems.Pop();

            _frameOffset = frame.FrameOffset;
            _remainingDataItems = frame.RemainingDataItems;
            _currentKeyOffset = frame.CurrentKeyOffset;
            _curentItemIsKey = frame.CurrentItemIsKey;
            _previousKeyRange = frame.PreviousKeyRange;
            _previousKeyRanges = frame.PreviousKeyRanges;
            // Popping items from the stack can change the reader state
            // without necessarily needing to advance the buffer
            // (e.g. we're at the end of a definite-length collection).
            // We therefore need to invalidate the cache here.
            _cachedState = CborReaderState.Unknown;
        }

        private void AdvanceDataItemCounters()
        {
            if (_currentKeyOffset != null) // is map context
            {
                if(_curentItemIsKey)
                {
                    HandleMapKeyAdded();
                }
                else
                {
                    HandleMapValueAdded();
                }
            }

            _remainingDataItems--;
            _isTagContext = false;
        }

        private void AdvanceBuffer(int length)
        {
            _buffer = _buffer.Slice(length);
            _bytesRead += length;
            // invalidate the state cache
            _cachedState = CborReaderState.Unknown;
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

        private readonly struct StackFrame
        {
            public StackFrame(CborMajorType type, int frameOffset, uint? remainingDataItems,
                              int? currentKeyOffset, bool currentItemIsKey,
                              (int offset, int length)? previousKeyRange, HashSet<(int offset, int length)>? previousKeyRanges)
            {
                MajorType = type;
                FrameOffset = frameOffset;
                RemainingDataItems = remainingDataItems;

                CurrentKeyOffset = currentKeyOffset;
                CurrentItemIsKey = currentItemIsKey;
                PreviousKeyRange = previousKeyRange;
                PreviousKeyRanges = previousKeyRanges;
            }

            public CborMajorType MajorType { get; }
            public int FrameOffset { get; }
            public uint? RemainingDataItems { get; }

            public int? CurrentKeyOffset { get; }
            public bool CurrentItemIsKey { get; }
            public (int offset, int length)? PreviousKeyRange { get; }
            public HashSet<(int offset, int length)>? PreviousKeyRanges { get; }
        }

        // Struct containing checkpoint data for rolling back reader state in the event of a failure
        // NB checkpoints do not contain stack information, so we can only roll back provided that the
        // reader is within the original context in which the checkpoint was created
        private readonly struct Checkpoint
        {
            public Checkpoint(int bytesRead, int stackDepth, int frameOffset, uint? remainingDataItems,
                              int? currentKeyOffset, bool currentItemIsKey, (int offset, int length)? previousKeyRange,
                              HashSet<(int offset, int length)>? previousKeyRanges)
            {
                BytesRead = bytesRead;
                StackDepth = stackDepth;
                FrameOffset = frameOffset;
                RemainingDataItems = remainingDataItems;

                CurrentKeyOffset = currentKeyOffset;
                CurrentItemIsKey = currentItemIsKey;
                PreviousKeyRange = previousKeyRange;
                PreviousKeyRanges = previousKeyRanges;
            }

            public int BytesRead { get; }
            public int StackDepth { get; }
            public int FrameOffset { get; }
            public uint? RemainingDataItems { get; }

            public int? CurrentKeyOffset { get; }
            public bool CurrentItemIsKey { get; }
            public (int offset, int length)? PreviousKeyRange { get; }
            public HashSet<(int offset, int length)>? PreviousKeyRanges { get; }
        }

        private Checkpoint CreateCheckpoint()
        {
            return new Checkpoint(
                bytesRead: _bytesRead,
                stackDepth: _nestedDataItems?.Count ?? 0,
                frameOffset: _frameOffset,
                remainingDataItems: _remainingDataItems,
                currentKeyOffset: _currentKeyOffset,
                currentItemIsKey: _curentItemIsKey,
                previousKeyRange: _previousKeyRange,
                previousKeyRanges: _previousKeyRanges);
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
                    _nestedDataItems.Skip(stackOffset - 1).FirstOrDefault().FrameOffset == checkpoint.FrameOffset,
                    "Attempting to restore checkpoint outside of its original context.");

                // pop any nested data items introduced after the checkpoint
                for (int i = 0; i < stackOffset; i++)
                {
                    _nestedDataItems.Pop();
                }
            }

            _buffer = _originalBuffer.Slice(checkpoint.BytesRead);
            _bytesRead = checkpoint.BytesRead;
            _frameOffset = checkpoint.FrameOffset;
            _remainingDataItems = checkpoint.RemainingDataItems;
            _currentKeyOffset = checkpoint.CurrentKeyOffset;
            _curentItemIsKey = checkpoint.CurrentItemIsKey;
            _previousKeyRange = checkpoint.PreviousKeyRange;
            _previousKeyRanges = checkpoint.PreviousKeyRanges;

            // invalidate the state cache
            _cachedState = CborReaderState.Unknown;
        }
    }
}
