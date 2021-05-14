// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// <see cref="ClockQuantizer"/> is a utility class that abstracts quantization of the reference clock. Essentially, the reference clock continuum is divided into discrete intervals with a <i>maximum</i> length of <see cref="ClockQuantizer.MaxIntervalTimeSpan"/>.
    /// A so-called metronome is used to start a new <see cref="Interval"/> every time when <see cref="ClockQuantizer.MaxIntervalTimeSpan"/> has passed. A <see cref="Interval"/> may be cut short when an "out-of-cadance" advance operation is performed - such operation is triggered by
    /// <see cref="Advance()"/> calls, as well as by <see cref="ISystemClockTemporalContext.ClockAdjusted"/> and <see cref="ISystemClockTemporalContext.MetronomeTicked"/> events.
    /// </summary>
    /// <remarks>Under certain conditions, an advance operation may be incurred by <see cref="EnsureInitializedExactClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition, bool)"/> calls.</remarks>
    internal class ClockQuantizer : IAsyncDisposable, IDisposable
    {
        private readonly TemporalContextDriver _driver;
        private Interval? _currentInterval;


        #region Fields & properties

        /// <summary>
        /// The maximum <see cref="TimeSpan"/> of each <see cref="Interval"/>, defined at <see cref="ClockQuantizer"/> construction.
        /// </summary>
        public readonly TimeSpan MaxIntervalTimeSpan;

        /// <value>The current <see cref="Interval"/> in the <see cref="ClockQuantizer"/>'s temporal context.</value>
        /// <remarks>A <see cref="ClockQuantizer"/> starts in an inhibited state. Only after the first advance operation, will <see cref="CurrentInterval"/> have a non-<see langword="null"/> value.</remarks>
        public Interval? CurrentInterval { get => _currentInterval; }

        /// <value>
        /// Represents the clock-specific offset at which the next <see cref="MetronomeTicked"/> event is expected.
        /// </value>
        /// <remarks>
        /// <para>While uninitialized initially, <see cref="NextMetronomicClockOffset"/> will always have a value after the first advance operation. Basically, having
        /// <c>CurrentInterval.ClockOffset + TimeSpanToClockOffsetUnits(MaxIntervalTimeSpan)</c> pre-calculated at the start of each metronomic interval, ammortizes the cost of this typical calculation during time-based decisions.</para>
        /// <para>When an "out-of-cadance" (i.e. non-metronomic) advance operation is performed, <see cref="CurrentInterval"/> (and its offset) will update, but not <see cref="NextMetronomicClockOffset"/>.</para>
        /// </remarks>
        public long? NextMetronomicClockOffset { get; private set; }


        /// <value>Returns the <see cref="ISystemClock.UtcNow"/> value of the reference clock.</value>
        /// <remarks>Depending on the actual reference clock implementation, this may or may not incur an expensive system call.</remarks>
        public DateTimeOffset UtcNow { get => _driver.UtcNow; }

        /// <value>Returns the <see cref="ISystemClock.UtcNowClockOffset"/> value of the reference clock.</value>
        public long UtcNowClockOffset { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _driver.UtcNowClockOffset; }

        #endregion


        #region Time representation conversions

        /// <summary>
        /// Converts a <see cref="DateTimeOffset"/> to an offset in clock-specific units (ticks).
        /// </summary>
        /// <param name="offset">The <see cref="DateTimeOffset"/> to convert</param>
        /// <returns>An offset in clock-specific units.</returns>
        /// <seealso cref="ISystemClock.ClockOffsetUnitsPerMillisecond"/>
        public long DateTimeOffsetToClockOffset(DateTimeOffset offset) => _driver.DateTimeOffsetToClockOffset(offset);

        /// <summary>
        /// Converts an offset in clock-specific units (ticks) to a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="offset">The clock-specific offset to convert</param>
        /// <returns>A <see cref="DateTimeOffset"/> in UTC.</returns>
        /// <seealso cref="ISystemClock.ClockOffsetUnitsPerMillisecond"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ClockOffsetToUtcDateTimeOffset(long offset) => _driver.ClockOffsetToUtcDateTimeOffset(offset);

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> to a count of clock-specific offset units (ticks).
        /// </summary>
        /// <param name="timeSpan">The <see cref="TimeSpan"/> to convert</param>
        /// <returns>The amount of clock-specific offset units covering the <see cref="TimeSpan"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long TimeSpanToClockOffsetUnits(TimeSpan timeSpan) => (long)(timeSpan.TotalMilliseconds * _driver.ClockOffsetUnitsPerMillisecond);

        /// <summary>
        /// Converts an amount of clock-specific offset units (ticks) to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="units">The amount of units to convert</param>
        /// <returns>A <see cref="TimeSpan"/> covering the specified number of <paramref name="units"/>.</returns>
        public TimeSpan ClockOffsetUnitsToTimeSpan(long units) => TimeSpan.FromMilliseconds((double)units / _driver.ClockOffsetUnitsPerMillisecond);

        #endregion


        #region Basic quantizer & clock-offset-serial position operations

        /// <summary>
        /// Establishes a new <b>lower bound</b> on the "last seen" exact <see cref="DateTimeOffset"/> within the
        /// <see cref="ClockQuantizer"/>'s temporal context: the reference clock's <see cref="ISystemClock.UtcNow"/>.
        /// </summary>
        /// <returns>The newly started <see cref="Interval"/>.</returns>
        public Interval Advance() => Advance(metronomic: false);

        /// <summary>
        /// If <paramref name="position"/> does not have an exact <see cref="LazyClockOffsetSerialPosition.ClockOffset"/> yet, it will be initialized with one. In every
        /// situation where initialization is still required, this will incur a call into the reference clock's <see cref="ISystemClock.UtcNow"/>.
        /// </summary>
        /// <param name="position">Reference to an (on-stack) <see cref="LazyClockOffsetSerialPosition"/> which may or may not have been initialized.</param>
        /// <param name="advance">Indicates if the <see cref="ClockQuantizer"/> should perform an advance operation. This is advised in situations where non-exact
        /// positions may still be acquired in the same <see cref="CurrentInterval"/> and exact ordering (e.g. in a cache LRU eviction algorithm) might be adversely affected.</param>
        /// <remarks>
        /// <para>An advance operation will incur an <see cref="ClockQuantizer.Advanced"/> event.</para>
        /// <para>Depending on the actual reference clock implementation, this may or may not incur an expensive system call.</para>
        /// </remarks>
        public void EnsureInitializedExactClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition position, bool advance)
        {
            if (!position.IsExact)    // test here as well to prevent unnecessary/unexpected Advance() if position was already initialzed
            {
                if (advance)
                {
                    var preparation = PrepareAdvance(metronomic: false);
                    Interval.EnsureInitializedClockOffsetSerialPosition(preparation.Interval, ref position);
                    CommitAdvance(preparation);
                }
                else
                {
                    Interval.EnsureInitializedClockOffsetSerialPosition(NewDisconnectedInterval(), ref position);
                }
            }
        }

        /// <summary>
        /// If <paramref name="position"/> does not have an <see cref="LazyClockOffsetSerialPosition.ClockOffset"/> yet, it will be initialized with one.
        /// </summary>
        /// <param name="position">Reference to an (on-stack) <see cref="LazyClockOffsetSerialPosition"/> which may or may not have been initialized.</param>
        /// <remarks>
        /// If the <see cref="ClockQuantizer"/> had not performed a first advance operation yet, the result will be an exact position
        /// (incurring a call into the reference clock's <see cref="ISystemClock.UtcNow"/>). Otherwise, returns a position bound to
        /// <see cref="CurrentInterval"/>'s <see cref="Interval.ClockOffset"/>, but with an incremented <see cref="LazyClockOffsetSerialPosition.SerialPosition"/>.
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureInitializedClockOffsetSerialPosition(ref LazyClockOffsetSerialPosition position)
        {
            if (!position.HasValue)
            {
                Interval.EnsureInitializedClockOffsetSerialPosition(_currentInterval ?? NewDisconnectedInterval(), ref position);
            }
        }

        #endregion


        #region Events

        /// <summary>
        /// Represents the ephemeral conditions at the time of an advance operation.
        /// </summary>
        public class NewIntervalEventArgs : EventArgs
        {
            /// <summary>
            /// The <see cref="System.DateTimeOffset"/> within the temporal context when the new <see cref="Interval"/> was started.
            /// </summary>
            public readonly DateTimeOffset DateTimeOffset;

            /// <summary>
            /// <see langword="true"/> if the new <see cref="Interval"/> was created due to a metronome "tick", <see langword="false"/> otherwise.
            /// </summary>
            public readonly bool IsMetronomic;

            /// <summary>
            /// An optional <see cref="System.TimeSpan"/> value representing the gap between the start of the new interval and the expected end of
            /// <see cref="ClockQuantizer.CurrentInterval"/>, if such gap is in fact detected.
            /// </summary>
            public readonly TimeSpan? GapToPriorIntervalExpectedEnd;

            internal NewIntervalEventArgs(DateTimeOffset offset, bool metronomic, TimeSpan? gap)
            {
                DateTimeOffset = offset;
                IsMetronomic = metronomic;
                GapToPriorIntervalExpectedEnd = gap;
            }
        }

        /// <summary>
        /// This event is fired direclty after the start of a new <see cref="Interval"/> within the <see cref="ClockQuantizer"/>'s temporal context.
        /// </summary>
        public event EventHandler<NewIntervalEventArgs>? Advanced;

        /// <summary>
        /// This event is fired almost immediately after each "tick" of the metronome. Every <see cref="MetronomeTicked"/> event is preceeded by an advance operation and a corresponding <see cref="Advanced"/> event, ensuring that a new <see cref="CurrentInterval"/> reference
        /// has been established in the <see cref="ClockQuantizer"/>'s temporal context at the time of firing.
        /// </summary>
        /// <remarks>
        /// Under typical operating conditions, the intermittent elapse of every <see cref="MaxIntervalTimeSpan"/> interval is signaled by the <see cref="ClockQuantizer"/>'s built-in metronome.
        /// Alternatively, metronome "ticks" may be generated by an external source that is firing <see cref="ISystemClockTemporalContext.MetronomeTicked"/> events.
        /// </remarks>
        /// <seealso cref="ISystemClockTemporalContext"/>
        public event EventHandler<NewIntervalEventArgs>? MetronomeTicked;

        /// <summary>
        /// Raises the <see cref="Advanced"/> event. May be overriden in derived implementations.
        /// </summary>
        /// <param name="e">A <see cref="NewIntervalEventArgs"/> instance</param>
        protected virtual void OnAdvanced(NewIntervalEventArgs e) => Advanced?.Invoke(this, e);

        /// <summary>
        /// Raises the <see cref="MetronomeTicked"/> event. May be overriden in derived implementations.
        /// </summary>
        /// <param name="e">A <see cref="NewIntervalEventArgs"/> instance</param>
        /// <remarks>
        /// <see cref="MetronomeTicked"/> events are always preceded with an <see cref="Advanced"/> event. The value of <paramref name="e"/> is the same in both consecutive events.
        /// </remarks>
        protected virtual void OnMetronomeTicked(NewIntervalEventArgs e) => MetronomeTicked?.Invoke(this, e);

        #endregion


        // Construction

        /// <summary>
        /// Creates a new <see cref="ClockQuantizer"/> instance.
        /// </summary>
        /// <param name="clock">The reference <see cref="ISystemClock"/></param>
        /// <param name="maxIntervalTimeSpan">The maximum <see cref="TimeSpan"/> of each <see cref="Interval"/></param>
        /// <remarks>
        /// If <paramref name="clock"/> also implements <see cref="ISystemClockTemporalContext"/>, the <see cref="ClockQuantizer"/> will pick up on external
        /// <see cref="ISystemClockTemporalContext.ClockAdjusted"/> events. Also, if <see cref="ISystemClockTemporalContext.MetronomeIntervalTimeSpan"/> is non-<see langword="null"/>,
        /// the <see cref="ClockQuantizer"/> will pick up on external <see cref="ISystemClockTemporalContext.MetronomeTicked"/> events, instead of relying on an internal metronome.
        /// </remarks>
        public ClockQuantizer(ISystemClock clock, TimeSpan maxIntervalTimeSpan)
        {
            _driver = new TemporalContextDriver(clock, MaxIntervalTimeSpan = maxIntervalTimeSpan);
            _driver.ClockAdjusted += Driver_ClockAdjusted;
            _driver.MetronomeTicked += Driver_MetronomeTicked;
        }


        // Quiescing

        private readonly object _quiescingLockObject = new object();

        /// <summary>
        /// Puts the <see cref="ClockQuantizer"/> into a quiescent state, effectively freeing any <em>owned</em> unmanaged resources. While in a quiescent state, the <see cref="ClockQuantizer"/> will not raise any events, nor perform metronomic advance operations.
        /// </summary>
        /// <remarks>
        /// Any externally initiated advance operation will automatically take the <see cref="ClockQuantizer"/> back into normal operation.
        /// </remarks>
        public void Quiesce()
        {
            // Ensure that quiesent state can be achieved without immediately being knocked out of it by a MetronomeTicked event that just happened to be in flight.
            lock (_quiescingLockObject)
            {
                _driver.Quiesce();
            }
        }

        /// <summary>
        /// Takes the <see cref="ClockQuantizer"/> out of a quiescent state into normal operation.
        /// </summary>
        public void Unquiesce() => _driver.Unquiesce();

        /// <value>
        /// Returns <see langword="true"/> if the <see cref="ClockQuantizer"/> is in a quiescent state, <see langword="false"/> otherwise.
        /// </value>
        public bool IsQuiescent { get => _driver.IsQuiescent; }


        // Advance primitives

        private struct AdvancePreparationInfo
        {
            public Interval Interval;
            public ClockQuantizer.NewIntervalEventArgs EventArgs;

            public AdvancePreparationInfo(Interval interval, ClockQuantizer.NewIntervalEventArgs eventArgs)
            {
                Interval = interval;
                EventArgs = eventArgs;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Interval NewDisconnectedInterval() => new Interval(UtcNowClockOffset);

        private Interval Advance(bool metronomic)
        {
            var preparation = PrepareAdvance(metronomic);
            return CommitAdvance(preparation);
        }

        private AdvancePreparationInfo PrepareAdvance(bool metronomic)
        {
            // Start metronome (if not imposed externally) on first Advance and consider first Advance as a metronomic event.
            bool unquiescing = _driver.Unquiesce();
            if (unquiescing || _currentInterval is null)
            {
                metronomic = true;
            }

            var previousInterval = _currentInterval;
            var interval = NewDisconnectedInterval();
            TimeSpan? detectedGap = null;
            if (previousInterval is not null)
            {
                // Ignore potential *internal* metronome gap due to tiny clock jitter
                if (unquiescing || !metronomic || (metronomic && !_driver.HasInternalMetronome))
                {
                    var gap = ClockOffsetUnitsToTimeSpan(interval.ClockOffset - previousInterval.ClockOffset) - MaxIntervalTimeSpan;
                    if (gap > TimeSpan.Zero)
                    {
                        detectedGap = gap;
                    }
                }
            }

            var e = new NewIntervalEventArgs(_driver.ClockOffsetToUtcDateTimeOffset(interval.ClockOffset), metronomic, detectedGap);

            return new AdvancePreparationInfo(interval, e);
        }

        private Interval CommitAdvance(AdvancePreparationInfo preparation)
        {
            _currentInterval = preparation.Interval.Seal();

            var e = preparation.EventArgs;
            if (e.IsMetronomic)
            {
                NextMetronomicClockOffset = _driver.DateTimeOffsetToClockOffset(e.DateTimeOffset + MaxIntervalTimeSpan);
            }

            OnAdvanced(e);

            if (e.IsMetronomic)
            {
                OnMetronomeTicked(e);
            }

            return preparation.Interval;
        }

        private void Driver_MetronomeTicked(object? _, EventArgs __)
        {
            lock (_quiescingLockObject)
            {
                // Ensure that any in-flight MetronomeTicked event during quiescing transition does not knock us out of a quiesent state that was juuuuuuust established.
                if (!IsQuiescent)
                {
                    Advance(metronomic: true);
                }
            }
        }

        private void Driver_ClockAdjusted(object? _, EventArgs __)
        {
            // Allow clock adjustemts to take us out of quiesent state (race possible; small chance of in-flight event, as underlying driver is quisced as well).
            Advance(metronomic: false);
        }


        #region IAsyncDisposable/IDisposable

        private int _areEventHandlersDetached;
        private void DetachEventHandlers()
        {
            if (Interlocked.CompareExchange(ref _areEventHandlersDetached, 1, 0) == 0)
            {
                _driver.ClockAdjusted -= Driver_ClockAdjusted;
                _driver.MetronomeTicked -= Driver_MetronomeTicked;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // This method is re-entrant
            DetachEventHandlers();

            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async ValueTask DisposeAsync()
        {
            // This method is re-entrant
            DetachEventHandlers();

            await DisposeAsyncCore().ConfigureAwait(false);

            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // This method is re-entrant and mutually co-existent with _driver.DisposeAsyncCore()
                _driver.Dispose();
            }
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            // This method is re-entrant and mutually co-existent with _driver.Dispose()
            await _driver.DisposeAsync().ConfigureAwait(false);
        }

        #endregion
    }
}

#nullable restore

