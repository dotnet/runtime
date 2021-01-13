// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Internal;

namespace System.Net
{
    internal class NameResolutionTelemetry
    {
        public static NameResolutionTelemetry Log => new NameResolutionTelemetry();

        public bool IsEnabled() => false;

        public ValueStopwatch BeforeResolution(string hostNameOrAddress) => default;

        public ValueStopwatch BeforeResolution(IPAddress address) => default;

        public void AfterResolution(ValueStopwatch stopwatch, bool successful) { }
    }
}
