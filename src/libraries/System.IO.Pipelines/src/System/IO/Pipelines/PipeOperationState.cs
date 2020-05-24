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
        private WriterData _writerData;
        [FieldOffset(128)]
        private ReaderData _readerData;

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
            _writerData._writeState = WriteState.Writing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            _writerData._writeState = WriteState.Inactive;
        }

        public bool IsWritingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_writerData._writeState & WriteState.Writing) != 0;
        }

        public bool IsReadingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_readerData._readState & ReadState.Reading) != 0;
        }

        private string State => $"WriteState: {_writerData._writeState}; ReadState: {_readerData._readState}";

        [StructLayout(LayoutKind.Auto)]
        private struct ReaderData
        {
            public volatile ReadState _readState;
        }

        [StructLayout(LayoutKind.Auto)]
        private struct WriterData
        {
            public volatile WriteState _writeState;
            // The write head which is the extent of the PipeWriter's written bytes
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
            Writing = 1
        }
    }
}
