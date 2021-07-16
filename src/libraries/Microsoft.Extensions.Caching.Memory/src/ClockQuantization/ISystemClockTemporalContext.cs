// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;


namespace Microsoft.Extensions.Internal.ClockQuantization
{
    /// <summary>
    /// Represents traits of the temporal context
    /// </summary>
    internal interface ISystemClockTemporalContext
    {
        /// <value>
        /// A non-<see langword="null"/> value if the temporal context provides a metronome feature - i.e., if it fires <see cref="MetronomeTicked"/> events.
        /// </value>
        TimeSpan? MetronomeIntervalTimeSpan { get; }
        bool ClockIsManual { get; }

        /// <summary>
        /// An event that can be raised to inform listeners that the <see cref="ISystemClock"/> was adjusted.
        /// </summary>
        /// <remarks>
        /// This will typically be used with synthetic clocks only.
        /// </remarks>
        event EventHandler? ClockAdjusted;

        /// <summary>
        /// An event that can be raised to inform listeners that a metronome "tick" occurred.
        /// </summary>
        event EventHandler? MetronomeTicked;
    }
}

#nullable restore
