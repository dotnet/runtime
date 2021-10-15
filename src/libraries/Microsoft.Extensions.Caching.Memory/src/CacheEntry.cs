// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private TimeSpan _absoluteExpirationRelativeToNow;
        private TimeSpan _slidingExpiration;
        private long _size = -1;
        private CacheEntry _previous; // this field is not null only before the entry is added to the cache and tracking is enabled
        private object _value;
        private DateTime _absoluteExpiration;
        private short _absoluteExpirationOffsetMinutes;
        private bool _isDisposed;
        private bool _isExpired;
        private bool _isValueSet;
        private byte _evictionReason;
        private byte _priority;

        internal CacheEntry(object key, MemoryCache memoryCache)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _previous = memoryCache.TrackLinkedCacheEntries ? CacheEntryHelper.EnterScope(this) : null;
            _priority = (byte)CacheItemPriority.Normal;
        }

        internal bool HasAbsoluteExpiration => Unsafe.As<DateTime, long>(ref _absoluteExpiration) != 0;

        internal DateTime AbsoluteExpiration => _absoluteExpiration;

        internal void SetAbsoluteExpirationUtc(DateTime value)
        {
            Debug.Assert(value.Kind == DateTimeKind.Utc);
            _absoluteExpiration = value;
            _absoluteExpirationOffsetMinutes = 0;
        }

        DateTimeOffset? ICacheEntry.AbsoluteExpiration
        {
            get
            {
                if (!HasAbsoluteExpiration)
                    return null;

                var offset = new TimeSpan(_absoluteExpirationOffsetMinutes * TimeSpan.TicksPerMinute);
                return new DateTimeOffset(_absoluteExpiration.Ticks + offset.Ticks, offset);
            }
            set
            {
                if (value is null)
                {
                    _absoluteExpiration = default;
                    _absoluteExpirationOffsetMinutes = default;
                }
                else
                {
                    _absoluteExpiration = value.GetValueOrDefault().UtcDateTime;
                    _absoluteExpirationOffsetMinutes = (short)(value.GetValueOrDefault().Offset.Ticks / TimeSpan.TicksPerMinute);
                }
            }
        }

        internal TimeSpan AbsoluteExpirationRelativeToNow => _absoluteExpirationRelativeToNow;

        TimeSpan? ICacheEntry.AbsoluteExpirationRelativeToNow
        {
            get => _absoluteExpirationRelativeToNow.Ticks == 0 ? null : _absoluteExpirationRelativeToNow;
            set
            {
                // this method does not set AbsoluteExpiration as it would require calling Clock.UtcNow twice:
                // once here and once in MemoryCache.SetEntry

                if (value is { Ticks: <= 0 })
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(AbsoluteExpirationRelativeToNow),
                        value,
                        "The relative expiration value must be positive.");
                }

                _absoluteExpirationRelativeToNow = value.GetValueOrDefault();
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpiration.Ticks == 0 ? null : _slidingExpiration;
            set
            {
                if (value is { Ticks: <= 0 })
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(SlidingExpiration),
                        value,
                        "The sliding expiration value must be positive.");
                }

                _slidingExpiration = value.GetValueOrDefault();
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
        public CacheItemPriority Priority { get => (CacheItemPriority)_priority; set => _priority = (byte)value; }

        internal long Size => _size;

        long? ICacheEntry.Size
        {
            get => _size < 0 ? null : _size;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
                }

                // disallow entry size changes after it has been committed
                if (_isDisposed)
                    return;

                _size = value ?? -1;
            }
        }

        public object Key { get; private set; }

        public object Value
        {
            get => _value;
            set
            {
                _value = value;
                _isValueSet = true;
            }
        }

        internal DateTime LastAccessed { get; set; }

        internal EvictionReason EvictionReason { get => (EvictionReason)_evictionReason; private set => _evictionReason = (byte)value; }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;

                if (_cache.TrackLinkedCacheEntries)
                {
                    CacheEntryHelper.ExitScope(this, _previous);
                }

                // Don't commit or propagate options if the CacheEntry Value was never set.
                // We assume an exception occurred causing the caller to not set the Value successfully,
                // so don't use this entry.
                if (_isValueSet)
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
        internal bool CheckExpired(DateTime utcNow)
            => _isExpired
                || CheckForExpiredTime(utcNow)
                || (_tokens != null && _tokens.CheckForExpiredTokens(this));

        internal void SetExpired(EvictionReason reason)
        {
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            _isExpired = true;
            _tokens?.DetachTokens();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)] // added based on profiling
        private bool CheckForExpiredTime(DateTime utcNow)
        {
            if (!HasAbsoluteExpiration && _slidingExpiration.Ticks == 0)
            {
                return false;
            }

            return FullCheck(utcNow);

            bool FullCheck(DateTime utcNow)
            {
                if (HasAbsoluteExpiration && _absoluteExpiration <= utcNow)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                if (_slidingExpiration.Ticks > 0
                    && (utcNow - LastAccessed) >= _slidingExpiration)
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
        internal bool CanPropagateOptions() => (_tokens != null && _tokens.CanPropagateTokens()) || HasAbsoluteExpiration;

        internal void PropagateOptions(CacheEntry parent)
        {
            if (parent == null)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            _tokens?.PropagateTokens(parent);

            if (HasAbsoluteExpiration && (!parent.HasAbsoluteExpiration || _absoluteExpiration < parent._absoluteExpiration))
            {
                parent._absoluteExpiration = _absoluteExpiration;
                parent._absoluteExpirationOffsetMinutes = _absoluteExpirationOffsetMinutes;
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
