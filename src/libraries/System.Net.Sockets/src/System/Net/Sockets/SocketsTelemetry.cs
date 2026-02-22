// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading;

namespace System.Net.Sockets
{
    [EventSource(Name = "System.Net.Sockets")]
    internal sealed partial class SocketsTelemetry : EventSource
    {
        private const string ActivitySourceName = "Experimental.System.Net.Sockets";
        private const string ConnectActivityName = ActivitySourceName + ".Connect";
        private static readonly ActivitySource s_connectActivitySource = new ActivitySource(ActivitySourceName);

        internal static class IoUringCounterNames
        {
            internal const string PrepareNonPinnableFallbacks = "io-uring-prepare-nonpinnable-fallbacks";
            internal const string SocketEventBufferFull = "io-uring-socket-event-buffer-full";
            internal const string CqOverflows = "io-uring-cq-overflows";
            internal const string CqOverflowRecoveries = "io-uring-cq-overflow-recoveries";
            internal const string PrepareQueueOverflows = "io-uring-prepare-queue-overflows";
            internal const string PrepareQueueOverflowFallbacks = "io-uring-prepare-queue-overflow-fallbacks";
            internal const string CompletionSlotExhaustions = "io-uring-completion-slot-exhaustions";
            internal const string CompletionSlotHighWaterMark = "io-uring-completion-slot-high-water-mark";
            internal const string CancellationQueueOverflows = "io-uring-cancellation-queue-overflows";
            internal const string ProvidedBufferDepletions = "io-uring-provided-buffer-depletions";
            internal const string SqPollWakeups = "io-uring-sqpoll-wakeups";
            internal const string SqPollSubmissionsSkipped = "io-uring-sqpoll-submissions-skipped";
        }

        public static readonly SocketsTelemetry Log = new SocketsTelemetry();

        private PollingCounter? _currentOutgoingConnectAttemptsCounter;
        private PollingCounter? _outgoingConnectionsEstablishedCounter;
        private PollingCounter? _incomingConnectionsEstablishedCounter;
        private PollingCounter? _bytesReceivedCounter;
        private PollingCounter? _bytesSentCounter;
        private PollingCounter? _datagramsReceivedCounter;
        private PollingCounter? _datagramsSentCounter;
        // Keep io_uring counter backing fields always present so EventCounter name contracts remain stable
        // across platforms; OnEventCommand only registers these counters on Linux.
        private PollingCounter? _ioUringPrepareNonPinnableFallbacksCounter;
        private PollingCounter? _ioUringSocketEventBufferFullCounter;
        private PollingCounter? _ioUringCqOverflowCounter;
        private PollingCounter? _ioUringCqOverflowRecoveriesCounter;
        private PollingCounter? _ioUringPrepareQueueOverflowsCounter;
        private PollingCounter? _ioUringPrepareQueueOverflowFallbacksCounter;
        private PollingCounter? _ioUringCompletionSlotExhaustionsCounter;
        private PollingCounter? _ioUringCompletionSlotHighWaterMarkCounter;
        private PollingCounter? _ioUringCancellationQueueOverflowsCounter;
        private PollingCounter? _ioUringProvidedBufferDepletionsCounter;
        private PollingCounter? _ioUringSqPollWakeupsCounter;
        private PollingCounter? _ioUringSqPollSubmissionsSkippedCounter;

        private long _currentOutgoingConnectAttempts;
        private long _outgoingConnectionsEstablished;
        private long _incomingConnectionsEstablished;
        private long _bytesReceived;
        private long _bytesSent;
        private long _datagramsReceived;
        private long _datagramsSent;
        // Backing fields stay cross-platform for contract stability; they are only surfaced as counters on Linux.
        private long _ioUringPrepareNonPinnableFallbacks;
        private long _ioUringSocketEventBufferFull;
        private long _ioUringCqOverflow;
        private long _ioUringCqOverflowRecoveries;
        private long _ioUringPrepareQueueOverflows;
        private long _ioUringPrepareQueueOverflowFallbacks;
        private long _ioUringCompletionSlotExhaustions;
        private long _ioUringCompletionSlotHighWaterMark;
        private long _ioUringCancellationQueueOverflows;
        private long _ioUringProvidedBufferDepletions;
        private long _ioUringSqPollWakeups;
        private long _ioUringSqPollSubmissionsSkipped;

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

