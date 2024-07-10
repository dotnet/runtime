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
        private const string ActivitySourceName = "Experimental.System.Net.Sockets";
        private const string ConnectActivityName = ActivitySourceName + ".Connect";
        private static readonly ActivitySource s_connectActivitySource = new(ActivitySourceName);

        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

        private PollingCounter? _currentOutgoingConnectAttemptsCounter;
        private PollingCounter? _outgoingConnectionsEstablishedCounter;
        private PollingCounter? _incomingConnectionsEstablishedCounter;
        private PollingCounter? _bytesReceivedCounter;
        private PollingCounter? _bytesSentCounter;
        private PollingCounter? _datagramsReceivedCounter;
        private PollingCounter? _datagramsSentCounter;

        private long _currentOutgoingConnectAttempts;
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
        public Activity? ConnectStart(SocketAddress address, EndPoint endPoint, bool keepActivityCurrent)
        {
            Interlocked.Increment(ref _currentOutgoingConnectAttempts);

            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                ConnectStart(address.ToString());
            }

            Activity? activity = null;
            if (s_connectActivitySource.HasListeners())
            {
                Activity? activityToReset = keepActivityCurrent ? Activity.Current : null;
                activity = s_connectActivitySource.StartActivity(ConnectActivityName, ActivityKind.Client);
                if (keepActivityCurrent)
                {
                    // Do not overwrite Activity.Current in the caller's ExecutionContext.
                    Activity.Current = activityToReset;
                }
            }

            if (activity is not null)
            {
                if (endPoint is IPEndPoint ipEndPoint)
                {
                    int port = ipEndPoint.Port;
                    activity.DisplayName = $"socket connect {ipEndPoint.Address}:{port}";
                    if (activity.IsAllDataRequested)
                    {
                        activity.SetTag("network.peer.address", ipEndPoint.Address.ToString());
                        activity.SetTag("network.peer.port", port);
                        activity.SetTag("network.type", ipEndPoint.AddressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6");
                    }
                }
                else if (endPoint is UnixDomainSocketEndPoint udsEndPoint)
                {
                    string peerAddress = udsEndPoint.ToString();
                    activity.DisplayName = $"socket connect {peerAddress}";

                    if (activity.IsAllDataRequested)
                    {
                        activity.SetTag("network.peer.address", peerAddress);
                    }
                }
            }

            return activity;
        }

        [NonEvent]
        public void AfterConnect(SocketError error, Activity? activity, string? exceptionMessage = null)
        {
            long newCount = Interlocked.Decrement(ref _currentOutgoingConnectAttempts);
            Debug.Assert(newCount >= 0);

            if (activity is not null)
            {
                if (error != SocketError.Success)
                {
                    activity.SetStatus(ActivityStatusCode.Error);
                    activity.SetTag("error.type", GetErrorType(error));
                }

                activity.Stop();
            }

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
        public void AcceptStart(SocketAddress address)
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

        private static string GetErrorType(SocketError socketError) => socketError switch
        {
            // Common connect() errors expected to be seen:
            // https://learn.microsoft.com/en-us/windows/win32/api/winsock2/nf-winsock2-connect#return-value
            // https://man7.org/linux/man-pages/man2/connect.2.html
            SocketError.NetworkDown => "network_down",
            SocketError.AddressAlreadyInUse => "address_already_in_use",
            SocketError.Interrupted => "interrupted",
            SocketError.InProgress => "in_progress",
            SocketError.AlreadyInProgress => "already_in_progress",
            SocketError.AddressNotAvailable => "address_not_available",
            SocketError.AddressFamilyNotSupported => "address_family_not_supported",
            SocketError.ConnectionRefused => "connection_refused",
            SocketError.Fault => "fault",
            SocketError.InvalidArgument => "invalid_argument",
            SocketError.IsConnected => "is_connected",
            SocketError.NetworkUnreachable => "network_unreachable",
            SocketError.HostUnreachable => "host_unreachable",
            SocketError.NoBufferSpaceAvailable => "no_buffer_space_available",
            SocketError.TimedOut => "timed_out",
            SocketError.AccessDenied => "access_denied",
            SocketError.ProtocolType => "protocol_type",

            _ => "_OTHER"
        };

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // This is the convention for initializing counters in the RuntimeEventSource (lazily on the first enable command).

                _currentOutgoingConnectAttemptsCounter ??= new PollingCounter("current-outgoing-connect-attempts", this, () => Interlocked.Read(ref _currentOutgoingConnectAttempts))
                {
                    DisplayName = "Current Outgoing Connect Attempts",
                };
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
