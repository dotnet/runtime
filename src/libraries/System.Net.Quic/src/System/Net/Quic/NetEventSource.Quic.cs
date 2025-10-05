// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Quic;

namespace System.Net
{
    [EventSource(Name = NetEventSourceName)]
    internal sealed partial class NetEventSource
    {
        private const string NetEventSourceName = "Private.InternalDiagnostics.System.Net.Quic";

        public NetEventSource() : base(NetEventSourceName) { }

        static partial void AdditionalCustomizedToString(object value, ref string? result)
        {
            if (value is MsQuicSafeHandle safeHandle)
            {
                result = safeHandle.ToString();
            }
        }
    }
}
