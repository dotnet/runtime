// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    /// <summary>
    /// Represents an ambient context that can be used to run an operation using a specific <see cref="TimeClock"/>,
    /// a specific <see cref="TimeZoneInfo"/>, or both.
    /// <list type="bullet">
    ///   <item>
    ///     <description>
    ///     Providing a <see cref="TimeClock"/> to any of the <c>Run</c> or <c>RunAsync</c> static methods, for the
    ///     scope of the provided operation, will use that clock to control the values of properties that give the
    ///     current time, including <see cref="DateTimeOffset.UtcNow"/>, <see cref="DateTime.UtcNow"/>,
    ///     <see cref="DateTimeOffset.Now"/>, <see cref="DateTime.Now"/>, and <see cref="DateTime.Today"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///     Providing a <see cref="TimeZoneInfo"/> to any of the <c>Run</c> or <c>RunAsync</c> static methods, for the
    ///     scope of the provided operation, will use that time zone to control the values of properties that give or use
    ///     the local time zone, including <see cref="TimeZoneInfo.Local"/>, <see cref="DateTimeOffset.Now"/>,
    ///     <see cref="DateTime.Now"/>, and <see cref="DateTime.Today"/>, and becomes the local time zone used by all
    ///     platform features that convert to or from local time, such as <see cref="DateTime.ToLocalTime"/>
    ///     <see cref="DateTime.ToUniversalTime"/>,
    ///     and many others.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    public sealed class TimeContext
    {
        private readonly TimeClock _clock;
        private readonly Func<TimeZoneInfo> _localTimeZoneAccessor;

        // The default time context will use the actual system clock and local system time zone.
        private static readonly TimeContext s_defaultTimeContext = new TimeContext(ActualSystemClock.Instance, TimeZoneInfo.GetActualSystemLocal);

        // This is the ambient storage for the time context.
        private static readonly AsyncLocal<TimeContext> s_context = new();

        // Private constructor only.  No public construction allowed.
        private TimeContext(TimeClock clock, Func<TimeZoneInfo> localTimeZoneAccessor)
        {
            _clock = clock;
            _localTimeZoneAccessor = localTimeZoneAccessor;
        }

        /// <summary>
        /// Gets the currently active ambient time context.
        /// </summary>
        public static TimeContext Current
        {
            get => s_context.Value ?? s_defaultTimeContext;
            private set => s_context.Value = value;
        }

        /// <summary>
        /// Gets the actual system clock, regardless of whether it is the current clock or not.
        /// </summary>
        public static ActualSystemClock ActualSystemClock => ActualSystemClock.Instance;

        /// <summary>
        /// Gets a value that indicates whether the current clock is the actual system clock.
        /// </summary>
        public static bool ActualSystemClockIsActive => Current._clock is ActualSystemClock;

        /// <summary>
        /// Gets the actual system time zone, regardless of whether it is the current local time zone or not.
        /// </summary>
        public static TimeZoneInfo ActualSystemLocalTimeZone => TimeZoneInfo.GetActualSystemLocal();

        /// <summary>
        /// Gets a value that indicates whether the current local time zone is the actual system local time zone.
        /// </summary>
        public static bool ActualSystemLocalTimeZoneIsActive => Current._localTimeZoneAccessor == TimeZoneInfo.GetActualSystemLocal;

        /// <summary>
        /// Gets the clock used by this time context.
        /// </summary>
        public TimeClock Clock => _clock;

        /// <summary>
        /// Gets the local time zone used by this time context.
        /// </summary>
        public TimeZoneInfo LocalTimeZone => _localTimeZoneAccessor();

        /// <summary>
        /// Runs a synchronous action under a <see cref="TimeContext"/> with the specified clock.
        /// </summary>
        /// <param name="clock">The clock that will be in effect during the action.</param>
        /// <param name="action">The action to run.</param>
        public static void Run(TimeClock clock, Action action)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, original._localTimeZoneAccessor);
            action.Invoke();
            Current = original;
        }

        /// <summary>
        /// Runs a synchronous action under a <see cref="TimeContext"/> with the specified local time zone.
        /// </summary>
        /// <param name="localTimeZone">The local time zone that will be in effect during the action.</param>
        /// <param name="action">The action to run.</param>
        public static void Run(TimeZoneInfo localTimeZone, Action action)
        {
            TimeContext original = Current;
            Current = new TimeContext(original._clock, () => localTimeZone);
            action.Invoke();
            Current = original;
        }

        /// <summary>
        /// Runs a synchronous action under a <see cref="TimeContext"/> with the specified clock and local time zone.
        /// </summary>
        /// <param name="clock">The clock that will be in effect during the action.</param>
        /// <param name="localTimeZone">The local time zone that will be in effect during the action.</param>
        /// <param name="action">The action to run.</param>
        public static void Run(TimeClock clock, TimeZoneInfo localTimeZone, Action action)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, () => localTimeZone);
            action.Invoke();
            Current = original;
        }

        /// <summary>
        /// Runs a synchronous function under a <see cref="TimeContext"/> with the specified clock.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>The result from running the function.</returns>
        public static TResult Run<TResult>(TimeClock clock, Func<TResult> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, original._localTimeZoneAccessor);
            TResult result = function.Invoke();
            Current = original;
            return result;
        }

        /// <summary>
        /// Runs a synchronous function under a <see cref="TimeContext"/> with the specified local time zone.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>The result from running the function.</returns>
        public static TResult Run<TResult>(TimeZoneInfo localTimeZone, Func<TResult> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(original._clock, () => localTimeZone);
            TResult result = function.Invoke();
            Current = original;
            return result;
        }

        /// <summary>
        /// Runs a synchronous function under a <see cref="TimeContext"/> with the specified clock and local time zone.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>The result from running the function.</returns>
        public static TResult Run<TResult>(TimeClock clock, TimeZoneInfo localTimeZone, Func<TResult> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, () => localTimeZone);
            TResult result = function.Invoke();
            Current = original;
            return result;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified clock.
        /// </summary>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task"/> from the asynchronous function call.</returns>
        public static async Task RunAsync(TimeClock clock, Func<Task> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, original._localTimeZoneAccessor);
            await function.Invoke().ConfigureAwait(false);
            Current = original;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified local time zone.
        /// </summary>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task"/> from the asynchronous function call.</returns>
        public static async Task RunAsync(TimeZoneInfo localTimeZone, Func<Task> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(original._clock, () => localTimeZone);
            await function.Invoke().ConfigureAwait(false);
            Current = original;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified clock and local time zone.
        /// </summary>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task"/> from the asynchronous function call.</returns>
        public static async Task RunAsync(TimeClock clock, TimeZoneInfo localTimeZone, Func<Task> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, () => localTimeZone);
            await function.Invoke().ConfigureAwait(false);
            Current = original;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified clock.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> from the asynchronous function call.</returns>
        public static async Task<TResult> RunAsync<TResult>(TimeClock clock, Func<Task<TResult>> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, original._localTimeZoneAccessor);
            TResult result = await function.Invoke().ConfigureAwait(false);
            Current = original;
            return result;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified local time zone.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> from the asynchronous function call.</returns>
        public static async Task<TResult> RunAsync<TResult>(TimeZoneInfo localTimeZone, Func<Task<TResult>> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(original._clock, () => localTimeZone);
            TResult result = await function.Invoke().ConfigureAwait(false);
            Current = original;
            return result;
        }

        /// <summary>
        /// Runs an asynchronous function under a <see cref="TimeContext"/> with the specified clock and local time zone.
        /// </summary>
        /// <typeparam name="TResult">The type of the result of the function.</typeparam>
        /// <param name="clock">The clock that will be in effect during the function.</param>
        /// <param name="localTimeZone">The local time zone that will be in effect during the function.</param>
        /// <param name="function">The function to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> from the asynchronous function call.</returns>
        public static async Task<TResult> RunAsync<TResult>(TimeClock clock, TimeZoneInfo localTimeZone, Func<Task<TResult>> function)
        {
            TimeContext original = Current;
            Current = new TimeContext(clock, () => localTimeZone);
            TResult result = await function.Invoke().ConfigureAwait(false);
            Current = original;
            return result;
        }
    }
}
