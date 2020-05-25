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
        public WriterDataState WriterData;
        [FieldOffset(128)]
        public ReaderDataState ReaderData;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginRead()
        {
            if ((ReaderData._readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            ReaderData._readState = ReadState.Reading;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginReadTentative()
        {
            if ((ReaderData._readState & ReadState.Reading) != 0)
            {
                ThrowHelper.ThrowInvalidOperationException_AlreadyReading();
            }

            ReaderData._readState = ReadState.ReadingTentative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndRead()
        {
            if (ReaderData._readState == ReadState.Inactive)
            {
                ThrowHelper.ThrowInvalidOperationException_NoReadToComplete();
            }

            ReaderData._readState = ReadState.Inactive;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BeginWrite()
        {
            WriterData._writeState = WriteState.Writing;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EndWrite()
        {
            WriterData._writeState = WriteState.Inactive;
        }

        public bool IsWritingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (WriterData._writeState & WriteState.Writing) != 0;
        }

        public bool IsReadingActive
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (ReaderData._readState & (ReadState.Reading | ReadState.ReadingTentative)) != 0;
        }

        private string State => $"WriteState: {WriterData._writeState}; ReadState: {ReaderData._readState}";

        [StructLayout(LayoutKind.Auto)]
        public struct ReaderDataState
        {
            // Do not use directly; alas there isn't a protection modifier that is only for containing type.
            public volatile ReadState _readState;

            // The read head which is the start of the PipeReader's consumed bytes
            public BufferSegment? ReadHead;
            public int ReadHeadIndex;
            // The extent of the bytes available to the PipeReader to consume
            public BufferSegment? ReadTail;
            public int ReadTailIndex;
            // The number of bytes flushed but not consumed by the reader
            public long UnconsumedBytes;
            // Stores the last examined position, used to calculate how many bytes were to release
            // for back pressure management
            public long LastExaminedIndex;
        }

        [StructLayout(LayoutKind.Auto)]
        public struct WriterDataState
        {
            // Do not use directly; alas there isn't a protection modifier that is only for containing type.
            public volatile WriteState _writeState;

            // The write head which is the extent of the PipeWriter's written bytes
            public BufferSegment? WritingHead;
            public Memory<byte> WritingHeadMemory;
            public int WritingHeadBytesBuffered;
            // The number of bytes written but not flushed
            public long UnflushedBytes;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Advance(int bytesWritten)
            {
                UnflushedBytes += bytesWritten;
                WritingHeadBytesBuffered += bytesWritten;
                WritingHeadMemory = WritingHeadMemory.Slice(bytesWritten);
            }
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
