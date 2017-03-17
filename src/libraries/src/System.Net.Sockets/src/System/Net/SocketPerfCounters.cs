// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;

namespace System.Net
{
    internal enum SocketPerfCounterName
    {
        SocketConnectionsEstablished = 0, // these enum values are used as index
        SocketBytesReceived,
        SocketBytesSent,
        SocketDatagramsReceived,
        SocketDatagramsSent,
    }

    internal sealed class SocketPerfCounter
    {
        private static SocketPerfCounter s_instance;

        public static SocketPerfCounter Instance => LazyInitializer.EnsureInitialized(ref s_instance);

        public bool Enabled => false; // TODO (#7833): Implement socket perf counters.

        [Conditional("TODO7833")]
        public void Increment(SocketPerfCounterName perfCounter, long amount = 1)
        {
            // TODO (#7833): Implement socket perf counters.
        }
    }
}
