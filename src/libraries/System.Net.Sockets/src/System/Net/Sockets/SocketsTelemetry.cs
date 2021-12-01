// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Sockets
{
    [EventSource(Name = "System.Net.Sockets")]
    internal sealed class SocketsTelemetry : EventSource
    {
        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

        private PollingCounter? _outgoingConnectionsEstablishedCounter;
        private PollingCounter? _incomingConnectionsEstablishedCounter;
        private PollingCounter? _bytesReceivedCounter;
        private PollingCounter? _bytesSentCounter;
        private PollingCounter? _datagramsReceivedCounter;
        private PollingCounter? _datagramsSentCounter;

        private long _outgoingConnectionsEstablished;
        private long _incomingConnectionsEstablished;
        private long _bytesReceived;
        private long _bytesSent;
        private long _datagramsReceived;
        private long _datagramsSent;

        [Event(1, Level = EventLevel.Informational)]
        private void ConnectStart(string? address)
        {
            WriteEvent(eventId: 1, address);
        }

        [Event(2, Level = EventLevel.Informational)]
        private void ConnectStop()
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 2);
            }
        }

        [Event(3, Level = EventLevel.Error)]
        private void ConnectFailed(SocketError error, string? exceptionMessage)
        {
            if (IsEnabled(EventLevel.Error, EventKeywords.All))
            {
                WriteEvent(eventId: 3, (int)error, exceptionMessage);
            }
        }

        [Event(4, Level = EventLevel.Informational)]
        private void AcceptStart(string? address)
        {
            WriteEvent(eventId: 4, address);
        }

        [Event(5, Level = EventLevel.Informational)]
        private void AcceptStop()
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 5);
            }
        }

        [Event(6, Level = EventLevel.Error)]
        private void AcceptFailed(SocketError error, string? exceptionMessage)
        {
            if (IsEnabled(EventLevel.Error, EventKeywords.All))
            {
                WriteEvent(eventId: 6, (int)error, exceptionMessage);
            }
        }

        [NonEvent]
        public void ConnectStart(Internals.SocketAddress address)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                ConnectStart(address.ToString());
            }
        }

        [NonEvent]
        public void AfterConnect(SocketError error, string? exceptionMessage = null)
        {
            if (error == SocketError.Success)
            {
                Debug.Assert(exceptionMessage is null);
                Interlocked.Increment(ref _outgoingConnectionsEstablished);
            }
            else
            {
                ConnectFailed(error, exceptionMessage);
            }

            ConnectStop();
        }

        [NonEvent]
        public void AcceptStart(Internals.SocketAddress address)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                AcceptStart(address.ToString());
            }
        }

        [NonEvent]
        public void AcceptStart(EndPoint address)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                AcceptStart(address.Serialize().ToString());
            }
        }

        [NonEvent]
        public void AfterAccept(SocketError error, string? exceptionMessage = null)
        {
            if (error == SocketError.Success)
            {
                Debug.Assert(exceptionMessage is null);
                Interlocked.Increment(ref _incomingConnectionsEstablished);
            }
            else
            {
                AcceptFailed(error, exceptionMessage);
            }

            AcceptStop();
        }

        [NonEvent]
        public void BytesReceived(int count)
        {
            Debug.Assert(count >= 0);
            Interlocked.Add(ref _bytesReceived, count);
        }

        [NonEvent]
        public void BytesSent(int count)
        {
            Debug.Assert(count >= 0);
            Interlocked.Add(ref _bytesSent, count);
        }

        [NonEvent]
        public void DatagramReceived()
        {
            Interlocked.Increment(ref _datagramsReceived);
        }

        [NonEvent]
        public void DatagramSent()
        {
            Interlocked.Increment(ref _datagramsSent);
        }

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).

                _outgoingConnectionsEstablishedCounter ??= new PollingCounter("outgoing-connections-established", this, () => Interlocked.Read(ref _outgoingConnectionsEstablished))
                {
                    DisplayName = "Outgoing Connections Established",
                };
                _incomingConnectionsEstablishedCounter ??= new PollingCounter("incoming-connections-established", this, () => Interlocked.Read(ref _incomingConnectionsEstablished))
                {
                    DisplayName = "Incoming Connections Established",
                };
                _bytesReceivedCounter ??= new PollingCounter("bytes-received", this, () => Interlocked.Read(ref _bytesReceived))
                {
                    DisplayName = "Bytes Received",
                };
                _bytesSentCounter ??= new PollingCounter("bytes-sent", this, () => Interlocked.Read(ref _bytesSent))
                {
                    DisplayName = "Bytes Sent",
                };
                _datagramsReceivedCounter ??= new PollingCounter("datagrams-received", this, () => Interlocked.Read(ref _datagramsReceived))
                {
                    DisplayName = "Datagrams Received",
                };
                _datagramsSentCounter ??= new PollingCounter("datagrams-sent", this, () => Interlocked.Read(ref _datagramsSent))
                {
                    DisplayName = "Datagrams Sent",
                };
            }
        }
    }
}
