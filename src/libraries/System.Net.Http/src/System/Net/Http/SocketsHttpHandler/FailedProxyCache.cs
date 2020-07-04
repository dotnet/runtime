// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace System.Net.Http
{
    /// <summary>
    /// Holds a cache of failing proxies and manages when they should be retried.
    /// </summary>
    internal sealed class FailedProxyCache
    {
        /// <summary>
        /// When returned by <see cref="GetProxyRenewTicks"/>, indicates a proxy is immediately usable.
        /// </summary>
        public const long Immediate = 0;

        // If a proxy fails, time out 30 minutes. WinHTTP and Firefox both use this.
        private const int FailureTimeoutInMilliseconds = 1000 * 60 * 30;

        // Scan through the failures and flush any that have expired every 5 minutes.
        private const int FlushFailuresTimerInMilliseconds = 1000 * 60 * 5;

        // _failedProxies will only be flushed (rare but somewhat expensive) if we have more than this number of proxies in our dictionary. See Cleanup() for details.
        private const int LargeProxyConfigBoundary = 8;

        // Value is the Environment.TickCount64 to remove the proxy from the failure list.
        private readonly ConcurrentDictionary<Uri, long> _failedProxies = new ConcurrentDictionary<Uri, long>();

        // When Environment.TickCount64 >= _nextFlushTicks, cause a flush.
        private long _nextFlushTicks = Environment.TickCount64 + FlushFailuresTimerInMilliseconds;

        // This lock can be folded into _nextFlushTicks for space optimization, but
        // this class should only have a single instance so would rather have clarity.
        private SpinLock _flushLock = new SpinLock(enableThreadOwnerTracking: false); // mutable struct; do not make this readonly

        /// <summary>
        /// Checks when a proxy will become usable.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> of the proxy to check.</param>
        /// <returns>If the proxy can be used, <see cref="Immediate"/>. Otherwise, the next <see cref="Environment.TickCount64"/> that it should be used.</returns>
        public long GetProxyRenewTicks(Uri uri)
        {
            Cleanup();

            // If not failed, ready immediately.
            if (!_failedProxies.TryGetValue(uri, out long renewTicks))
            {
                return Immediate;
            }

            // If we haven't reached out renew time, the proxy can't be used.
            if (Environment.TickCount64 < renewTicks)
            {
                return renewTicks;
            }

            // Renew time reached, we can remove the proxy from the cache.
            if (TryRenewProxy(uri, renewTicks))
            {
                return Immediate;
            }

            // Another thread updated the cache before we could remove it.
            // We can't know if this is a removal or an update, so check again.
            return _failedProxies.TryGetValue(uri, out renewTicks) ? renewTicks : Immediate;
        }

        /// <summary>
        /// Sets a proxy as failed, to avoid trying it again for some time.
        /// </summary>
        /// <param name="uri">The URI of the proxy.</param>
        public void SetProxyFailed(Uri uri)
        {
            _failedProxies[uri] = Environment.TickCount64 + FailureTimeoutInMilliseconds;
            Cleanup();
        }

        /// <summary>
        /// Renews a proxy prior to its period expiring. Used when all proxies are failed to renew the proxy closest to being renewed.
        /// </summary>
        /// <param name="uri">The <paramref name="uri"/> of the proxy to renew.</param>
        /// <param name="renewTicks">The current renewal time for the proxy. If the value has changed from this, the proxy will not be renewed.</param>
        public bool TryRenewProxy(Uri uri, long renewTicks) =>
            _failedProxies.TryRemove(new KeyValuePair<Uri, long>(uri, renewTicks));

        /// <summary>
        /// Cleans up any old proxies that should no longer be marked as failing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Cleanup()
        {
            if (_failedProxies.Count > LargeProxyConfigBoundary && Environment.TickCount64 >= Interlocked.Read(ref _nextFlushTicks))
            {
                CleanupHelper();
            }
        }

        /// <summary>
        /// Cleans up any old proxies that should no longer be marked as failing.
        /// </summary>
        /// <remarks>
        /// I expect this to never be called by <see cref="Cleanup"/> in a production system. It is only needed in the case
        /// that a system has a very large number of proxies that the PAC script cycles through. It is moderately expensive,
        /// so it's only run periodically and is disabled until we exceed <see cref="LargeProxyConfigBoundary"/> failed proxies.
        /// </remarks>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void CleanupHelper()
        {
            bool lockTaken = false;
            try
            {
                _flushLock.TryEnter(ref lockTaken);
                if (!lockTaken)
                {
                    return;
                }

                long curTicks = Environment.TickCount64;

                foreach (KeyValuePair<Uri, long> kvp in _failedProxies)
                {
                    if (curTicks >= kvp.Value)
                    {
                        ((ICollection<KeyValuePair<Uri, long>>)_failedProxies).Remove(kvp);
                    }
                }
            }
            finally
            {
                if (lockTaken)
                {
                    Interlocked.Exchange(ref _nextFlushTicks, Environment.TickCount64 + FlushFailuresTimerInMilliseconds);
                    _flushLock.Exit(false);
                }
            }
        }
    }
}
