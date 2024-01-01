// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;

namespace System
{
    public interface IDefaultTimeProvider
    {
        /// <summary>
        /// Gets a <see cref="TimeZoneInfo"/> object that represents the local time zone according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        TimeZoneInfo LocalTimeZone { get; }
        /// <summary>
        /// Gets the frequency of <see cref="GetTimestamp"/> of high-frequency value per second.
        /// </summary>
        long TimestampFrequency { get; }
        /// <summary>
        /// Creates a new <see cref="ITimer"/> instance, using <see cref="TimeSpan"/> values to measure time intervals.
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns>The newly created <see cref="ITimer"/> instance.</returns>
        ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);
        /// <summary>
        /// Gets the current high-frequency value designed to measure small time intervals with high accuracy in the timer mechanism.
        /// </summary>
        /// <returns>A long integer representing the high-frequency counter value of the underlying timer mechanism.</returns>
        long GetTimestamp();
        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value whose date and time are set to the current
        /// Coordinated Universal Time (UTC) date and time and whose offset is Zero,
        /// all according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        /// <returns>The date and time in UTC.</returns>
        DateTimeOffset GetUtcNow();
    }
}
