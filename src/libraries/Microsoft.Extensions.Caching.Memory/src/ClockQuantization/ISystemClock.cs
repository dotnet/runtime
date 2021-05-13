// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// Abstracts the system clock to facilitate synthetic clocks (e.g. for replay or testing).
    /// </summary>
    internal interface ISystemClock
    {
        /// <value>
        /// The current system time in UTC.
        /// </value>
        DateTimeOffset UtcNow { get; }

#if NET5_0 || NET5_0_OR_GREATER
        /// <value>
        /// An offset (in ticks) representing the current system time in UTC.
        /// </value>
        /// <remarks>
        /// Depending on implementation this may be an absolute value based on <see cref="DateTimeOffset.UtcTicks"/>, a relative value based on <see cref="Environment.TickCount64"/> etc.
        /// </remarks>
#else
        /// <value>
        /// An offset (in ticks) representing the current system time in UTC.
        /// </value>
        /// <remarks>
        /// Depending on implementation this may be an absolute value based on <see cref="DateTimeOffset.UtcTicks"/> etc.
        /// </remarks>
#endif
        long UtcNowClockOffset { get; }

        /// <value>Represents the number of offset units (ticks) in 1 millisecond</value>
        /// <seealso cref="UtcNowClockOffset"/>
        /// <seealso cref="ClockOffsetToUtcDateTimeOffset"/>
        /// <seealso cref="DateTimeOffsetToClockOffset"/>
        long ClockOffsetUnitsPerMillisecond { get; }

        /// <summary>
        /// Converts clock-specific <paramref name="offset"/> to a <see cref="DateTimeOffset"/> in UTC.
        /// </summary>
        /// <param name="offset">The offset to convert</param>
        /// <returns>The corresponding <see cref="DateTimeOffset"/></returns>
        DateTimeOffset ClockOffsetToUtcDateTimeOffset(long offset);

        /// <summary>
        /// Converts <paramref name="offset"/> to a clock-specific offset.
        /// </summary>
        /// <param name="offset">The offset to convert</param>
        /// <returns>The corresponding clock-specific offset</returns>
        long DateTimeOffsetToClockOffset(DateTimeOffset offset);
    }
}

#nullable restore
