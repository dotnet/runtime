// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        private static object s_lockObject = new object();

        public static SocketPerfCounter Instance
        {
            get
            {
                if (Volatile.Read(ref s_instance) == null)
                {
                    lock (s_lockObject)
                    {
                        if (Volatile.Read(ref s_instance) == null)
                        {
                            s_instance = new SocketPerfCounter();
                        }
                    }
                }
                return s_instance;
            }
        }

        public bool Enabled
        {
            get
            {
                // TODO (#7833): Implement socket perf counters.
                return false;
            }
        }

        public void Increment(SocketPerfCounterName perfCounter)
        {
            // TODO (#7833): Implement socket perf counters.
        }

        public void Increment(SocketPerfCounterName perfCounter, long amount)
        {
            // TODO (#7833): Implement socket perf counters.
        }
    }
}
