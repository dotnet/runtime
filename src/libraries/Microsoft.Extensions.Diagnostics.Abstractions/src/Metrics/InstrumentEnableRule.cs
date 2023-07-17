// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Metrics;

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class InstrumentEnableRule
    {
        public InstrumentEnableRule(string? listenerName, string? meterName, MeterScope scopes, string? instrumentName, Action<string?, Instrument, bool> filter) { }
        public string? ListenerName { get; }
        public string? MeterName { get; }
        public MeterScope Scopes { get; }
        public string? InstrumentName { get; }
        public Func<string?, Instrument, bool>? Filter { get; }
    }
}
