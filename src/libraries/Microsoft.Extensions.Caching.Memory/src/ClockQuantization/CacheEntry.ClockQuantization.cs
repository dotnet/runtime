// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Caching.Memory
{
    internal partial class CacheEntry : ICacheEntry
    {
        private long? _slidingExpirationClockOffsetUnits;


        private Internal.ClockQuantization.LazyClockOffsetSerialPosition _lastAccessedClockOffsetSerialPosition;
        internal Internal.ClockQuantization.LazyClockOffsetSerialPosition LastAccessedClockOffsetSerialPosition
        {
            get => _lastAccessedClockOffsetSerialPosition;
            set
            {
                _lastAccessedClockOffsetSerialPosition = value;
            }
        }


        private long? _absoluteExpirationClockOffset;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool CheckExpired(ref Internal.ClockQuantization.LazyClockOffsetSerialPosition now)
            => _state.IsExpired
                || CheckForExpiredTime(ref now)
                || (_tokens != null && _tokens.CheckForExpiredTokens(this));


        private bool CheckForExpiredTime(ref Internal.ClockQuantization.LazyClockOffsetSerialPosition now)
        {
            var absoluteExpirationUndecided = AbsoluteExpiration.HasValue;

            if (!absoluteExpirationUndecided)
            {
                if (!_slidingExpirationClockOffsetUnits.HasValue)
                {
                    return false;
                }
            }

            var slidingExpirationUndecided = _slidingExpirationClockOffsetUnits.HasValue;

            if (now.IsExact)
            {
                return ExactFullCheckForExpiredTime(now.ClockOffset, absoluteExpirationUndecided, slidingExpirationUndecided);
            }

            return IntervalBasedFullCheckForExpiredTime(ref now, absoluteExpirationUndecided, slidingExpirationUndecided);
        }

        private bool ExactFullCheckForExpiredTime(long offset, bool absoluteExpirationUndecided, bool slidingExpirationUndecided)
        {
            if (absoluteExpirationUndecided && offset >= _absoluteExpirationClockOffset!.Value)
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            if (slidingExpirationUndecided && ((offset - _lastAccessedClockOffsetSerialPosition.ClockOffset) >= _slidingExpirationClockOffsetUnits!.Value))
            {
                SetExpired(EvictionReason.Expired);
                return true;
            }

            return false;
        }

        private bool IntervalBasedFullCheckForExpiredTime(ref Internal.ClockQuantization.LazyClockOffsetSerialPosition position, bool absoluteExpirationUndecided, bool slidingExpirationUndecided)
        {
            var quantizer = _cache.ClockQuantizer;
            var referenceOffset = position.HasValue ? position.ClockOffset : quantizer.CurrentInterval!.ClockOffset;
            var nextMetronomicOffset = quantizer.NextMetronomicClockOffset!.Value;

            // Relatively cheap tests based on current clock interval
            var absoluteExpiresAtOffset = default(long);
            if (absoluteExpirationUndecided)
            {
                absoluteExpiresAtOffset = _absoluteExpirationClockOffset!.Value;
                if (IntervalCheckForExpiredTime(nextMetronomicOffset, referenceOffset, absoluteExpiresAtOffset, ref absoluteExpirationUndecided))
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }
            }

            // Relatively cheap tests based on current clock interval
            var slidingExpiresAtOffset = default(long);
            if (slidingExpirationUndecided)
            {
                slidingExpiresAtOffset = _lastAccessedClockOffsetSerialPosition.ClockOffset + _slidingExpirationClockOffsetUnits!.Value;
                if (IntervalCheckForExpiredTime(nextMetronomicOffset, referenceOffset, slidingExpiresAtOffset, ref slidingExpirationUndecided))
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }
            }

            if (absoluteExpirationUndecided || slidingExpirationUndecided)
            {
                // If still in doubt about anything, we must bite the bullet and fetch an exact timestamp through the clock (a system call, but the least expensive option)
                var now = quantizer.UtcNowClockOffset;

                if (absoluteExpirationUndecided && absoluteExpiresAtOffset <= now)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }

                if (slidingExpirationUndecided && slidingExpiresAtOffset <= now)
                {
                    SetExpired(EvictionReason.Expired);
                    return true;
                }
            }

            // We need an exact position for unexpired entries with sliding expiration (to properly update last accessed) and ensure proper LRU ordering as we go.
            if (_slidingExpirationClockOffsetUnits.HasValue)
            {
                quantizer.EnsureInitializedExactClockOffsetSerialPosition(ref position, advance: true);
            }

            return false;
        }

        /// <summary>
        /// The workhorse logic that aims to make as many time-based decisions as possible, solely based on expiration offset, interval boundaries and expiration policy
        /// </summary>
        /// <param name="nextMetronomicOffset">The upper boundary of the interval</param>
        /// <param name="referenceOffset">The lower boundary of the interval</param>
        /// <param name="expiresAtOffset"></param>
        /// <param name="expirationUndecided">Indicated if a conclusion was already reached; typically <see langword="true" /> when an item expires sometime within the interval being expected.</param>
        /// <returns><see langword="true"/> if expired, <see langword="false"/> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IntervalCheckForExpiredTime(long nextMetronomicOffset, long referenceOffset, long expiresAtOffset, ref bool expirationUndecided)
        {
            expirationUndecided = false;

            if (referenceOffset >= expiresAtOffset)
            {
                // Expired (before start of interval being inspected) - capturing all non-pessimistic cases
                return true;
            }

            // Undecided (expiration sometime within interval being inspected) - more work needed to reach conclusion under precise expiration policy
            expirationUndecided = nextMetronomicOffset > expiresAtOffset;

            // We conclude that the item did not expire yet (or that we don't know yet, if expiration undecided)
            return false;
        }
    }
}

#nullable restore
