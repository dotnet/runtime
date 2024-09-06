// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Versioning;

namespace System.Net
{
#pragma warning disable IDE0060
#pragma warning disable CA1822
    internal sealed class NameResolutionTelemetry : EventSource
    {
        public static readonly NameResolutionTelemetry Log = new NameResolutionTelemetry();

        [NonEvent]
        public static bool AnyDiagnosticsEnabled()
        {
            return false;
        }

        [NonEvent]
        public NameResolutionActivity BeforeResolution(object hostNameOrAddress, long startingTimestamp = 0)
        {
            return default;
        }

        [NonEvent]
        public void AfterResolution(object hostNameOrAddress, in NameResolutionActivity activity, object? answer, Exception? exception = null)
        {
        }
    }

    internal readonly struct NameResolutionActivity
    {
        public static bool IsTracingEnabled()
        {
            return false;
        }
    }
}
