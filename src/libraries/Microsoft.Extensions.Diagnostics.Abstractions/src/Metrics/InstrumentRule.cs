// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    /// <summary>
    /// A set of parameters used to determine which instruments are enabled for which listeners. Unspecified
    /// parameters match anything.
    /// </summary>
    /// <remarks>
    /// The most specific rule that matches a given instrument will be used. The priority of parameters is as follows:
    /// - MeterName, either an exact match, or the longest prefix match. See <see cref="Meter.Name"/>.
    /// - InstrumentName, an exact match. <see cref="Instrument.Name"/>.
    /// - ListenerName, an exact match. <see cref="IMetricsListener.Name"/>.
    /// - Scopes
    /// </remarks>
    /// <param name="meterName">The <see cref="Meter.Name"/> or prefix.</param>
    /// <param name="instrumentName">The <see cref="Instrument.Name"/>.</param>
    /// <param name="listenerName">The <see cref="IMetricsListener.Name"/>.</param>
    /// <param name="scopes">The <see cref="MeterScope"/>'s to consider.</param>
    /// <param name="enable">Enables or disabled the matched instrument for this listener.</param>
    public class InstrumentRule(string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enable)
    {
        /// <summary>
        /// The <see cref="Meter.Name"/>, either an exact match or the longest prefix match. Only full segment matches are considered.
        /// All meters are matched if this is null.
        /// </summary>
        public string? MeterName { get; } = meterName;

        /// <summary>
        /// The <see cref="Instrument.Name"/>, an exact match.
        /// All instruments for the given meter are matched if this is null.
        /// </summary>
        public string? InstrumentName { get; } = instrumentName;

        /// <summary>
        /// The <see cref="IMetricsListener.Name"/>, an exact match.
        /// All listeners are matched if this is null.
        /// </summary>
        public string? ListenerName { get; } = listenerName;

        /// <summary>
        /// The <see cref="MeterScope"/>. This is used to distinguish between meters created via <see cref="Meter"/> constructors (<see cref="MeterScope.Global"/>)
        /// and those created via Dependency Injection with <see cref="IMeterFactory.Create(MeterOptions)"/> (<see cref="MeterScope.Local"/>)."/>.
        /// </summary>
        public MeterScope Scopes { get; } = scopes == MeterScope.None
            ? throw new ArgumentOutOfRangeException(nameof(scopes), scopes, "The MeterScope must be Global, Local, or both.")
            : scopes;

        /// <summary>
        /// Indicates if the instrument should be enabled for the listener.
        /// </summary>
        public bool Enable { get; } = enable;
    }
}
