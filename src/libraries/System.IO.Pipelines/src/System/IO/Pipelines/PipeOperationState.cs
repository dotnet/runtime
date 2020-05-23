// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.IO.Pipelines
{
    [DebuggerDisplay("State: {State}")]
    [StructLayout(LayoutKind.Explicit)]
    internal struct PipeOperationState
    {
        // Ensure reader and writer data not on same cache line
        [FieldOffset(0)]
        private ReaderData _readerData;
        [FieldOffset(128)]
        private WriterData _writerData;

        public BufferSegment? WritingHead
        {
            get => _writerData._writingHead;
            set => _writerData._writingHead = value;
        }

        public Memory<byte> WritingHeadMemory
        {
            get => _writerData._writingHeadMemory;
            set => _writerData._writingHeadMemory = value;
        }

        public int WritingHeadBytesBuffered
        {
            get => _writerData._writingHeadBytesBuffered;
            set => _writerData._writingHeadBytesBuffered = value;
        }

        public long UnflushedBytes
        {
            get => _writerData._unflushedBytes;
            set => _writerData._unflushedBytes = value;
        }

        public BufferSegment? ReadTail
        {
            get => _readerData._readTail;
            set => _readerData._readTail = value;
        }

        public int ReadTailIndex
        {
            get => _readerData._readTailIndex;
            set => _readerData._readTailIndex = value;
        }

        public BufferSegment? ReadHead
        {
            get => _readerData._readHead;
            set => _readerData._readHead = value;
        }

        public int ReadHeadIndex
        {
            get => _readerData._readHeadIndex;
            set => _readerData._readHeadIndex = value;
        }

        public long UnconsumedBytes
        {
            get => _readerData._unconsumedBytes;
            set => _readerData._unconsumedBytes = value;
        }

        public long LastExaminedIndex
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _readerData._lastExaminedIndex;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _readerData._lastExaminedIndex = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRead()
        {
            if ((_readerData._readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _readerData._readState |= ReadState.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginReadTentative()
        {
            if ((_readerData._readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            _readerData._readState |= ReadState.ReadingTentative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndRead()
        {
            if (_readerData._readState == ReadState.Inactive)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadToComplete();
            }

            _readerData._readState = ReadState.Inactive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWrite()
        {
            _writerData._writeState = (WriteState.Allocating | WriteState.Writing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            _writerData._writeState = WriteState.Inactive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWriteAllocation()
        {
            Debug.Assert(_writerData._writeState == (WriteState.Allocating | WriteState.Writing));
            _writerData._writeState = WriteState.Writing;
        }

        public bool IsWritingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_writerData._writeState & WriteState.Writing) != 0;
        }

        public bool IsWritingAllocating
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_writerData._writeState & WriteState.Allocating) != 0;
        }

        public bool IsReadingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_readerData._readState & ReadState.Reading) != 0;
        }

        private string State => $"WriteState: {_writerData._writeState}; ReadState: {_readerData._readState}";

        private struct ReaderData
        {
            public ReadState _readState;
            // The read head which is the start of the PipeReader's consumed bytes
            public BufferSegment? _readHead;
            public int _readHeadIndex;
            // The extent of the bytes available to the PipeReader to consume
            public BufferSegment? _readTail;
            public int _readTailIndex;
            // The number of bytes flushed but not consumed by the reader
            public long _unconsumedBytes;
            // Stores the last examined position, used to calculate how many bytes were to release
            // for back pressure management
            public long _lastExaminedIndex;
        }

        private struct WriterData
        {
            public volatile WriteState _writeState;
            // The write head which is the extent of the PipeWriter's written bytes
            public BufferSegment? _writingHead;
            public Memory<byte> _writingHeadMemory;
            public int _writingHeadBytesBuffered;
            // The number of bytes written but not flushed
            public long _unflushedBytes;
        }

        [Flags]
        internal enum ReadState : int
        {
            Inactive = 0,
            Reading = 1,
            ReadingTentative = 2
        }

        [Flags]
        internal enum WriteState : int
        {
            Inactive = 0,
            Allocating = 1,
            Writing = 2
        }
    }
}
