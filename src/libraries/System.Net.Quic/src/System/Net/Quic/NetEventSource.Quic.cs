// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using System.Net.Quic.Implementations.MsQuic.Internal;

namespace System.Net
{
    [EventSource(Name = "Private.InternalDiagnostics.System.Net.Quic")]
    internal sealed partial class NetEventSource : EventSource
    {
        static partial void AdditionalCustomizedToString<T>(T value, ref string? result)
        {
            MsQuicSafeHandle? safeHandle = value as MsQuicSafeHandle;
            if (safeHandle is not null)
            {
                result = safeHandle.ToString();
            }
        }
    }
}
