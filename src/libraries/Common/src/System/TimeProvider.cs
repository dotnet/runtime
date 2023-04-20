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
        /// <summary>
        /// Gets a <see cref="TimeProvider"/> that provides a clock based on <see cref="DateTimeOffset.UtcNow"/>,
        /// a time zone based on <see cref="TimeZoneInfo.Local"/>, a high-performance time stamp based on <see cref="Stopwatch"/>,
        /// and a timer based on <see cref="Timer"/>.
        /// </summary>
        /// <remarks>
        /// If the <see cref="TimeZoneInfo.Local"/> changes after the object is returned, the change will be reflected in any subsequent operations that retrieve <see cref="TimeProvider.GetLocalNow"/>.
        /// </remarks>
        public static TimeProvider System { get; } = new SystemTimeProvider();

        /// <summary>
        /// Initializes the <see cref="TimeProvider"/>.
        /// </summary>
        protected TimeProvider()
        {
        }

        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value whose date and time are set to the current
        /// Coordinated Universal Time (UTC) date and time and whose offset is Zero,
        /// all according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see cref="DateTimeOffset.UtcNow"/>.
        /// </remarks>
        public virtual DateTimeOffset GetUtcNow() => DateTimeOffset.UtcNow;

        private static readonly long s_minDateTicks = DateTime.MinValue.Ticks;
        private static readonly long s_maxDateTicks = DateTime.MaxValue.Ticks;

        /// <summary>
        /// Gets a <see cref="DateTimeOffset"/> value that is set to the current date and time according to this <see cref="TimeProvider"/>'s
        /// notion of time based on <see cref="GetUtcNow"/>, with the offset set to the <see cref="LocalTimeZone"/>'s offset from Coordinated Universal Time (UTC).
        /// </summary>
        public DateTimeOffset GetLocalNow()
        {
            DateTimeOffset utcDateTime = GetUtcNow();
            TimeZoneInfo zoneInfo = LocalTimeZone;
            if (zoneInfo is null)
            {
#if SYSTEM_PRIVATE_CORELIB
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_TimeProviderNullLocalTimeZone);
#else
                throw new InvalidOperationException(SR.InvalidOperation_TimeProviderNullLocalTimeZone);
#endif // SYSTEM_PRIVATE_CORELIB
            }
            TimeSpan offset = zoneInfo.GetUtcOffset(utcDateTime);

            long localTicks = utcDateTime.Ticks + offset.Ticks;
            if ((ulong)localTicks > (ulong)s_maxDateTicks)
            {
                localTicks = localTicks < s_minDateTicks ? s_minDateTicks : s_maxDateTicks;
            }

            return new DateTimeOffset(localTicks, offset);
        }

        /// <summary>
        /// Gets a <see cref="TimeZoneInfo"/> object that represents the local time zone according to this <see cref="TimeProvider"/>'s notion of time.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see cref="TimeZoneInfo.Local"/>.
        /// </remarks>
        public virtual TimeZoneInfo LocalTimeZone { get => TimeZoneInfo.Local; }

        /// <summary>
        /// Gets the frequency of <see cref="GetTimestamp"/> of high-frequency value per second.
        /// </summary>
        /// <remarks>
        /// The default implementation returns <see cref="Stopwatch.Frequency"/>. For a given TimeProvider instance, the value must be idempotent and remain unchanged.
        /// </remarks>
        public virtual long TimestampFrequency { get => Stopwatch.Frequency; }

        /// <summary>
        /// Gets the current high-frequency value designed to measure small time intervals with high accuracy in the timer mechanism.
        /// </summary>
        /// <returns>A long integer representing the high-frequency counter value of the underlying timer mechanism. </returns>
        /// <remarks>
        /// The default implementation returns <see cref="Stopwatch.GetTimestamp"/>.
        /// </remarks>
        public virtual long GetTimestamp() => Stopwatch.GetTimestamp();

        /// <summary>
        /// Gets the elapsed time between two timestamps retrieved using <see cref="GetTimestamp"/>.
        /// </summary>
        /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
        /// <param name="endingTimestamp">The timestamp marking the end of the time period.</param>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting and ending timestamps.</returns>
        public TimeSpan GetElapsedTime(long startingTimestamp, long endingTimestamp)
        {
            long timestampFrequency = TimestampFrequency;
            if (timestampFrequency <= 0)
            {
#if SYSTEM_PRIVATE_CORELIB
                ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_TimeProviderInvalidTimestampFrequency);
#else
                throw new InvalidOperationException(SR.InvalidOperation_TimeProviderInvalidTimestampFrequency);
#endif // SYSTEM_PRIVATE_CORELIB
            }

            return new TimeSpan((long)((endingTimestamp - startingTimestamp) * ((double)TimeSpan.TicksPerSecond / timestampFrequency)));
        }

        /// <summary>
        /// Gets the elapsed time since the <paramref name="startingTimestamp"/> value retrieved using <see cref="GetTimestamp"/>.
        /// </summary>
        /// <param name="startingTimestamp">The timestamp marking the beginning of the time period.</param>
        /// <returns>A <see cref="TimeSpan"/> for the elapsed time between the starting timestamp and the time of this call./></returns>
        public TimeSpan GetElapsedTime(long startingTimestamp) => GetElapsedTime(startingTimestamp, GetTimestamp());

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
        public virtual ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
