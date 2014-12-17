// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

            if (!_leaveOpen && _stream != null)
            {
                _stream.Dispose();
                _stream = null;
            }

            if (_lazyMemoryMap != null)
            {
                _lazyMemoryMap.Dispose();
                _lazyMemoryMap = null;
            }
        }

        public override int Size
        {
            get
            {
                return _imageSize;
            }
        }

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
                IDisposable accessor;
                if (TryCreateMemoryMapAccessor(absoluteStart, size, out accessor))
                {
                    return new MemoryMappedFileBlock(accessor, size);
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

        private bool TryCreateMemoryMapAccessor(long start, int size, out IDisposable accessor)
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
                    accessor = null;
                    return false;
                }

                if (Interlocked.CompareExchange(ref _lazyMemoryMap, newMemoryMap, null) != null)
                {
                    newMemoryMap.Dispose();
                }
            }

            accessor = MemoryMapLightUp.CreateViewAccessor(_lazyMemoryMap, start, size);
            return accessor != null;
        }
    }
}