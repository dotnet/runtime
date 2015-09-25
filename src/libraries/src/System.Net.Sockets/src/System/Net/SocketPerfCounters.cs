// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                // TODO (Event logging for System.Net.* #2500): Implement socket perf counters.
                return false;
            }
        }

        public void Increment(SocketPerfCounterName perfCounter)
        {
            // TODO (Event logging for System.Net.* #2500): Implement socket perf counters.
        }

        public void Increment(SocketPerfCounterName perfCounter, long amount)
        {
            // TODO (Event logging for System.Net.* #2500): Implement socket perf counters.
        }
    }
}
