// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO.Compression
{
    /// <summary>
    /// Provides a wrapper around the ZLib decompression API.
    /// </summary>
    internal sealed class Inflater : IDisposable
    {
        private const int MinWindowBits = -15;                      // WindowBits must be between -8..-15 to ignore the header, 8..15 for
        private const int MaxWindowBits = 47;                       // zlib headers, 24..31 for GZip headers, or 40..47 for either Zlib or GZip

        private bool _nonEmptyInput;                                // Whether there is any non empty input
        private bool _finished;                                     // Whether the end of the stream has been reached
        private bool _isDisposed;                                   // Prevents multiple disposals
        private readonly int _windowBits;                           // The WindowBits parameter passed to Inflater construction
        private readonly ZLibNative.ZLibStreamHandle _zlibStream;   // The handle to the primary underlying zlib stream
        private MemoryHandle _inputBufferHandle;                    // The handle to the buffer that provides input to _zlibStream
        private readonly long _uncompressedSize;
        private long _currentInflatedCount;

        private object SyncLock => this;                    // Used to make writing to unmanaged structures atomic

        private Inflater(int windowBits, long uncompressedSize, ZLibNative.ZLibStreamHandle zlibStream)
        {
            _finished = false;
            _nonEmptyInput = false;
            _isDisposed = false;
            _windowBits = windowBits;
            _uncompressedSize = uncompressedSize;
            _zlibStream = zlibStream;
        }

        public int AvailableOutput => (int)_zlibStream.AvailOut;

        /// <summary>
        /// Returns true if the end of the stream has been reached.
        /// </summary>
        public bool Finished() => _finished;

        public unsafe bool Inflate(out byte b)
        {
            fixed (byte* bufPtr = &b)
            {
                int bytesRead = InflateVerified(bufPtr, 1);
                Debug.Assert(bytesRead == 0 || bytesRead == 1);
                return bytesRead != 0;
            }
        }

        public unsafe int Inflate(byte[] bytes, int offset, int length)
        {
            // If Inflate is called on an invalid or unready inflater, return 0 to indicate no bytes have been read.
            if (length == 0)
                return 0;

            Debug.Assert(null != bytes, "Can't pass in a null output buffer!");
            fixed (byte* bufPtr = bytes)
            {
                return InflateVerified(bufPtr + offset, length);
            }
        }

        public unsafe int Inflate(Span<byte> destination)
        {
            // If Inflate is called on an invalid or unready inflater, return 0 to indicate no bytes have been read.
            if (destination.Length == 0)
                return 0;

            fixed (byte* bufPtr = &MemoryMarshal.GetReference(destination))
            {
                return InflateVerified(bufPtr, destination.Length);
            }
        }

        public unsafe int InflateVerified(byte* bufPtr, int length)
        {
            // State is valid; attempt inflation
            try
            {
                int bytesRead = 0;
                if (_uncompressedSize == -1)
                {
                    ReadOutput(bufPtr, length, out bytesRead);
                }
                else
                {
                    if (_uncompressedSize > _currentInflatedCount)
                    {
                        length = (int)Math.Min(length, _uncompressedSize - _currentInflatedCount);
                        ReadOutput(bufPtr, length, out bytesRead);
                        _currentInflatedCount += bytesRead;
                    }
                    else
                    {
                        _finished = true;
                        _zlibStream.AvailIn = 0;
                    }
                }
                return bytesRead;
            }
            finally
            {
                // Before returning, make sure to release input buffer if necessary:
                if (0 == _zlibStream.AvailIn && IsInputBufferHandleAllocated)
                {
                    DeallocateInputBufferHandle(resetStreamHandle: true);
                }
            }
        }

        private unsafe void ReadOutput(byte* bufPtr, int length, out int bytesRead)
        {
            if (ReadInflateOutput(bufPtr, length, ZLibNative.FlushCode.NoFlush, out bytesRead) == ZLibNative.ErrorCode.StreamEnd)
            {
                if (!NeedsInput() && IsGzipStream() && IsInputBufferHandleAllocated)
                {
                    _finished = ResetStreamForLeftoverInput();
                }
                else
                {
                    _finished = true;
                }
            }
        }

        /// <summary>
        /// If this stream has some input leftover that hasn't been processed then we should
        /// check if it is another GZip file concatenated with this one.
        ///
        /// Returns false if the leftover input is another GZip data stream.
        /// </summary>
        private unsafe bool ResetStreamForLeftoverInput()
        {
            Debug.Assert(!NeedsInput());
            Debug.Assert(IsGzipStream());
            Debug.Assert(IsInputBufferHandleAllocated);

            lock (SyncLock)
            {
                byte* nextInPointer = (byte*)_zlibStream.NextIn;
                uint nextAvailIn = _zlibStream.AvailIn;

                // Check the leftover bytes to see if they start with the gzip header ID bytes
                if (*nextInPointer != ZLibNative.GZip_Header_ID1 || (nextAvailIn > 1 && *(nextInPointer + 1) != ZLibNative.GZip_Header_ID2))
                {
                    return true;
                }

                // Reset our existing zstream.
                _zlibStream.InflateReset2_(_windowBits);

                _finished = false;
            }

            return false;
        }

        internal bool IsGzipStream() => _windowBits >= 24 && _windowBits <= 31;

        public bool NeedsInput() => _zlibStream.AvailIn == 0;

        public bool NonEmptyInput() => _nonEmptyInput;

        public void SetInput(byte[] inputBuffer, int startIndex, int count)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(inputBuffer != null);
            Debug.Assert(startIndex >= 0 && count >= 0 && count + startIndex <= inputBuffer.Length);
            Debug.Assert(!IsInputBufferHandleAllocated);

            SetInput(inputBuffer.AsMemory(startIndex, count));
        }

        public unsafe void SetInput(ReadOnlyMemory<byte> inputBuffer)
        {
            Debug.Assert(NeedsInput(), "We have something left in previous input!");
            Debug.Assert(!IsInputBufferHandleAllocated);

            if (inputBuffer.IsEmpty)
                return;

            lock (SyncLock)
            {
                _inputBufferHandle = inputBuffer.Pin();
                _zlibStream.NextIn = (IntPtr)_inputBufferHandle.Pointer;
                _zlibStream.AvailIn = (uint)inputBuffer.Length;
                _finished = false;
                _nonEmptyInput = true;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    _zlibStream.Dispose();
                }

                if (IsInputBufferHandleAllocated)
                {
                    // Unpin the input buffer, but avoid modifying the ZLibStreamHandle (which may have been disposed of).
                    DeallocateInputBufferHandle(resetStreamHandle: false);
                }

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Inflater()
        {
            Dispose(false);
        }

        /// <summary>
        /// Wrapper around the ZLib inflate function, configuring the stream appropriately.
        /// </summary>
        private unsafe ZLibNative.ErrorCode ReadInflateOutput(byte* bufPtr, int length, ZLibNative.FlushCode flushCode, out int bytesRead)
        {
            lock (SyncLock)
            {
                _zlibStream.NextOut = (IntPtr)bufPtr;
                _zlibStream.AvailOut = (uint)length;

                ZLibNative.ErrorCode errC = Inflate(flushCode);
                bytesRead = length - (int)_zlibStream.AvailOut;

                return errC;
            }
        }

        /// <summary>
        /// Wrapper around the ZLib inflate function
        /// </summary>
        private ZLibNative.ErrorCode Inflate(ZLibNative.FlushCode flushCode)
        {
            ZLibNative.ErrorCode errC;
            try
            {
                errC = _zlibStream.Inflate(flushCode);
            }
            catch (Exception cause) // could not load the Zlib DLL correctly
            {
                throw new ZLibException(SR.ZLibErrorDLLLoadError, cause);
            }
            switch (errC)
            {
                case ZLibNative.ErrorCode.Ok:           // progress has been made inflating
                case ZLibNative.ErrorCode.StreamEnd:    // The end of the input stream has been reached
                    return errC;

                case ZLibNative.ErrorCode.BufError:     // No room in the output buffer - inflate() can be called again with more space to continue
                    return errC;

                case ZLibNative.ErrorCode.MemError:     // Not enough memory to complete the operation
                    throw new ZLibException(SR.ZLibErrorNotEnoughMemory, "inflate_", (int)errC, _zlibStream.GetErrorMessage());

                case ZLibNative.ErrorCode.DataError:    // The input data was corrupted (input stream not conforming to the zlib format or incorrect check value)
                    throw new InvalidDataException(SR.UnsupportedCompression);

                case ZLibNative.ErrorCode.StreamError:  //the stream structure was inconsistent (for example if next_in or next_out was NULL),
                    throw new ZLibException(SR.ZLibErrorInconsistentStream, "inflate_", (int)errC, _zlibStream.GetErrorMessage());

                default:
                    throw new ZLibException(SR.ZLibErrorUnexpected, "inflate_", (int)errC, _zlibStream.GetErrorMessage());
            }
        }

        /// <summary>
        /// Frees the GCHandle being used to store the input buffer
        /// </summary>
        private void DeallocateInputBufferHandle(bool resetStreamHandle)
        {
            Debug.Assert(IsInputBufferHandleAllocated);

            lock (SyncLock)
            {
                if (resetStreamHandle)
                {
                    _zlibStream.AvailIn = 0;
                    _zlibStream.NextIn = ZLibNative.ZNullPtr;
                }
                _inputBufferHandle.Dispose();
            }
        }

        public static Inflater CreateInflater(int windowBits, long uncompressedSize = -1)
        {
            Debug.Assert(windowBits >= MinWindowBits && windowBits <= MaxWindowBits);

            ZLibNative.ZLibStreamHandle zlibStream = ZLibNative.ZLibStreamHandle.CreateForInflate(windowBits);

            return new Inflater(windowBits, uncompressedSize, zlibStream);
        }

        private unsafe bool IsInputBufferHandleAllocated => _inputBufferHandle.Pointer != default;
    }
}
