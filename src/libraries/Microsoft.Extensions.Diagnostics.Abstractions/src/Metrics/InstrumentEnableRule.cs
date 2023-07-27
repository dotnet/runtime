// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class InstrumentEnableRule(string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enable)
    {
        public string? ListenerName { get; } = listenerName;
        public string? MeterName { get; } = meterName;
        public MeterScope Scopes { get; } = scopes;
        public string? InstrumentName { get; } = instrumentName;
        public bool Enable { get; } = enable;
    }
}
