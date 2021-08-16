// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal sealed partial class CacheEntry : ICacheEntry
    {
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;

        private readonly MemoryCache _cache;

        private CacheEntryTokens _tokens; // might be null if user is not using the tokens or callbacks
        private TimeSpan? _absoluteExpirationRelativeToNow;
        private TimeSpan? _slidingExpiration;
        private long? _size;
        private CacheEntry _previous; // this field is not null only before the entry is added to the cache
        private object _value;
        private CacheEntryState _state;

        internal CacheEntry(object key, MemoryCache memoryCache)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _previous = CacheEntryHelper.EnterScope(this);
            _state = new CacheEntryState(CacheItemPriority.Normal);
        }

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration { get; set; }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => _absoluteExpirationRelativeToNow;
            set
            {
                // this method does not set AbsoluteExpiration as it would require calling Clock.UtcNow twice:
                // once here and once in MemoryCache.SetEntry

                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(AbsoluteExpirationRelativeToNow),
                        value,
                        "The relative expiration value must be positive.");
                }

                _absoluteExpirationRelativeToNow = value;
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpiration;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(SlidingExpiration),
                        value,
                        "The sliding expiration value must be positive.");
                }

                _slidingExpiration = value;
            }
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens => GetOrCreateTokens().ExpirationTokens;

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => GetOrCreateTokens().PostEvictionCallbacks;

        /// <summary>
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        /// memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        public CacheItemPriority Priority { get => _state.Priority; set => _state.Priority = value; }

        /// <summary>
        /// Gets or sets the size of the cache entry value.
        /// </summary>
        public long? Size
        {
            get => _size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                _size = value;
            }
        }

        public object Key { get; private set; }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                _state.IsValueSet = true;
            }
        }

        internal DateTimeOffset LastAccessed { get; set; }

        internal EvictionReason EvictionReason { get => _state.EvictionReason; private set => _state.EvictionReason = value; }

        public void Dispose()
        {
            if (!_state.IsDisposed)
            {
                _state.IsDisposed = true;

                CacheEntryHelper.ExitScope(this, _previous);

                // Don't commit or propagate options if the CacheEntry Value was never set.
                // We assume an exception occurred causing the caller to not set the Value successfully,
                // so don't use this entry.
                if (_state.IsValueSet)
                {
                    _cache.SetEntry(this);

                    if (_previous != null && CanPropagateOptions())
                    {
                        PropagateOptions(_previous);
                    }
                }

                _previous = null; // we don't want to root unnecessary objects
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        internal bool CheckExpired(in DateTimeOffset now)
            => _state.IsExpired
                || CheckForExpiredTime(now)
                || (_tokens != null && _tokens.CheckForExpiredTokens(this));

        internal void SetExpired(EvictionReason reason)
        {
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            _state.IsExpired = true;
            _tokens?.DetachTokens();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        private bool CheckForExpiredTime(in DateTimeOffset now)
        {
            if (!AbsoluteExpiration.HasValue && !_slidingExpiration.HasValue)
            {
                return false;
            }

            return FullCheck(now);

            bool FullCheck(in DateTimeOffset offset)
            {
                if (AbsoluteExpiration.HasValue && AbsoluteExpiration.Value <= offset)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                if (_slidingExpiration.HasValue
                    && (offset - LastAccessed) >= _slidingExpiration)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                return false;
            }
        }

        internal void AttachTokens() => _tokens?.AttachTokens(this);

        private static void ExpirationTokensExpired(object obj)
        {
            // start a new thread to avoid issues with callbacks called from RegisterChangeCallback
            Task.Factory.StartNew(state =>
            {
                var entry = (CacheEntry)state;
                entry.SetExpired(EvictionReason.TokenExpired);
                entry._cache.EntryExpired(entry);
            }, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        internal void InvokeEvictionCallbacks() => _tokens?.InvokeEvictionCallbacks(this);

        // this simple check very often allows us to avoid expensive call to PropagateOptions(CacheEntryHelper.Current)
        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        internal bool CanPropagateOptions() => (_tokens != null && _tokens.CanPropagateTokens()) || AbsoluteExpiration.HasValue;

        internal void PropagateOptions(CacheEntry parent)
        {
            if (parent == null)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            _tokens?.PropagateTokens(parent);

            if (AbsoluteExpiration.HasValue)
            {
                if (!parent.AbsoluteExpiration.HasValue || AbsoluteExpiration < parent.AbsoluteExpiration)
                {
                    parent.AbsoluteExpiration = AbsoluteExpiration;
                }
            }
        }

        private CacheEntryTokens GetOrCreateTokens()
        {
            if (_tokens != null)
            {
                return _tokens;
            }

            CacheEntryTokens result = new CacheEntryTokens();
            return Interlocked.CompareExchange(ref _tokens, result, null) ?? result;
        }
    }
}
