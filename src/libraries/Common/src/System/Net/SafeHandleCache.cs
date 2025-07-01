// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace System.Net
{
    internal interface ISafeHandleCachable
    {
        // Attempts to resever the handle for use. If the handle is already
        // disposed (or scheduled to be disposed), this will return false.
        //
        // each successful call to TryAddRentCount() must be paired with a Dispose() call.
        bool TryAddRentCount();

        // Marks the handle as scheduled for disposal if it is not being used.
        // Returns false if the handle is currently being used.
        // once marked, no new renters are allowed.
        bool TryMarkForDispose();
    }

    /// <summary>
    /// Helper class for implementing a cache for types deriving from <see
    /// cref="SafeHandle"/>. The purpose of the cache is to allow reuse of
    /// resources which may enable additional features (such as TLS resumption).
    /// The cache handles insertion and eviction in a thread-safe manner and
    /// implements simple mechanism for preventing unbounded growth and memory
    /// leaks.
    /// </summary>
    internal class SafeHandleCache<TKey, THandle> where TKey : IEquatable<TKey> where THandle : SafeHandle, ISafeHandleCachable
    {
        private const int CheckExpiredModulo = 32;

        private readonly ConcurrentDictionary<TKey, THandle> _cache = new();

        /// <summary>
        /// Gets the handle from the cache if it exists, otherwise creates a new one using the
        /// provided factory function and context.
        ///
        /// In case of two racing inserts with the same key, the handle returned by the factory may
        /// end up being discarded in favor of the one that was inserted first.  In such case, the
        /// factory handle is disposed and the cached handle is returned.
        ///
        /// The handle returned from this function should be disposed exactly once when it is no
        /// longer needed.
        /// </summary>
        internal THandle GetOrCreate<TContext>(TKey key, Func<TContext, THandle> factory, TContext factoryContext)
        {
            if (_cache.TryGetValue(key, out THandle? handle) && handle.TryAddRentCount())
            {
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"Found cached {handle}.");
                }
                return handle;
            }

            // if we get here, the handle is either not in the cache, or we lost
            // the race between TryAddRentCount on this thread and
            // MarkForDispose on another thread doing cache cleanup.  In either
            // case, we need to create a new handle.

            handle = factory(factoryContext);
            handle.TryAddRentCount(); // The caler is the first renter

            THandle cached;
            do
            {
                cached = _cache.GetOrAdd(key, handle);
            }
            // If we get the same handle back, we successfully added it to the cache and we are done.
            // If we get a different handle back, we need to increase the rent count.
            // If we fail to add the rent count, then the existing/cached handle is in process of
            // being removed from the cache and we can try again, eventually either succeeding to
            // add our new handle or getting a fresh handle inserted by another thread meanwhile.
            while (cached != handle && !cached.TryAddRentCount());

            if (cached != handle)
            {
                // we lost a race with another thread to insert new handle into the cache
                if (NetEventSource.Log.IsEnabled())
                {
                    NetEventSource.Info(this, $"Discarding {handle} (preferring cached {cached}).");
                }

                // First dispose decrements the rent count we added before attempting the cache insertion
                // and second closes the handle
                handle.Dispose();
                handle.Dispose();
                Debug.Assert(handle.IsClosed);

                return cached;
            }

            CheckForCleanup();

            return handle;
        }

        private void CheckForCleanup()
        {
            // We check the cache size after every couple of insertions, and
            // discard all handles which are not being actively rented. This
            // should still be flexible enough to allow "stable set" of
            // arbitrary size, while still preventing unbounded growth.

            var count = _cache.Count;
            if (count % CheckExpiredModulo == 0)
            {
                // let only one thread perform cleanup at a time
                lock (_cache)
                {
                    // check again, if another thread just cleaned up (and cached count went down) we are unlikely
                    // to clean anything
                    if (_cache.Count >= count)
                    {
                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(this, $"Current size: {_cache.Count}.");
                        }

                        foreach ((TKey key, THandle handle) in _cache)
                        {
                            if (!handle.TryMarkForDispose())
                            {
                                // handle in use
                                continue;
                            }

                            // the handle is not in use and has been marked such that no new rents can be added.
                            if (NetEventSource.Log.IsEnabled())
                            {
                                NetEventSource.Info(this, $"Evicting cached {handle}.");
                            }

                            bool removed = _cache.TryRemove(key, out _);
                            Debug.Assert(removed);
                            handle.Dispose();

                            // Since the handle is not used anywhere, this should close the handle
                            Debug.Assert(handle.IsClosed);
                        }

                        if (NetEventSource.Log.IsEnabled())
                        {
                            NetEventSource.Info(this, $"New size: {_cache.Count}.");
                        }
                    }
                }
            }
        }
    }
}
