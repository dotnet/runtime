// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Threading;

namespace System.Reflection.Internal
{
    /// <summary>
    /// Represents data read from a stream.
    /// </summary>
    /// <remarks>
    /// Uses memory map to load data from streams backed by files that are bigger than <see cref="MemoryMapThreshold"/>.
    /// </remarks>
    internal sealed class StreamMemoryBlockProvider : MemoryBlockProvider
    {
        // We're trying to balance total VM usage (which is a minimum of 64KB for a memory mapped file) 
        // with private working set (since heap memory will be backed by the paging file and non-sharable).
        // Internal for testing.
        internal const int MemoryMapThreshold = 16 * 1024;

        // The stream is user specified and might not be thread-safe.
        // Any read from the stream must be protected by streamGuard.
        private Stream _stream;
        private readonly object _streamGuard;

        private readonly bool _leaveOpen;
        private bool _useMemoryMap;
        private readonly bool _isFileStream;

        private readonly long _imageStart;
        private readonly int _imageSize;

        // MemoryMappedFile
        private IDisposable _lazyMemoryMap;

        public StreamMemoryBlockProvider(Stream stream, long imageStart, int imageSize, bool isFileStream, bool leaveOpen)
        {
            Debug.Assert(stream.CanSeek && stream.CanRead);
            _stream = stream;
            _streamGuard = new object();
            _imageStart = imageStart;
            _imageSize = imageSize;
            _leaveOpen = leaveOpen;
            _isFileStream = isFileStream;
            _useMemoryMap = isFileStream && MemoryMapLightUp.IsAvailable;
        }

        protected override void Dispose(bool disposing)
        {
            Debug.Assert(disposing);
            if (!_leaveOpen)
            {
                Interlocked.Exchange(ref _stream, null)?.Dispose();
            }

            Interlocked.Exchange(ref _lazyMemoryMap, null)?.Dispose();
        }

        public override int Size
        {
            get
            {
                return _imageSize;
            }
        }

        /// <exception cref="IOException">Error reading from the stream.</exception>
        internal static unsafe NativeHeapMemoryBlock ReadMemoryBlockNoLock(Stream stream, bool isFileStream, long start, int size)
        {
            var block = new NativeHeapMemoryBlock(size);
            bool fault = true;
            try
            {
                stream.Seek(start, SeekOrigin.Begin);

                if (!isFileStream || !FileStreamReadLightUp.TryReadFile(stream, block.Pointer, start, size))
                {
                    stream.CopyTo(block.Pointer, size);
                }

                fault = false;
            }
            finally
            {
                if (fault)
                {
                    block.Dispose();
                }
            }

            return block;
        }

        /// <exception cref="IOException">Error while reading from the stream.</exception>
        protected override AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
        {
            long absoluteStart = _imageStart + start;

            if (_useMemoryMap && size > MemoryMapThreshold)
            {
                MemoryMappedFileBlock block;
                if (TryCreateMemoryMappedFileBlock(absoluteStart, size, out block))
                {
                    return block;
                }

                _useMemoryMap = false;
            }

            lock (_streamGuard)
            {
                return ReadMemoryBlockNoLock(_stream, _isFileStream, absoluteStart, size);
            }
        }

        public override Stream GetStream(out StreamConstraints constraints)
        {
            constraints = new StreamConstraints(_streamGuard, _imageStart, _imageSize);
            return _stream;
        }

        /// <exception cref="IOException">IO error while mapping memory or not enough memory to create the mapping.</exception>
        private unsafe bool TryCreateMemoryMappedFileBlock(long start, int size, out MemoryMappedFileBlock block)
        {
            if (_lazyMemoryMap == null)
            {
                // leave the underlying stream open. It will be closed by the Dispose method.
                IDisposable newMemoryMap;

                // CreateMemoryMap might modify the stream (calls FileStream.Flush)
                lock (_streamGuard)
                {
                    newMemoryMap = MemoryMapLightUp.CreateMemoryMap(_stream);
                }

                if (newMemoryMap == null)
                {
                    block = null;
                    return false;
                }

                if (Interlocked.CompareExchange(ref _lazyMemoryMap, newMemoryMap, null) != null)
                {
                    newMemoryMap.Dispose();
                }
            }

            IDisposable accessor = MemoryMapLightUp.CreateViewAccessor(_lazyMemoryMap, start, size);
            if (accessor == null)
            {
                block = null;
                return false;
            }

            if (!MemoryMapLightUp.TryGetSafeBufferAndPointerOffset(accessor, out var safeBuffer, out long offset))
            {
                block = null;
                return false;
            }

            block = new MemoryMappedFileBlock(accessor, safeBuffer, offset, size);
            return true;
        }
    }
}
