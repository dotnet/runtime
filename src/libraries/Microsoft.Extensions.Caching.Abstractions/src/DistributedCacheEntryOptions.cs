// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Caching.Distributed
{
    /// <summary>
    /// Provides the cache options for an entry in <see cref="IDistributedCache"/>.
    /// </summary>
    public class DistributedCacheEntryOptions
    {
        private DateTimeOffset _absoluteExpiration;
        private bool _absoluteExpirationSet;
        private TimeSpan _absoluteExpirationRelativeToNow;
        private bool _absoluteExpirationRelativeToNowSet;
        private TimeSpan _slidingExpiration;
        private bool _slidingExpirationSet;
        private bool _frozen;

        /// <summary>
        /// Gets or sets an absolute expiration date for the cache entry.
        /// </summary>
        public DateTimeOffset? AbsoluteExpiration
        {
            get => _absoluteExpirationSet ? _absoluteExpiration : null;
            set => Set(ref _absoluteExpiration, ref _absoluteExpirationSet, value);
        }

        /// <summary>
        /// Gets or sets an absolute expiration time, relative to now.
        /// </summary>
        public TimeSpan? AbsoluteExpirationRelativeToNow
        {
            get => _absoluteExpirationRelativeToNowSet ? _absoluteExpirationRelativeToNow : null;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(AbsoluteExpirationRelativeToNow),
                        value,
                        "The relative expiration value must be positive.");
                }

                Set(ref _absoluteExpirationRelativeToNow, ref _absoluteExpirationRelativeToNowSet, value);
            }
        }

        /// <summary>
        /// Gets or sets how long a cache entry can be inactive (for example, not accessed) before it will be removed.
        /// This will not extend the entry lifetime beyond the absolute expiration (if set).
        /// </summary>
        public TimeSpan? SlidingExpiration
        {
            get => _slidingExpirationSet ? _slidingExpiration : null;
            set
            {
                if (value <= TimeSpan.Zero)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(SlidingExpiration),
                        value,
                        "The sliding expiration value must be positive.");
                }
                Set(ref _slidingExpiration, ref _slidingExpirationSet, value);
            }
        }

        internal DistributedCacheEntryOptions Freeze()
        {
            _frozen = true;
            return this;
        }

        private void Set<T>(ref T field, ref bool isSet, in T? value) where T : struct
        {
            if (_frozen)
            {
                ThrowFrozen();
            }

            field = value.GetValueOrDefault();
            isSet = value.HasValue;

            static void ThrowFrozen() => throw new InvalidOperationException("This instance has been frozen and cannot be mutated");
        }
    }
}
