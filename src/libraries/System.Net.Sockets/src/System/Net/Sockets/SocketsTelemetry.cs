// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;

namespace System.Net.Sockets
{
    [EventSource(Name = "System.Net.Sockets")]
    internal sealed class SocketsTelemetry : EventSource
    {
        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

        [NonEvent]
        public void ConnectStart(Internals.SocketAddress address)
        {
            if (IsEnabled())
            {
                ConnectStart(address.ToString());
            }
        }

        [NonEvent]
        public void ConnectStart(EndPoint address)
        {
            if (IsEnabled())
            {
                var addressString = address.ToString();
                if (addressString != null)
                {
                    ConnectStart(addressString);
                }
            }
        }

        [Event(1, Level = EventLevel.Informational)]
        public void ConnectStart(string address)
        {
            WriteSocketEvent(eventId: 1, address);
        }

        [Event(2, Level = EventLevel.Informational)]
        public void ConnectStop()
        {
            WriteEvent(eventId: 2);
        }

        [Event(3, Level = EventLevel.Error)]
        public void ConnectFailed()
        {
            WriteEvent(eventId: 3);
        }

        [Event(4, Level = EventLevel.Warning)]
        public void ConnectCancelled()
        {
            WriteEvent(eventId: 4);
        }

        [NonEvent]
        private unsafe void WriteSocketEvent(int eventId, string address)
        {
            if (IsEnabled())
            {
                if (address == null) address = "";

                fixed (char* addressPtr = address)
                {
                    const int NumEventDatas = 1;
                    var descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)addressPtr,
                        Size = (address.Length + 1) * sizeof(char)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }
    }
}
