// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Diagnostics
{
    /// <summary>
    /// Implements a <see cref="TimeClock"/> that retrieves the current time from
    /// the actual system clock, as provided by the underlying operating system.
    /// </summary>
    public sealed partial class ActualSystemClock : TimeClock
    {
        private ActualSystemClock()
        {
        }

        /// <summary>
        /// Gets a singleton instance of the <see cref="ActualSystemClock"/>.
        /// </summary>
        public static ActualSystemClock Instance { get; } = new();

        protected override DateTimeOffset GetCurrentUtcDateTimeOffsetImpl()
        {
            ulong ticks = GetTicks();
            return new DateTimeOffset(0, new DateTime(ticks));
        }

        internal override DateTime GetCurrentUtcDateTimeImpl()
        {
            ulong ticks = GetTicks();
            return new DateTime(ticks | DateTime.KindUtc);
        }
    }
}
