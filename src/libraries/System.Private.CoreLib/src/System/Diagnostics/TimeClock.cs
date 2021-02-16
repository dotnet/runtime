// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Provides an abstraction for a clock which is used to retrieve the current time.
    /// </summary>
    public abstract class TimeClock
    {
        /// <summary>
        /// Gets the current time from the clock instance, as a <see cref="DateTimeOffset"/>,
        /// in terms of Coordinated Universal Time (UTC).
        /// </summary>
        /// <returns>
        /// A <see cref="DateTimeOffset"/> value representing the current UTC time.
        /// The value will always have a zero offset.
        /// </returns>
        public DateTimeOffset GetCurrentUtcDateTimeOffset()
        {
            DateTimeOffset value = GetCurrentUtcDateTimeOffsetImpl();
            return value.Offset == TimeSpan.Zero ? value : value.ToUniversalTime();
        }

        /// <summary>
        /// Gets the current time from the clock instance, as a <see cref="DateTime"/>,
        /// in terms of Coordinated Universal Time (UTC).
        /// </summary>
        /// <returns>
        /// A <see cref="DateTime"/> value representing the current UTC time.
        /// The value will always have a kind of <see cref="DateTimeKind.Utc"/>.
        /// </returns>
        public DateTime GetCurrentUtcDateTime() => GetCurrentUtcDateTimeImpl();

        /// <summary>
        /// Provides the implementation logic for the clock instance to return the current time.
        /// </summary>
        /// <returns>
        /// A <see cref="DateTimeOffset"/> value representing the current time from the clock instance.
        /// </returns>
        protected abstract DateTimeOffset GetCurrentUtcDateTimeOffsetImpl();

        /// <summary>
        /// Provides the implementation logic for the clock instance to return the current time.
        /// </summary>
        /// <returns>
        /// A <see cref="DateTime"/> value representing the current time from the clock instance.
        /// </returns>
        /// <remarks>
        /// This method is overriden internally by the <see cref="ActualSystemClock"/> only, for performance benefits.
        /// All other clocks will implement only the <see cref="GetCurrentUtcDateTimeOffsetImpl"/> method.
        /// </remarks>
        internal virtual DateTime GetCurrentUtcDateTimeImpl() => GetCurrentUtcDateTimeOffsetImpl().UtcDateTime;
    }
}
