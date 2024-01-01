// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;

namespace System
{
    public class DefaultTimeProvider : TimeProvider, IDefaultTimeProvider
    {
        /// <summary>
        /// Gets a <see cref="TimeZoneInfo"/> object that represents the local time zone according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see cref="TimeZoneInfo.Local"/>.
        /// </remarks>
        public override TimeZoneInfo LocalTimeZone => base.LocalTimeZone;
        /// <summary>
        /// Gets the frequency of <see cref="GetTimestamp"/> of high-frequency value per second.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see cref="Stopwatch.Frequency"/>. For a given TimeProvider instance, the value must be idempotent and remain unchanged.
        /// </remarks>
        public override long TimestampFrequency => base.TimestampFrequency;
        /// <summary>Creates a new <see cref="ITimer"/> instance, using <see cref="TimeSpan"/> values to measure time intervals.</summary>
        /// <param name="callback">
        /// A delegate representing a method to be executed when the timer fires. The method specified for callback should be reentrant,
        /// as it may be invoked simultaneously on two threads if the timer fires again before or while a previous callback is still being handled.
        /// </param>
        /// <param name="state">An object to be passed to the <paramref name="callback"/>. This may be null.</param>
        /// <param name="dueTime">The amount of time to delay before <paramref name="callback"/> is invoked. Specify <see cref="Timeout.InfiniteTimeSpan"/> to prevent the timer from starting. Specify <see cref="TimeSpan.Zero"/> to start the timer immediately.</param>
        /// <param name="period">The time interval between invocations of <paramref name="callback"/>. Specify <see cref="Timeout.InfiniteTimeSpan"/> to disable periodic signaling.</param>
        /// <returns>
        /// The newly created <see cref="ITimer"/> instance.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="callback"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The number of milliseconds in the value of <paramref name="dueTime"/> or <paramref name="period"/> is negative and not equal to <see cref="Timeout.Infinite"/>, or is greater than <see cref="int.MaxValue"/>.</exception>
        /// <remarks>
        /// <para>
        /// The delegate specified by the callback parameter is invoked once after <paramref name="dueTime"/> elapses, and thereafter each time the <paramref name="period"/> time interval elapses.
        /// </para>
        /// <para>
        /// If <paramref name="dueTime"/> is zero, the callback is invoked immediately. If <paramref name="dueTime"/> is -1 milliseconds, <paramref name="callback"/> is not invoked; the timer is disabled,
        /// but can be re-enabled by calling the <see cref="ITimer.Change"/> method.
        /// </para>
        /// <para>
        /// If <paramref name="period"/> is 0 or -1 milliseconds and <paramref name="dueTime"/> is positive, <paramref name="callback"/> is invoked once; the periodic behavior of the timer is disabled,
        /// but can be re-enabled using the <see cref="ITimer.Change"/> method.
        /// </para>
        /// <para>
        /// The return <see cref="ITimer"/> instance will be implicitly rooted while the timer is still scheduled.
        /// </para>
        /// <para>
        /// <see cref="CreateTimer"/> captures the <see cref="ExecutionContext"/> and stores that with the <see cref="ITimer"/> for use in invoking <paramref name="callback"/>
        /// each time it's called. That capture can be suppressed with <see cref="ExecutionContext.SuppressFlow"/>.
        /// </para>
        /// </remarks>
        public override ITimer CreateTimer(TimerCallback callback,
                                           object? state,
                                           TimeSpan dueTime,
                                           TimeSpan period)
        {
            return base.CreateTimer(callback,
                                    state,
                                    dueTime,
                                    period);
        }
        /// <summary>
        /// Gets the current high-frequency value designed to measure small time intervals with high accuracy in the timer mechanism.
        /// </summary>
        /// <returns>A long integer representing the high-frequency counter value of the underlying timer mechanism.</returns>
        public override long GetTimestamp()
        {
            return base.GetTimestamp();
        }
        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value whose date and time are set to the current
        /// Coordinated Universal Time (UTC) date and time and whose offset is Zero,
        /// all according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        /// <returns>The date and time in UTC.</returns>
        public override DateTimeOffset GetUtcNow()
        {
            return base.GetUtcNow();
        }
    }
}
