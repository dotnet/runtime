// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using static System.IO.Compression.ZLibNative;

namespace System.Net.WebSockets.Compression
{
    internal sealed class ZLibStreamPool
    {
        private static readonly ZLibStreamPool?[] s_pools
            = new ZLibStreamPool[WebSocketValidate.MaxDeflateWindowBits - WebSocketValidate.MinDeflateWindowBits + 1];

        /// <summary>
        /// The default amount of time after which a cached item will be removed.
        /// </summary>
        private const int DefaultTimeoutMilliseconds = 60_000;

        private readonly int _windowBits;
        private readonly List<CacheItem> _inflaters = new();
        private readonly List<CacheItem> _deflaters = new();
        private readonly Timer _cleaningTimer;

        /// <summary>
        /// The amount of time after which a cached item will be removed.
        /// </summary>
        private readonly int _timeoutMilliseconds;

        /// <summary>
        /// The number of cached inflaters and deflaters.
        /// </summary>
        private int _activeCount;

        private ZLibStreamPool(int windowBits, int timeoutMilliseconds)
        {
            // Use negative window bits to for raw deflate data
            _windowBits = -windowBits;
            _timeoutMilliseconds = timeoutMilliseconds;

            bool restoreFlow = false;
            try
            {
                if (!ExecutionContext.IsFlowSuppressed())
                {
                    ExecutionContext.SuppressFlow();
                    restoreFlow = true;
                }

                // There is no need to use weak references here, because these pools are kept
                // for the entire lifetime of the application. Also we reset the timer on each tick,
                // which prevents the object being rooted forever.
                _cleaningTimer = new Timer(x => ((ZLibStreamPool)x!).RemoveStaleItems(),
                    state: this, Timeout.Infinite, Timeout.Infinite);
            }
            finally
            {
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
        }

        public static ZLibStreamPool GetOrCreate(int windowBits)
        {
            Debug.Assert(windowBits >= WebSocketValidate.MinDeflateWindowBits
                      && windowBits <= WebSocketValidate.MaxDeflateWindowBits);

            int index = windowBits - WebSocketValidate.MinDeflateWindowBits;
            ref ZLibStreamPool? pool = ref s_pools[index];

            return Volatile.Read(ref pool) ?? EnsureInitialized(windowBits, ref pool);

            static ZLibStreamPool EnsureInitialized(int windowBits, ref ZLibStreamPool? target)
            {
                Interlocked.CompareExchange(ref target, new ZLibStreamPool(windowBits, DefaultTimeoutMilliseconds), null);

                Debug.Assert(target != null);
                return target;
            }
        }

        public ZLibStreamHandle GetInflater()
        {
            if (TryGet(_inflaters, out ZLibStreamHandle? stream))
            {
                return stream;
            }

            return CreateInflater();
        }

        public void ReturnInflater(ZLibStreamHandle stream)
        {
            if (stream.InflateReset() != ErrorCode.Ok)
            {
                stream.Dispose();
                return;
            }

            Return(stream, _inflaters);
        }

        public ZLibStreamHandle GetDeflater()
        {
            if (TryGet(_deflaters, out ZLibStreamHandle? stream))
            {
                return stream;
            }

            return CreateDeflater();
        }

        public void ReturnDeflater(ZLibStreamHandle stream)
        {
            if (stream.DeflateReset() != ErrorCode.Ok)
            {
                stream.Dispose();
                return;
            }

            Return(stream, _deflaters);
        }

        private void Return(ZLibStreamHandle stream, List<CacheItem> cache)
        {
            lock (cache)
            {
                cache.Add(new CacheItem(stream));

                if (Interlocked.Increment(ref _activeCount) == 1)
                {
                    _cleaningTimer.Change(_timeoutMilliseconds, Timeout.Infinite);
                }
            }
        }

        private bool TryGet(List<CacheItem> cache, [NotNullWhen(true)] out ZLibStreamHandle? stream)
        {
            lock (cache)
            {
                int count = cache.Count;

                if (count > 0)
                {
                    CacheItem item = cache[count - 1];
                    cache.RemoveAt(count - 1);
                    Interlocked.Decrement(ref _activeCount);

                    stream = item.Stream;
                    return true;
                }
            }

            stream = null;
            return false;
        }

        private void RemoveStaleItems()
        {
            RemoveStaleItems(_inflaters);
            RemoveStaleItems(_deflaters);

            // There is a race condition here, were _activeCount could be decremented
            // by a rent operation, but it's not big deal to schedule a timer tick that
            // would eventually do nothing.
            if (_activeCount > 0)
            {
                _cleaningTimer.Change(_timeoutMilliseconds, Timeout.Infinite);
            }
        }

        private void RemoveStaleItems(List<CacheItem> cache)
        {
            long currentTimestamp = Environment.TickCount64;
            List<ZLibStreamHandle>? removedStreams = null;

            lock (cache)
            {
                for (int index = 0; index < cache.Count; ++index)
                {
                    CacheItem item = cache[index];

                    if (currentTimestamp - item.Timestamp > _timeoutMilliseconds)
                    {
                        removedStreams ??= new List<ZLibStreamHandle>();
                        removedStreams.Add(item.Stream);
                        Interlocked.Decrement(ref _activeCount);
                    }
                    else
                    {
                        // The freshest streams are in the back of the collection.
                        // If we've reached a stream that is not timed out, all
                        // other after it will not be as well.
                        break;
                    }
                }

                if (removedStreams is null)
                {
                    return;
                }

                cache.RemoveRange(0, removedStreams.Count);
            }

            foreach (ZLibStreamHandle stream in removedStreams)
            {
                stream.Dispose();
            }
        }

        private ZLibStreamHandle CreateDeflater()
        {
            ZLibStreamHandle stream;
            ErrorCode errorCode;
            try
            {
                errorCode = CreateZLibStreamForDeflate(out stream,
                    level: CompressionLevel.DefaultCompression,
                    windowBits: _windowBits,
                    memLevel: Deflate_DefaultMemLevel,
                    strategy: CompressionStrategy.DefaultStrategy);
            }
            catch (Exception cause)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, cause);
            }

            if (errorCode != ErrorCode.Ok)
            {
                string message = errorCode == ErrorCode.MemError
                    ? SR.ZLibErrorNotEnoughMemory
                    : string.Format(SR.ZLibErrorUnexpected, (int)errorCode);
                throw new WebSocketException(message);
            }

            return stream;
        }

        private ZLibStreamHandle CreateInflater()
        {
            ZLibStreamHandle stream;
            ErrorCode errorCode;

            try
            {
                errorCode = CreateZLibStreamForInflate(out stream, _windowBits);
            }
            catch (Exception exception)
            {
                throw new WebSocketException(SR.ZLibErrorDLLLoadError, exception);
            }

            if (errorCode == ErrorCode.Ok)
            {
                return stream;
            }

            stream.Dispose();

            string message = errorCode == ErrorCode.MemError
                ? SR.ZLibErrorNotEnoughMemory
                : string.Format(SR.ZLibErrorUnexpected, (int)errorCode);
            throw new WebSocketException(message);
        }

        private readonly struct CacheItem
        {
            public CacheItem(ZLibStreamHandle stream)
            {
                Stream = stream;
                Timestamp = Environment.TickCount64;
            }

            public ZLibStreamHandle Stream { get; }

            /// <summary>
            /// The time when this item was returned to cache.
            /// </summary>
            public long Timestamp { get; }
        }
    }
}
