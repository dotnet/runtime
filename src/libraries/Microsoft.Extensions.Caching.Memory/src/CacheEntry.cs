// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal sealed partial class CacheEntry : ICacheEntry
    {
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;
        private static readonly AsyncLocal<CacheEntry?> _current = new AsyncLocal<CacheEntry?>();

        private readonly MemoryCache _cache;

        private CacheEntryTokens? _tokens; // might be null if user is not using the tokens or callbacks
        private TimeSpan _absoluteExpirationRelativeToNow;
        private TimeSpan _slidingExpiration;
        private long _size = NotSet;
        private CacheEntry? _previous; // this field is not null only before the entry is added to the cache and tracking is enabled
        private object? _value;
        private long _absoluteExpirationTicks = NotSet;
        private short _absoluteExpirationOffsetMinutes;
        private bool _isDisposed;
        private bool _isExpired;
        private bool _isValueSet;
        private byte _evictionReason;
        private byte _priority = (byte)CacheItemPriority.Normal;

        private const int NotSet = -1;

        internal CacheEntry(object key, MemoryCache memoryCache)
        {
            ThrowHelper.ThrowIfNull(key);
            ThrowHelper.ThrowIfNull(memoryCache);

            Key = key;
            _cache = memoryCache;
            if (memoryCache.TrackLinkedCacheEntries)
            {
                AsyncLocal<CacheEntry?> holder = _current;
                _previous = holder.Value;
                holder.Value = this;
            }
        }

        // internal for testing
        internal static CacheEntry? Current => _current.Value;

        internal long AbsoluteExpirationTicks
        {
            get => _absoluteExpirationTicks;
            set
            {
                _absoluteExpirationTicks = value;
                _absoluteExpirationOffsetMinutes = 0;
            }
        }

        DateTimeOffset? ICacheEntry.AbsoluteExpiration
        {
            get
            {
                if (_absoluteExpirationTicks < 0)
                    return null;

                var offset = new TimeSpan(_absoluteExpirationOffsetMinutes * TimeSpan.TicksPerMinute);
                return new DateTimeOffset(_absoluteExpirationTicks + offset.Ticks, offset);
            }
            set
            {
                if (value is null)
                {
                    _absoluteExpirationTicks = NotSet;
                    _absoluteExpirationOffsetMinutes = default;
                }
                else
                {
                    DateTimeOffset expiration = value.GetValueOrDefault();
                    _absoluteExpirationTicks = expiration.UtcTicks;
                    _absoluteExpirationOffsetMinutes = (short)(expiration.Offset.Ticks / TimeSpan.TicksPerMinute);
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
        [MemberNotNull(nameof(_tokens))]
        public IList<IChangeToken> ExpirationTokens => GetOrCreateTokens().ExpirationTokens;

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        [MemberNotNull(nameof(_tokens))]
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

                _size = value ?? NotSet;
            }
        }

        public object Key { get; }

        public object? Value
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
                    CommitWithTracking();
                }
                else if (_isValueSet)
                {
                    _cache.SetEntry(this);
                }
            }
        }

        private void CommitWithTracking()
        {
            Debug.Assert(_current.Value == this, "Entries disposed in invalid order");
            _current.Value = _previous;

            // Don't commit or propagate options if the CacheEntry Value was never set.
            // We assume an exception occurred causing the caller to not set the Value successfully,
            // so don't use this entry.
            if (_isValueSet)
            {
                _cache.SetEntry(this);

                CacheEntry? parent = _previous;
                if (parent != null)
                {
                    if ((ulong)_absoluteExpirationTicks < (ulong)parent._absoluteExpirationTicks)
                    {
                        parent._absoluteExpirationTicks = _absoluteExpirationTicks;
                        parent._absoluteExpirationOffsetMinutes = _absoluteExpirationOffsetMinutes;
                    }
                    _tokens?.PropagateTokens(parent);
                }
            }

            _previous = null; // we don't want to root unnecessary objects
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
            if (_absoluteExpirationTicks < 0 && _slidingExpiration.Ticks == 0)
            {
                return false;
            }

            return FullCheck(utcNow);

            bool FullCheck(DateTime utcNow)
            {
                if ((ulong)_absoluteExpirationTicks <= (ulong)utcNow.Ticks)
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
                var entry = (CacheEntry)state!;
                entry.SetExpired(EvictionReason.TokenExpired);
                entry._cache.EntryExpired(entry);
            }, obj, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        internal void InvokeEvictionCallbacks() => _tokens?.InvokeEvictionCallbacks(this);

        internal void PropagateOptionsToCurrent()
        {
            if ((_tokens == null || !_tokens.CanPropagateTokens()) && _absoluteExpirationTicks < 0 || _current.Value is not CacheEntry parent)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            if ((ulong)_absoluteExpirationTicks < (ulong)parent._absoluteExpirationTicks)
            {
                parent._absoluteExpirationTicks = _absoluteExpirationTicks;
                parent._absoluteExpirationOffsetMinutes = _absoluteExpirationOffsetMinutes;
            }

            _tokens?.PropagateTokens(parent);
        }

        [MemberNotNull(nameof(_tokens))]
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
