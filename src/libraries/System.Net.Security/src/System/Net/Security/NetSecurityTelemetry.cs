// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Security.Authentication;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace System.Net.Security
{
    [EventSource(Name = "System.Net.Security")]
    internal sealed class NetSecurityTelemetry : EventSource
    {
        public static readonly NetSecurityTelemetry Log = new NetSecurityTelemetry();

        private IncrementingPollingCounter? _tlsHandshakeRateCounter;
        private PollingCounter? _totalTlsHandshakesCounter;
        private PollingCounter? _currentTlsHandshakesCounter;
        private PollingCounter? _failedTlsHandshakesCounter;
        private PollingCounter? _connectionsOpenTls10Counter;
        private PollingCounter? _connectionsOpenTls11Counter;
        private PollingCounter? _connectionsOpenTls12Counter;
        private PollingCounter? _connectionsOpenTls13Counter;
        private EventCounter? _handshakeDurationTls10Counter;
        private EventCounter? _handshakeDurationTls11Counter;
        private EventCounter? _handshakeDurationTls12Counter;
        private EventCounter? _handshakeDurationTls13Counter;

        private long _finishedTlsHandshakes; // Successfully and failed
        private long _startedTlsHandshakes;
        private long _failedTlsHandshakes;
        private long _connectionsOpenTls10;
        private long _connectionsOpenTls11;
        private long _connectionsOpenTls12;
        private long _connectionsOpenTls13;

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                _tlsHandshakeRateCounter ??= new IncrementingPollingCounter("tls-handshake-rate", this, () => Interlocked.Read(ref _finishedTlsHandshakes))
                {
                    DisplayName = "TLS handshakes completed",
                    DisplayRateTimeScale = TimeSpan.FromSeconds(1)
                };

                _totalTlsHandshakesCounter ??= new PollingCounter("total-tls-handshakes", this, () => Interlocked.Read(ref _finishedTlsHandshakes))
                {
                    DisplayName = "Total TLS handshakes completed"
                };

                _currentTlsHandshakesCounter ??= new PollingCounter("current-tls-handshakes", this, () => -Interlocked.Read(ref _finishedTlsHandshakes) + Interlocked.Read(ref _startedTlsHandshakes))
                {
                    DisplayName = "Current TLS handshakes"
                };

                _failedTlsHandshakesCounter ??= new PollingCounter("failed-tls-handshakes", this, () => Interlocked.Read(ref _failedTlsHandshakes))
                {
                    DisplayName = "Total TLS handshakes failed"
                };

                _connectionsOpenTls10Counter ??= new PollingCounter("tls10-connections-open", this, () => Interlocked.Read(ref _connectionsOpenTls10))
                {
                    DisplayName = "TLS 1.0 Connections Active"
                };

                _connectionsOpenTls11Counter ??= new PollingCounter("tls11-connections-open", this, () => Interlocked.Read(ref _connectionsOpenTls11))
                {
                    DisplayName = "TLS 1.1 Connections Active"
                };

                _connectionsOpenTls12Counter ??= new PollingCounter("tls12-connections-open", this, () => Interlocked.Read(ref _connectionsOpenTls12))
                {
                    DisplayName = "TLS 1.2 Connections Active"
                };

                _connectionsOpenTls13Counter ??= new PollingCounter("tls13-connections-open", this, () => Interlocked.Read(ref _connectionsOpenTls13))
                {
                    DisplayName = "TLS 1.3 Connections Active"
                };

                _handshakeDurationTls10Counter ??= new EventCounter("tls10-handshake-duration", this)
                {
                    DisplayName = "TLS 1.0 Handshake Duration",
                    DisplayUnits = "ms"
                };

                _handshakeDurationTls11Counter ??= new EventCounter("tls11-handshake-duration", this)
                {
                    DisplayName = "TLS 1.1 Handshake Duration",
                    DisplayUnits = "ms"
                };

                _handshakeDurationTls12Counter ??= new EventCounter("tls12-handshake-duration", this)
                {
                    DisplayName = "TLS 1.2 Handshake Duration",
                    DisplayUnits = "ms"
                };

                _handshakeDurationTls13Counter ??= new EventCounter("tls13-handshake-duration", this)
                {
                    DisplayName = "TLS 1.3 Handshake Duration",
                    DisplayUnits = "ms"
                };
            }
        }


        [Event(1, Level = EventLevel.Informational)]
        public void HandshakeStart(bool isServer, string targetHost)
        {
            Interlocked.Increment(ref _startedTlsHandshakes);

            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                WriteEvent(eventId: 1, isServer, targetHost);
            }
        }

        [Event(2, Level = EventLevel.Informational)]
        private void HandshakeStop(string protocol)
        {
            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                WriteEvent(eventId: 2, protocol);
            }
        }

        [Event(3, Level = EventLevel.Error)]
        private void HandshakeFailed(bool isServer, double elapsedMilliseconds, string exceptionMessage)
        {
            WriteEvent(eventId: 3, isServer, elapsedMilliseconds, exceptionMessage);
        }


        [NonEvent]
        public void HandshakeFailed(bool isServer, ValueStopwatch stopwatch, string exceptionMessage)
        {
            Interlocked.Increment(ref _finishedTlsHandshakes);
            Interlocked.Increment(ref _failedTlsHandshakes);

            if (IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                HandshakeFailed(isServer, stopwatch.GetElapsedTime().TotalMilliseconds, exceptionMessage);
            }

            HandshakeStop(protocol: string.Empty);
        }

        [NonEvent]
        public void HandshakeCompleted(SslProtocols protocol, ValueStopwatch stopwatch, bool connectionOpen)
        {
            Interlocked.Increment(ref _finishedTlsHandshakes);

            long dummy = 0;
            ref long connectionsOpen = ref dummy;
            EventCounter? handshakeDurationCounter = null;

            switch (protocol)
            {
                case SslProtocols.Tls:
                    connectionsOpen = ref _connectionsOpenTls10;
                    handshakeDurationCounter = _handshakeDurationTls10Counter;
                    break;

                case SslProtocols.Tls11:
                    connectionsOpen = ref _connectionsOpenTls11;
                    handshakeDurationCounter = _handshakeDurationTls11Counter;
                    break;

                case SslProtocols.Tls12:
                    connectionsOpen = ref _connectionsOpenTls12;
                    handshakeDurationCounter = _handshakeDurationTls12Counter;
                    break;

                case SslProtocols.Tls13:
                    connectionsOpen = ref _connectionsOpenTls13;
                    handshakeDurationCounter = _handshakeDurationTls13Counter;
                    break;
            }

            if (connectionOpen)
            {
                Interlocked.Increment(ref connectionsOpen);
            }

            if (handshakeDurationCounter != null)
            {
                handshakeDurationCounter.WriteMetric(stopwatch.GetElapsedTime().TotalMilliseconds);
            }

            if (IsEnabled(EventLevel.Informational, EventKeywords.None))
            {
                HandshakeStop(protocol.ToString());
            }
        }

        [NonEvent]
        public void ConnectionClosed(SslProtocols protocol)
        {
            long count = 0;

            switch (protocol)
            {
                case SslProtocols.Tls:
                    count = Interlocked.Decrement(ref _connectionsOpenTls10);
                    break;

                case SslProtocols.Tls11:
                    count = Interlocked.Decrement(ref _connectionsOpenTls11);
                    break;

                case SslProtocols.Tls12:
                    count = Interlocked.Decrement(ref _connectionsOpenTls12);
                    break;

                case SslProtocols.Tls13:
                    count = Interlocked.Decrement(ref _connectionsOpenTls13);
                    break;
            }

            Debug.Assert(count >= 0);
        }


        [NonEvent]
        private unsafe void WriteEvent(int eventId, bool arg1, string? arg2)
        {
            if (IsEnabled())
            {
                arg2 ??= string.Empty;

                fixed (char* arg2Ptr = arg2)
                {
                    const int NumEventDatas = 2;
                    EventData* descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg1),
                        Size = sizeof(int) // EventSource defines bool as a 32-bit type
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(arg2Ptr),
                        Size = (arg2.Length + 1) * sizeof(char)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, bool arg1, double arg2, string? arg3)
        {
            if (IsEnabled())
            {
                arg3 ??= string.Empty;

                fixed (char* arg3Ptr = arg3)
                {
                    const int NumEventDatas = 3;
                    EventData* descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg1),
                        Size = sizeof(int) // EventSource defines bool as a 32-bit type
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg2),
                        Size = sizeof(double)
                    };
                    descrs[2] = new EventData
                    {
                        DataPointer = (IntPtr)(arg3Ptr),
                        Size = (arg3.Length + 1) * sizeof(char)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }
    }
}