        [Event(7, Level = EventLevel.Informational)]
        private void SocketEngineBackendSelected(string backend, int isIoUringPort, int sqPollEnabled)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 7, backend, isIoUringPort, sqPollEnabled);
            }
        }

        [Event(8, Level = EventLevel.Warning)]
        private void IoUringSqPollNegotiatedWarning(string message)
        {
            if (IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                WriteEvent(eventId: 8, message);
            }
        }

        [Event(9, Level = EventLevel.Informational)]
        private void IoUringResolvedConfiguration(string configuration)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                WriteEvent(eventId: 9, configuration);
            }
        }

        [NonEvent]
        public Activity? ConnectStart(SocketAddress address, ProtocolType protocolType, EndPoint endPoint, bool keepActivityCurrent)
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
                activity = s_connectActivitySource.StartActivity(ConnectActivityName);
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
                        if (protocolType is ProtocolType.Tcp)
                        {
                            SetNetworkTransport(activity, "tcp");
                        }
                        else if (protocolType is ProtocolType.Udp)
                        {
                            SetNetworkTransport(activity, "udp");
                        }
                    }
                }
                else if (endPoint is UnixDomainSocketEndPoint udsEndPoint)
                {
                    string peerAddress = udsEndPoint.ToString();
                    activity.DisplayName = $"socket connect {peerAddress}";

                    if (activity.IsAllDataRequested)
                    {
                        activity.SetTag("network.peer.address", peerAddress);
                        SetNetworkTransport(activity, "unix");
                    }
                }
            }

            static void SetNetworkTransport(Activity activity, string transportType) => activity.SetTag("network.transport", transportType);

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
        internal void ReportSocketEngineBackendSelected(bool isIoUringPort, bool isCompletionMode, bool sqPollEnabled)
        {
            if (!IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                return;
            }

            SocketEngineBackendSelected(
                isCompletionMode ? "io_uring_completion" : "epoll",
                isIoUringPort ? 1 : 0,
                sqPollEnabled ? 1 : 0);
        }

        [NonEvent]
        internal void ReportIoUringSqPollNegotiatedWarning()
        {
            if (!IsEnabled(EventLevel.Warning, EventKeywords.All))
            {
                return;
            }

            IoUringSqPollNegotiatedWarning(
                "io_uring SQPOLL negotiated: kernel polling thread is enabled and may increase privileges in containerized environments.");
        }

        [NonEvent]
        internal void ReportIoUringResolvedConfiguration(string configuration)
        {
            if (!IsEnabled(EventLevel.Informational, EventKeywords.All))
            {
                return;
            }

            IoUringResolvedConfiguration(configuration);
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

        [NonEvent]
        public void IoUringPrepareNonPinnableFallback(long count = 1)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringPrepareNonPinnableFallbacks, count);
        }

        [NonEvent]
        public void IoUringSocketEventBufferFull(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringSocketEventBufferFull, count);
        }

        [NonEvent]
        public void IoUringCqOverflow(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringCqOverflow, count);
        }

        [NonEvent]
        public void IoUringCqOverflowRecovery(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringCqOverflowRecoveries, count);
        }

        [NonEvent]
        public void IoUringPrepareQueueOverflow(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringPrepareQueueOverflows, count);
        }

        [NonEvent]
        public void IoUringPrepareQueueOverflowFallback(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringPrepareQueueOverflowFallbacks, count);
        }

        [NonEvent]
        public void IoUringCompletionSlotExhaustion(long count)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringCompletionSlotExhaustions, count);
        }

        [NonEvent]
        public void IoUringCompletionSlotHighWaterMark(long count)
        {
            Debug.Assert(count >= 0);
            while (true)
            {
                long observed = Volatile.Read(ref _ioUringCompletionSlotHighWaterMark);
                if (count <= observed)
                {
                    return;
                }

                if (Interlocked.CompareExchange(ref _ioUringCompletionSlotHighWaterMark, count, observed) == observed)
                {
                    return;
                }
            }
        }

        [NonEvent]
        public void IoUringCancellationQueueOverflow(long count = 1)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringCancellationQueueOverflows, count);
        }

        [NonEvent]
        public void IoUringProvidedBufferDepletion(long count = 1)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringProvidedBufferDepletions, count);
        }

        [NonEvent]
        public void IoUringSqPollWakeup(long count = 1)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringSqPollWakeups, count);
        }

        [NonEvent]
        public void IoUringSqPollSubmissionSkipped(long count = 1)
        {
            Debug.Assert(count >= 0);
            if (IsEnabled())
                Interlocked.Add(ref _ioUringSqPollSubmissionsSkipped, count);
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

                if (!OperatingSystem.IsLinux())
                {
                    return;
                }

                _ioUringPrepareNonPinnableFallbacksCounter ??= new PollingCounter(IoUringCounterNames.PrepareNonPinnableFallbacks, this, () => Interlocked.Read(ref _ioUringPrepareNonPinnableFallbacks))
                {
                    DisplayName = "io_uring Prepare Non-Pinnable Fallbacks",
                };
                _ioUringSocketEventBufferFullCounter ??= new PollingCounter(IoUringCounterNames.SocketEventBufferFull, this, () => Interlocked.Read(ref _ioUringSocketEventBufferFull))
                {
                    DisplayName = "io_uring Socket Event Buffer Full",
                };
                _ioUringCqOverflowCounter ??= new PollingCounter(IoUringCounterNames.CqOverflows, this, () => Interlocked.Read(ref _ioUringCqOverflow))
                {
                    DisplayName = "io_uring Completion Queue Overflow",
                };
                _ioUringCqOverflowRecoveriesCounter ??= new PollingCounter(IoUringCounterNames.CqOverflowRecoveries, this, () => Interlocked.Read(ref _ioUringCqOverflowRecoveries))
                {
                    DisplayName = "io_uring Completion Queue Overflow Recoveries",
                };
                _ioUringPrepareQueueOverflowsCounter ??= new PollingCounter(IoUringCounterNames.PrepareQueueOverflows, this, () => Interlocked.Read(ref _ioUringPrepareQueueOverflows))
                {
                    DisplayName = "io_uring Prepare Queue Overflows",
                };
                _ioUringPrepareQueueOverflowFallbacksCounter ??= new PollingCounter(IoUringCounterNames.PrepareQueueOverflowFallbacks, this, () => Interlocked.Read(ref _ioUringPrepareQueueOverflowFallbacks))
                {
                    DisplayName = "io_uring Prepare Queue Overflow Fallbacks",
                };
                _ioUringCompletionSlotExhaustionsCounter ??= new PollingCounter(IoUringCounterNames.CompletionSlotExhaustions, this, () => Interlocked.Read(ref _ioUringCompletionSlotExhaustions))
                {
                    DisplayName = "io_uring Completion Slot Exhaustions",
                };
                _ioUringCompletionSlotHighWaterMarkCounter ??= new PollingCounter(IoUringCounterNames.CompletionSlotHighWaterMark, this, () => Interlocked.Read(ref _ioUringCompletionSlotHighWaterMark))
                {
                    DisplayName = "io_uring Completion Slot High-Water Mark",
                };
                _ioUringCancellationQueueOverflowsCounter ??= new PollingCounter(IoUringCounterNames.CancellationQueueOverflows, this, () => Interlocked.Read(ref _ioUringCancellationQueueOverflows))
                {
                    DisplayName = "io_uring Cancellation Queue Overflows",
                };
                _ioUringProvidedBufferDepletionsCounter ??= new PollingCounter(IoUringCounterNames.ProvidedBufferDepletions, this, () => Interlocked.Read(ref _ioUringProvidedBufferDepletions))
                {
                    DisplayName = "io_uring Provided Buffer Depletions",
                };
                _ioUringSqPollWakeupsCounter ??= new PollingCounter(IoUringCounterNames.SqPollWakeups, this, () => Interlocked.Read(ref _ioUringSqPollWakeups))
                {
                    DisplayName = "io_uring SQPOLL Wakeups",
                };
                _ioUringSqPollSubmissionsSkippedCounter ??= new PollingCounter(IoUringCounterNames.SqPollSubmissionsSkipped, this, () => Interlocked.Read(ref _ioUringSqPollSubmissionsSkipped))
                {
                    DisplayName = "io_uring SQPOLL Submissions Skipped",
                };
            }
        }
    }
}