#if SYSTEM_PRIVATE_CORELIB
            ArgumentNullException.ThrowIfNull(callback);
#else
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }
#endif // SYSTEM_PRIVATE_CORELIB

            return new SystemTimeProviderTimer(dueTime, period, callback, state);
        }

        /// <summary>Thin wrapper for a <see cref="Timer"/>.</summary>
        /// <remarks>
        /// We don't return a TimerQueueTimer directly as it implements IThreadPoolWorkItem and we don't
        /// want it exposed in a way that user code could directly queue the timer to the thread pool.
        /// We also use this instead of Timer because CreateTimer needs to return a timer that's implicitly
        /// rooted while scheduled.
        /// </remarks>
        private sealed class SystemTimeProviderTimer : ITimer
        {
#if SYSTEM_PRIVATE_CORELIB
            private readonly TimerQueueTimer _timer;
#else
            private readonly Timer _timer;
#endif // SYSTEM_PRIVATE_CORELIB
            public SystemTimeProviderTimer(TimeSpan dueTime, TimeSpan period, TimerCallback callback, object? state)
            {
                (uint duration, uint periodTime) = CheckAndGetValues(dueTime, period);
#if SYSTEM_PRIVATE_CORELIB
                _timer = new TimerQueueTimer(callback, state, duration, periodTime, flowExecutionContext: true);
#else
                // We want to ensure the timer we create will be tracked as long as it is scheduled.
                // To do that, we call the constructor which track only the callback which will make the time to be tracked by the scheduler
                // then we call Change on the timer to set the desired duration and period.
                _timer = new Timer(_ => callback(state));
                _timer.Change(duration, periodTime);
#endif // SYSTEM_PRIVATE_CORELIB
            }

            public bool Change(TimeSpan dueTime, TimeSpan period)
            {
                (uint duration, uint periodTime) = CheckAndGetValues(dueTime, period);
                try
                {
                    return _timer.Change(duration, periodTime);
                }
                catch (ObjectDisposedException)
                {
                    return false;
                }
            }

            public void Dispose() => _timer.Dispose();


#if SYSTEM_PRIVATE_CORELIB
            public ValueTask DisposeAsync() => _timer.DisposeAsync();
#else
            public ValueTask DisposeAsync()
            {
                _timer.Dispose();
                return default;
            }
#endif // SYSTEM_PRIVATE_CORELIB

            private static (uint duration, uint periodTime) CheckAndGetValues(TimeSpan dueTime, TimeSpan periodTime)
            {
                long dueTm = (long)dueTime.TotalMilliseconds;
                long periodTm = (long)periodTime.TotalMilliseconds;

#if SYSTEM_PRIVATE_CORELIB
                ArgumentOutOfRangeException.ThrowIfLessThan(dueTm, -1, nameof(dueTime));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(dueTm, Timer.MaxSupportedTimeout, nameof(dueTime));

                ArgumentOutOfRangeException.ThrowIfLessThan(periodTm, -1, nameof(periodTime));
                ArgumentOutOfRangeException.ThrowIfGreaterThan(periodTm, Timer.MaxSupportedTimeout, nameof(periodTime));
#else
                const uint MaxSupportedTimeout = 0xfffffffe;

                if (dueTm < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(dueTime), dueTm, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, nameof(dueTime), -1));
                }

                if (dueTm > MaxSupportedTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(dueTime), dueTm, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLessOrEqual, nameof(dueTime), MaxSupportedTimeout));
                }

                if (periodTm < -1)
                {
                    throw new ArgumentOutOfRangeException(nameof(periodTm), periodTm, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeGreaterOrEqual, nameof(periodTm), -1));
                }

                if (periodTm > MaxSupportedTimeout)
                {
                    throw new ArgumentOutOfRangeException(nameof(periodTm), periodTm, SR.Format(SR.ArgumentOutOfRange_Generic_MustBeLessOrEqual, nameof(periodTm), MaxSupportedTimeout));
                }
#endif // SYSTEM_PRIVATE_CORELIB

                return ((uint)dueTm, (uint)periodTm);
            }
        }

        /// <summary>
        /// Used to create a <see cref="TimeProvider"/> instance returned from <see cref="TimeProvider.System"/> and uses the default implementation
        /// provided by <see cref="TimeProvider"/> which uses <see cref="DateTimeOffset.UtcNow"/>, <see cref="TimeZoneInfo.Local"/>, <see cref="Stopwatch"/>, and <see cref="Timer"/>.
        /// </summary>
        private sealed class SystemTimeProvider : TimeProvider
        {
            /// <summary>Initializes the instance.</summary>
            internal SystemTimeProvider() : base()
            {
            }
        }
    }
}
