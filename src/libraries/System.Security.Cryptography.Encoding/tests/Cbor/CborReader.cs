// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
        FinishedWithTrailingBytes,
        EndOfData,
        FormatError,
    }

    internal partial class CborReader
    {
        private readonly ReadOnlyMemory<byte> _originalBuffer;
        private ReadOnlyMemory<byte> _buffer;
        private int _bytesRead = 0;

        private Stack<StackFrame>? _nestedDataItems;
        private int? _remainingDataItems; // remaining data items to read if context is definite-length collection
        private int _frameOffset = 0; // buffer offset particular to the current data item context
        private bool _isTagContext = false; // true if reader is expecting a tagged value

        // Map-specific bookkeeping
        private int? _currentKeyOffset = null;
        private bool _curentItemIsKey = false;
        private (int Offset, int Length)? _previousKeyRange;
        private HashSet<(int Offset, int Length)>? _previousKeyRanges;

        // keeps a cached copy of the reader state; 'Unknown' denotes uncomputed state
        private CborReaderState _cachedState = CborReaderState.Unknown;

        internal CborReader(ReadOnlyMemory<byte> buffer, CborConformanceLevel conformanceLevel = CborConformanceLevel.Lax, bool allowMultipleRootLevelValues = false)
        {
            CborConformanceLevelHelpers.Validate(conformanceLevel);

            _originalBuffer = buffer;
            _buffer = buffer;
            ConformanceLevel = conformanceLevel;
            AllowMultipleRootLevelValues = allowMultipleRootLevelValues;
            _remainingDataItems = allowMultipleRootLevelValues ? null : (int?)1;
        }

        public CborConformanceLevel ConformanceLevel { get; }
        public bool AllowMultipleRootLevelValues { get; }
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
                    // is at the end of a definite-length collection
                    switch (_nestedDataItems.Peek().MajorType)
                    {
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map: return CborReaderState.EndMap;
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception("CborReader internal error. Invalid CBOR major type pushed to stack.");
                    };
                }
                else
                {
                    // is at the end of the root value
                    return _buffer.IsEmpty ? CborReaderState.Finished : CborReaderState.FinishedWithTrailingBytes;
                }
            }

            if (_buffer.IsEmpty)
            {
                if (_remainingDataItems is null && (_nestedDataItems?.Count ?? 0) == 0)
                {
                    // is at the end of a well-defined sequence of root-level values
                    return CborReaderState.Finished;
                }

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

                if (_remainingDataItems is null)
                {
                    // stack guaranteed to be populated since root context cannot be indefinite-length
                    Debug.Assert(_nestedDataItems != null && _nestedDataItems.Count > 0);

                    switch (_nestedDataItems.Peek().MajorType)
                    {
                        case CborMajorType.ByteString: return CborReaderState.EndByteString;
                        case CborMajorType.TextString: return CborReaderState.EndTextString;
                        case CborMajorType.Array: return CborReaderState.EndArray;
                        case CborMajorType.Map when !_curentItemIsKey: return CborReaderState.FormatError;
                        case CborMajorType.Map: return CborReaderState.EndMap;
                        default:
                            Debug.Fail("CborReader internal error. Invalid CBOR major type pushed to stack.");
                            throw new Exception("CborReader internal error. Invalid CBOR major type pushed to stack.");
                    };
                }
                else
                {
                    return CborReaderState.FormatError;
                }
            }

            if (_remainingDataItems is null && _nestedDataItems?.Count > 0)
            {
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
                    Debug.Fail("CborReader internal error. Invalid major type.");
                    throw new Exception("CborReader internal error. Invalid major type.");
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
                if (_remainingDataItems is null && _bytesRead > 0 && (_nestedDataItems?.Count ?? 0) == 0)
                {
                    throw new InvalidOperationException("No remaining root-level CBOR data items in the buffer.");
                }

                throw new FormatException("Unexpected end of buffer.");
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

        private void PushDataItem(CborMajorType type, int? expectedNestedItems)
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

            _remainingDataItems = expectedNestedItems;
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

            if (frame.MajorType == CborMajorType.Map)
            {
                ReturnKeyEncodingRangeSet();
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

        private void ResetBuffer(int position)
        {
            _buffer = _originalBuffer.Slice(position);
            _bytesRead = position;
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

        private readonly struct StackFrame
        {
            public StackFrame(CborMajorType type, int frameOffset, int? remainingDataItems,
                int? currentKeyOffset, bool currentItemIsKey,
                (int Offset, int Length)? previousKeyRange, HashSet<(int Offset, int Length)>? previousKeyRanges)
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
            public int? RemainingDataItems { get; }

            public int? CurrentKeyOffset { get; }
            public bool CurrentItemIsKey { get; }
            public (int Offset, int Length)? PreviousKeyRange { get; }
            public HashSet<(int Offset, int Length)>? PreviousKeyRanges { get; }
        }

        // Struct containing checkpoint data for rolling back reader state in the event of a failure
        // NB checkpoints do not contain stack information, so we can only roll back provided that the
        // reader is within the original context in which the checkpoint was created
        private readonly struct Checkpoint
        {
            public Checkpoint(int bytesRead, int stackDepth, int frameOffset, int? remainingDataItems,
                              int? currentKeyOffset, bool currentItemIsKey, (int Offset, int Length)? previousKeyRange,
                              HashSet<(int Offset, int Length)>? previousKeyRanges)
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
            public int? RemainingDataItems { get; }

            public int? CurrentKeyOffset { get; }
            public bool CurrentItemIsKey { get; }
            public (int Offset, int Length)? PreviousKeyRange { get; }
            public HashSet<(int Offset, int Length)>? PreviousKeyRanges { get; }
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
