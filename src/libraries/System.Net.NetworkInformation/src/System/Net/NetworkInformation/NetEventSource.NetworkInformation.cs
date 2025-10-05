// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Net
{
    [EventSource(Name = NetEventSourceName)]
    internal sealed class NetEventSource : EventSource
    {
        private const string NetEventSourceName = "Private.InternalDiagnostics.System.Net.NetworkInformation";

        public NetEventSource() : base(NetEventSourceName) { }

        public static readonly NetEventSource Log = new NetEventSource();

        private const int ErrorEventId = 2;

        [NonEvent]
        public static void Error(Exception exception, [CallerMemberName] string? memberName = null) =>
            Log.ErrorMessage(memberName ?? "(?)", exception.ToString());

        [Event(ErrorEventId, Level = EventLevel.Error)]
        private void ErrorMessage(string memberName, string message)
        {
            Debug.Assert(IsEnabled());
            WriteEvent(ErrorEventId, memberName, message);
        }
    }
}
