// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// Abstracts the system clock to facilitate synthetic clocks (e.g. for replay or testing).
    /// </summary>
    internal abstract class SystemClock : ISystemClock
    {
        private readonly Func<DateTimeOffset> _getUtcNow;

#if NET5_0 || NET5_0_OR_GREATER
        private readonly DateTimeOffset _assumedUtcSystemGenesis;
#endif

        protected SystemClock(Func<DateTimeOffset> GetUtcNow)
        {
            _getUtcNow = GetUtcNow;

#if NET5_0 || NET5_0_OR_GREATER
            // Fetch values separately, quickest one last to try narrow the gap as much as possible;
            // this is instead of: var approximateUtcGenesis = DateTimeOffset.UtcNow.AddMilliseconds(-Environment.TickCount64);
            var utcNow = DateTimeOffset.UtcNow;
            var millisecondsSinceSystemGenesis = Environment.TickCount64;
            var approximateUtcSystemGenesis = utcNow.AddMilliseconds(-millisecondsSinceSystemGenesis);

            // Get rid of residual bits in _utcGenesis (beyond the millisecond frontier)
            var assumedTicksSinceApproximateUtcGenesis = approximateUtcSystemGenesis.Ticks + TimeSpan.TicksPerMillisecond / 2;
            var roundedAssumedTicksSinceApproximateUtcGenesis = assumedTicksSinceApproximateUtcGenesis - assumedTicksSinceApproximateUtcGenesis % TimeSpan.TicksPerMillisecond;

            _assumedUtcSystemGenesis = new DateTimeOffset((long) roundedAssumedTicksSinceApproximateUtcGenesis, TimeSpan.Zero);
#endif
        }

        /// <inheritdoc/>
        public DateTimeOffset UtcNow => _getUtcNow();


#if NET5_0 || NET5_0_OR_GREATER
        /// <inheritdoc/>
        public long ClockOffsetUnitsPerMillisecond { get; } = 1L;

        public abstract long UtcNowClockOffset { get;  }

        /// <inheritdoc/>
        public long DateTimeOffsetToClockOffset(DateTimeOffset offset) => (long) offset.Subtract(_assumedUtcSystemGenesis).TotalMilliseconds;

        /// <inheritdoc/>
        public DateTimeOffset ClockOffsetToUtcDateTimeOffset(long offset) => _assumedUtcSystemGenesis.AddMilliseconds(offset);
#else
        /// <inheritdoc/>
        public long ClockOffsetUnitsPerMillisecond { get; } = TimeSpan.TicksPerMillisecond;

        /// <inheritdoc/>
        public long UtcNowClockOffset => DateTimeOffsetToClockOffset(UtcNow);

        /// <inheritdoc/>
        public long DateTimeOffsetToClockOffset(DateTimeOffset offset) => offset.UtcTicks;

        /// <inheritdoc/>
        public DateTimeOffset ClockOffsetToUtcDateTimeOffset(long offset) => new DateTimeOffset(offset, TimeSpan.Zero);
#endif

        private class ExternalClock : SystemClock
        {
            public ExternalClock(Microsoft.Extensions.Internal.ISystemClock clock)
                : base(() => clock.UtcNow)
            {
            }

#if NET5_0 || NET5_0_OR_GREATER
            /// <inheritdoc/>
            public override long UtcNowClockOffset => DateTimeOffsetToClockOffset(UtcNow);
#endif
        }

        private class ExternalClockWithTemporalContext : ExternalClock, ISystemClockTemporalContext
        {
            private readonly ISystemClockTemporalContext _context;

            public ExternalClockWithTemporalContext(Microsoft.Extensions.Internal.ISystemClock clock, ISystemClockTemporalContext context)
                : base(clock)
            {
                _context = context;
            }

            public TimeSpan? MetronomeIntervalTimeSpan => _context.MetronomeIntervalTimeSpan;

            public bool ClockIsManual => _context.ClockIsManual;

            public event EventHandler? ClockAdjusted
            {
                add => _context.ClockAdjusted += value;
                remove => _context.ClockAdjusted -= value;
            }

            public event EventHandler? MetronomeTicked
            {
                add => _context.MetronomeTicked += value;
                remove => _context.MetronomeTicked -= value;
            }
        }

        private sealed class ManualClock : ExternalClockWithTemporalContext
        {
            public ManualClock(Microsoft.Extensions.Internal.ISystemClock clock)
                : base(clock, (ISystemClockTemporalContext) clock)
            {
            }
        }

        private sealed class InternalClock : SystemClock
        {
            public InternalClock()
                : base(() => DateTimeOffset.UtcNow)
            {
            }

#if NET5_0 || NET5_0_OR_GREATER
            /// <inheritdoc/>
            public override long UtcNowClockOffset => Environment.TickCount64;
#endif
        }

        public static ISystemClock Create(Microsoft.Extensions.Internal.ISystemClock? clock)
        {
            if (clock is ISystemClockTemporalContext context)
            {
                // Note:class ManualClock merely acts as "syntactic sugar" and comes in handy to spot a clock as a manual clock while debugging
                return context.ClockIsManual ? new ManualClock(clock) : new ExternalClockWithTemporalContext(clock, context);
            }

            return clock != null ? new ExternalClock(clock) : new InternalClock();
        }
    }
}

#nullable restore
