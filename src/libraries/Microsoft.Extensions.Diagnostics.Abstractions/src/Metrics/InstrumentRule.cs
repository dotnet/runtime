// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Diagnostics.Metrics
{
    public class InstrumentRule(string? meterName, string? instrumentName, string? listenerName, MeterScope scopes, bool enable)
    {
        public string? MeterName { get; } = meterName;
        public string? InstrumentName { get; } = instrumentName;
        public string? ListenerName { get; } = listenerName;
        public MeterScope Scopes { get; } = scopes;
        public bool Enable { get; } = enable;
    }
}
