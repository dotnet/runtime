// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;

namespace System.Runtime.InteropServices
{
    /// <summary>
    /// Allows limited thread safe reuse of heap buffers to limit memory pressure.
    /// 
    /// This cache does not ensure that multiple copies of handles are not released back into the cache.
    /// </summary>
    internal sealed class SafeHeapHandleCache : IDisposable
    {
        private readonly ulong _minSize;
        private readonly ulong _maxSize;

        // internal for testing
        internal readonly SafeHeapHandle[] _handleCache;

        /// <param name="minSize">Smallest buffer size to allocate in bytes.</param>
        /// <param name="maxSize">The largest buffer size to cache in bytes.</param>
        /// <param name="maxHandles">The maximum number of handles to cache.</param>
        public SafeHeapHandleCache(ulong minSize = 64, ulong maxSize = 1024 * 2, int maxHandles = 0)
        {
            _minSize = minSize;
            _maxSize = maxSize;
            _handleCache = new SafeHeapHandle[maxHandles > 0 ? maxHandles : Environment.ProcessorCount * 4];
        }

        /// <summary>
        /// Get a HeapHandle
        /// </summary>
        public SafeHeapHandle Acquire(ulong minSize = 0)
        {
            if (minSize < _minSize) minSize = _minSize;

            SafeHeapHandle handle = null;

            for (int i = 0; i < _handleCache.Length; i++)
            {
                handle = Interlocked.Exchange(ref _handleCache[i], null);
                if (handle != null) break;
            }

            if (handle != null)
            {
                // One possible future consideration is to attempt cycling through to
                // find one that might already have sufficient capacity
                if (handle.ByteLength < minSize)
                    handle.Resize(minSize);
            }
            else
            {
                handle = new SafeHeapHandle(minSize);
            }

            return handle;
        }

        /// <summary>
        /// Give a HeapHandle back for potential reuse
        /// </summary>
        public void Release(SafeHeapHandle handle)
        {
            if (handle.ByteLength <= _maxSize)
            {
                for (int i = 0; i < _handleCache.Length; i++)
                {
                    // Push the handles down, walking the last one off the end to keep
                    // the top of the "stack" fresh
                    handle = Interlocked.Exchange(ref _handleCache[i], handle);
                    if (handle == null) return;
                }
            }

            handle.Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }

        private void Dispose(bool disposing)
        {
            if (disposing && _handleCache != null)
            {
                foreach (SafeHeapHandle handle in _handleCache)
                {
                    if (handle != null) handle.Dispose();
                }
            }
        }
    }
}
