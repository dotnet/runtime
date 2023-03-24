// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
    /// <summary>Provides an abstraction for time.</summary>
    public abstract class TimeProvider
    {
        private readonly double _timeToTicksRatio;

        /// <summary>
        /// Gets a <see cref="TimeProvider"/> that provides a clock based on <see cref="DateTimeOffset.UtcNow"/>,
        /// a time zone based on <see cref="TimeZoneInfo.Local"/>, a high-performance time stamp based on <see cref="Stopwatch"/>,
        /// and a timer based on <see cref="Timer"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="TimeZoneInfo.Local"/> changes after the object is returned, the change will be reflected in any subsequent operations that retrieve <see cref="TimeProvider.LocalNow"/>.
        /// </remarks>
        public static TimeProvider System { get; } = new SystemTimeProvider(null);

        /// <summary>
        /// Initializes the instance with the timestamp frequency.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value of <paramref name="timestampFrequency"/> is negative or zero.</exception>
        /// <param name="timestampFrequency">Frequency of the values returned from <see cref="GetTimestamp"/> method.</param>
        protected TimeProvider(long timestampFrequency)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timestampFrequency);
            TimestampFrequency = timestampFrequency;
            _timeToTicksRatio = (double)TimeSpan.TicksPerSecond / TimestampFrequency;
        }

        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value whose date and time are set to the current
        /// Coordinated Universal Time (UTC) date and time and whose offset is Zero,
        /// all according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        public abstract DateTimeOffset UtcNow { get; }

        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value that is set to the current date and time according to this <see cref="TimeProvider"/>'s
        /// notion of time based on <see cref="UtcNow"/>, with the offset set to the <see cref="LocalTimeZone"/>'s offset from Coordinated Universal Time (UTC).
        /// </summary>
        public DateTimeOffset LocalNow
        {
            get
            {
                DateTime utcDateTime = UtcNow.UtcDateTime;
                TimeSpan offset = LocalTimeZone.GetUtcOffset(utcDateTime);

                long localTicks = utcDateTime.Ticks + offset.Ticks;
                if ((ulong)localTicks > DateTime.MaxTicks)
                {
                    localTicks = localTicks < DateTime.MinTicks ? DateTime.MinTicks : DateTime.MaxTicks;
                }

                return new DateTimeOffset(localTicks, offset);
            }
        }

        /// <summary>
        /// Gets a <see cref="TimeZoneInfo"/> object that represents the local time zone according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        public abstract TimeZoneInfo LocalTimeZone { get; }

        /// <summary>
        /// Gets the frequency of <see cref="GetTimestamp"/> of high-frequency value per second.
        /// </summary>
        public long TimestampFrequency { get; }

        /// <summary>
        /// Creates a <see cref="TimeProvider"/> that provides a clock based on <see cref="DateTimeOffset.UtcNow"/>,
        /// a time zone based on <paramref name="timeZone"/>, a high-performance time stamp based on <see cref="Stopwatch"/>,
        /// and a timer based on <see cref="Timer"/>.
        /// </summary>
        /// <param name="timeZone">The time zone to use in getting the local time using <see cref="LocalNow"/>. </param>
        /// <returns>A new instance of <see cref="TimeProvider"/>. </returns>
        /// <exception cref="ArgumentNullException"><paramref name="timeZone"/> is null.</exception>
        public static TimeProvider FromLocalTimeZone(TimeZoneInfo timeZone)
        {
            ArgumentNullException.ThrowIfNull(timeZone);
            return new SystemTimeProvider(timeZone);
        }

        /// <summary>
        /// Gets the current high-frequency value designed to measure small time intervals with high accuracy in the timer mechanism.
        /// </summary>
        /// <returns>A long integer representing the high-frequency counter value of the underlying timer mechanism. </returns>
        public abstract long GetTimestamp();

        /// <summary>
        /// Gets the elapsed time between two timestamps retrieved using <see cref="GetTimestamp"/>.
        /// </summary>
        /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
        /// <param name="endingTimestamp">The timestamp marking the end of the time period.</param>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</returns>
        public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp) =>
            new TimeSpan((long)((endingTimestamp - startingTimestamp) * _timeToTicksRatio));

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
        public abstract ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period);

        /// <summary>
        /// Provides a default implementation of <see cref="TimeProvider"/> based on <see cref="DateTimeOffset.UtcNow"/>,
        /// <see cref="TimeZoneInfo.Local"/>, <see cref="Stopwatch"/>, and <see cref="Timer"/>.
        /// </summary>
        private sealed class SystemTimeProvider : TimeProvider
        {
            /// <summary>The time zone to treat as local.  If null, <see cref="TimeZoneInfo.Local"/> is used.</summary>
            private readonly TimeZoneInfo? _localTimeZone;

            /// <summary>Initializes the instance.</summary>
            /// <param name="localTimeZone">The time zone to treat as local.  If null, <see cref="TimeZoneInfo.Local"/> is used.</param>
            internal SystemTimeProvider(TimeZoneInfo? localTimeZone) : base(Stopwatch.Frequency) => _localTimeZone = localTimeZone;

            /// <inheritdoc/>
            public override TimeZoneInfo LocalTimeZone => _localTimeZone ?? TimeZoneInfo.Local;

            /// <inheritdoc/>
            public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
            {
                ArgumentNullException.ThrowIfNull(callback);
                return new SystemTimeProviderTimer(dueTime, period, callback, state);
            }

            /// <inheritdoc/>
            public override long GetTimestamp() => Stopwatch.GetTimestamp();

            /// <inheritdoc/>
            public override DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

            /// <summary>Thin wrapper for a <see cref="TimerQueueTimer"/>.</summary>
            /// <remarks>
            /// We don't return a TimerQueueTimer directly as it implements IThreadPoolWorkItem and we don't
            /// want it exposed in a way that user code could directly queue the timer to the thread pool.
            /// We also use this instead of Timer because CreateTimer needs to return a timer that's implicitly
            /// rooted while scheduled.
            /// </remarks>
            private sealed class SystemTimeProviderTimer : ITimer
            {
                private readonly TimerQueueTimer _timer;

                public SystemTimeProviderTimer(TimeSpan dueTime, TimeSpan period, TimerCallback callback, object? state)
                {
                    (uint duration, uint periodTime) = CheckAndGetValues(dueTime, period);
                    _timer = new TimerQueueTimer(callback, state, duration, periodTime, flowExecutionContext: true);
                }

                public bool Change(TimeSpan dueTime, TimeSpan period)
                {
                    (uint duration, uint periodTime) = CheckAndGetValues(dueTime, period);
                    return _timer.Change(duration, periodTime);
                }

                public void Dispose() => _timer.Dispose();

                public ValueTask DisposeAsync() => _timer.DisposeAsync();

                private static (uint duration, uint periodTime) CheckAndGetValues(TimeSpan dueTime, TimeSpan periodTime)
                {
                    long dueTm = (long)dueTime.TotalMilliseconds;
                    ArgumentOutOfRangeException.ThrowIfLessThan(dueTm, -1, nameof(dueTime));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTm, Timer.MaxSupportedTimeout, nameof(dueTime));

                    long periodTm = (long)periodTime.TotalMilliseconds;
                    ArgumentOutOfRangeException.ThrowIfLessThan(periodTm, -1, nameof(periodTime));
                    ArgumentOutOfRangeException.ThrowIfGreaterThan(periodTm, Timer.MaxSupportedTimeout, nameof(periodTime));

                    return ((uint)dueTm, (uint)periodTm);
                }
            }
        }
    }
}
