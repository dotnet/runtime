// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class InstrumentEnableRule(string? listenerName, string? meterName, MeterScope scopes, string? instrumentName, Func<string?, Instrument, bool>? filter)
    {
        public string? ListenerName { get; } = listenerName;
        public string? MeterName { get; } = meterName;
        public MeterScope Scopes { get; } = scopes;
        public string? InstrumentName { get; } = instrumentName;

        /// <summary>
        /// A filter callback that takes a listener name and can be used to enable or disable an instrument.
        /// </summary>
        public Func<string?, Instrument, bool>? Filter { get; } = filter;
    }
}
