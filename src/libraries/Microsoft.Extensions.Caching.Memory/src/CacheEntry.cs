// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Caching.Memory
{
    internal class CacheEntry : ICacheEntry
    {
        private static readonly Action<object> ExpirationCallback = ExpirationTokensExpired;

        private readonly object _lock = new object();
        private readonly MemoryCache _cache;

        private IList<IDisposable> _expirationTokenRegistrations;
        private IList<PostEvictionCallbackRegistration> _postEvictionCallbacks;
        private IList<IChangeToken> _expirationTokens;
        private TimeSpan? _slidingExpiration;
        private long? _size;
        private IDisposable _scope;
        private object _value;
        private State _state;

        internal CacheEntry(object key, MemoryCache memoryCache)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _scope = CacheEntryHelper.EnterScope(this);
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
            get => AbsoluteExpiration.HasValue ? AbsoluteExpiration.Value - _cache.GetUtcNow() : null;
            set
            {
                if (value.HasValue)
                {
                    AbsoluteExpiration = value.Value > TimeSpan.Zero
                        ? (_cache.GetUtcNow() + value)
                        : throw new ArgumentOutOfRangeException(nameof(AbsoluteExpirationRelativeToNow), value, "The relative expiration value must be positive.");
                }
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (e.g. not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpiration;
            set => _slidingExpiration =  !value.HasValue || value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(SlidingExpiration), value, "The sliding expiration value must be positive.");
        }

        /// <summary>
        /// Gets the <see cref="IChangeToken"/> instances which cause the cache entry to expire.
        /// </summary>
        public IList<IChangeToken> ExpirationTokens => _expirationTokens ??= new List<IChangeToken>();

        /// <summary>
        /// Gets or sets the callbacks will be fired after the cache entry is evicted from the cache.
        /// </summary>
        public IList<PostEvictionCallbackRegistration> PostEvictionCallbacks => _postEvictionCallbacks ??= new List<PostEvictionCallbackRegistration>();

        /// <summary>
        /// Gets or sets the priority for keeping the cache entry in the cache during a
        /// memory pressure triggered cleanup. The default is <see cref="CacheItemPriority.Normal"/>.
        /// </summary>
        public CacheItemPriority Priority { get; set; } = CacheItemPriority.Normal;

        /// <summary>
        /// Gets or sets the size of the cache entry value.
        /// </summary>
        public long? Size
        {
            get => _size;
            set => _size = !value.HasValue || value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be non-negative.");
        }

        public object Key { get; private set; }

        public object Value
        {
            get => _value;
            set { _value = value; IsValueSet = true; }
        }

        internal DateTimeOffset LastAccessed { get; set; }

        internal EvictionReason EvictionReason { get; private set; }

        private bool IsDisposed { get => _state.HasFlag(State.IsDisposed); set => Set(State.IsDisposed, value); }

        private bool IsExpired { get => _state.HasFlag(State.IsExpired); set => Set(State.IsExpired, value); }

        private bool IsValueSet { get => _state.HasFlag(State.IsValueSet); set => Set(State.IsValueSet, value); }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;

                // Ensure the _scope reference is cleared because it can reference other CacheEntry instances.
                // This CacheEntry is going to be put into a MemoryCache, and we don't want to root unnecessary objects.
                _scope.Dispose();
                _scope = null;

                // Don't commit or propagate options if the CacheEntry Value was never set.
                // We assume an exception occurred causing the caller to not set the Value successfully,
                // so don't use this entry.
                if (IsValueSet)
                {
                    _cache.SetEntry(this);

                    if (CanPropagateOptions())
                    {
                        PropagateOptions(CacheEntryHelper.Current);
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool CheckExpired(in DateTimeOffset now) => IsExpired || CheckForExpiredTime(now) || CheckForExpiredTokens();

        internal void SetExpired(EvictionReason reason)
        {
            if (EvictionReason == EvictionReason.None)
            {
                EvictionReason = reason;
            }
            IsExpired = true;
            DetachTokens();
        }

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

        private bool CheckForExpiredTokens()
        {
            if (_expirationTokens == null)
            {
                return false;
            }

            return CheckTokens();

            bool CheckTokens()
            {
                for (int i = 0; i < _expirationTokens.Count; i++)
                {
                    IChangeToken expiredToken = _expirationTokens[i];
                    if (expiredToken.HasChanged)
                    {
                        SetExpired(EvictionReason.TokenExpired);
                        return true;
                    }
                }
                return false;
            }
        }

        internal void AttachTokens()
        {
            if (_expirationTokens != null)
            {
                lock (_lock)
                {
                    for (int i = 0; i < _expirationTokens.Count; i++)
                    {
                        IChangeToken expirationToken = _expirationTokens[i];
                        if (expirationToken.ActiveChangeCallbacks)
                        {
                            if (_expirationTokenRegistrations == null)
                            {
                                _expirationTokenRegistrations = new List<IDisposable>(1);
                            }
                            IDisposable registration = expirationToken.RegisterChangeCallback(ExpirationCallback, this);
                            _expirationTokenRegistrations.Add(registration);
                        }
                    }
                }
            }
        }

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

        private void DetachTokens()
        {
            if (_expirationTokenRegistrations != null)
            {
                lock (_lock)
                {
                    IList<IDisposable> registrations = _expirationTokenRegistrations;
                    if (registrations != null)
                    {
                        _expirationTokenRegistrations = null;
                        for (int i = 0; i < registrations.Count; i++)
                        {
                            IDisposable registration = registrations[i];
                            registration.Dispose();
                        }
                    }
                }
            }
        }

        internal void InvokeEvictionCallbacks()
        {
            if (_postEvictionCallbacks != null)
            {
                Task.Factory.StartNew(state => InvokeCallbacks((CacheEntry)state), this,
                    CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            }
        }

        private static void InvokeCallbacks(CacheEntry entry)
        {
            IList<PostEvictionCallbackRegistration> callbackRegistrations = Interlocked.Exchange(ref entry._postEvictionCallbacks, null);

            if (callbackRegistrations == null)
            {
                return;
            }

            for (int i = 0; i < callbackRegistrations.Count; i++)
            {
                PostEvictionCallbackRegistration registration = callbackRegistrations[i];

                try
                {
                    registration.EvictionCallback?.Invoke(entry.Key, entry.Value, entry.EvictionReason, registration.State);
                }
                catch (Exception e)
                {
                    // This will be invoked on a background thread, don't let it throw.
                    entry._cache._logger.LogError(e, "EvictionCallback invoked failed");
                }
            }
        }

        internal bool CanPropagateOptions() => _expirationTokens != null || AbsoluteExpiration.HasValue;

        internal void PropagateOptions(CacheEntry parent)
        {
            if (parent == null)
            {
                return;
            }

            // Copy expiration tokens and AbsoluteExpiration to the cache entries hierarchy.
            // We do this regardless of it gets cached because the tokens are associated with the value we'll return.
            if (_expirationTokens != null)
            {
                lock (_lock)
                {
                    lock (parent._lock)
                    {
                        foreach (IChangeToken expirationToken in _expirationTokens)
                        {
                            parent.AddExpirationToken(expirationToken);
                        }
                    }
                }
            }

            if (AbsoluteExpiration.HasValue)
            {
                if (!parent.AbsoluteExpiration.HasValue || AbsoluteExpiration < parent.AbsoluteExpiration)
                {
                    parent.AbsoluteExpiration = AbsoluteExpiration;
                }
            }
        }

        private void Set(State option, bool value) => _state = value ? (_state | option) : (_state & ~option);

        [Flags]
        private enum State
        {
            Default = 0,
            IsValueSet = 1 << 0,
            IsExpired = 1 << 1,
            IsDisposed = 1 << 2,
        }
    }
}
