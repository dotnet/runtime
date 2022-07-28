// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Diagnostics.CodeAnalysis;
using System.Security.Authentication;
using System.Threading;

namespace System.Net.Security
{
    [EventSource(Name = "System.Net.Security")]
    internal sealed class NetSecurityTelemetry : EventSource
    {
        private const string EventSourceSuppressMessage = "Parameters to this method are primitive and are trimmer safe";
        public static readonly NetSecurityTelemetry Log = new NetSecurityTelemetry();

        private IncrementingPollingCounter? _tlsHandshakeRateCounter;
        private PollingCounter? _totalTlsHandshakesCounter;
        private PollingCounter? _currentTlsHandshakesCounter;
        private PollingCounter? _failedTlsHandshakesCounter;
        private PollingCounter? _sessionsOpenCounter;
        private PollingCounter? _sessionsOpenTls10Counter;
        private PollingCounter? _sessionsOpenTls11Counter;
        private PollingCounter? _sessionsOpenTls12Counter;
        private PollingCounter? _sessionsOpenTls13Counter;
        private EventCounter? _handshakeDurationCounter;
        private EventCounter? _handshakeDurationTls10Counter;
        private EventCounter? _handshakeDurationTls11Counter;
        private EventCounter? _handshakeDurationTls12Counter;
        private EventCounter? _handshakeDurationTls13Counter;

        private long _finishedTlsHandshakes; // Successfully and failed
        private long _startedTlsHandshakes;
        private long _failedTlsHandshakes;
        private long _sessionsOpen;
        private long _sessionsOpenTls10;
        private long _sessionsOpenTls11;
        private long _sessionsOpenTls12;
        private long _sessionsOpenTls13;

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

                _sessionsOpenCounter ??= new PollingCounter("all-tls-sessions-open", this, () => Interlocked.Read(ref _sessionsOpen))
                {
                    DisplayName = "All TLS Sessions Active"
                };

                _sessionsOpenTls10Counter ??= new PollingCounter("tls10-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls10))
                {
                    DisplayName = "TLS 1.0 Sessions Active"
                };

                _sessionsOpenTls11Counter ??= new PollingCounter("tls11-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls11))
                {
                    DisplayName = "TLS 1.1 Sessions Active"
                };

                _sessionsOpenTls12Counter ??= new PollingCounter("tls12-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls12))
                {
                    DisplayName = "TLS 1.2 Sessions Active"
                };

                _sessionsOpenTls13Counter ??= new PollingCounter("tls13-sessions-open", this, () => Interlocked.Read(ref _sessionsOpenTls13))
                {
                    DisplayName = "TLS 1.3 Sessions Active"
                };

                _handshakeDurationCounter ??= new EventCounter("all-tls-handshake-duration", this)
                {
                    DisplayName = "TLS Handshake Duration",
                    DisplayUnits = "ms"
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
        private void HandshakeStop(SslProtocols protocol)
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
        public void HandshakeFailed(bool isServer, long startingTimestamp, string exceptionMessage)
        {
            Interlocked.Increment(ref _finishedTlsHandshakes);
            Interlocked.Increment(ref _failedTlsHandshakes);

            if (IsEnabled(EventLevel.Error, EventKeywords.None))
            {
                HandshakeFailed(isServer, Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds, exceptionMessage);
            }

            HandshakeStop(SslProtocols.None);
        }

        [NonEvent]
        public void HandshakeCompleted(SslProtocols protocol, long startingTimestamp, bool connectionOpen)
        {
            Interlocked.Increment(ref _finishedTlsHandshakes);

            long dummy = 0;
            ref long protocolSessionsOpen = ref dummy;
            EventCounter? handshakeDurationCounter = null;

            Debug.Assert(Enum.GetValues<SslProtocols>()[^1] == SslProtocols.Tls13, "Make sure to add a counter for new SslProtocols");

            switch (protocol)
            {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                case SslProtocols.Tls:
                    protocolSessionsOpen = ref _sessionsOpenTls10;
                    handshakeDurationCounter = _handshakeDurationTls10Counter;
                    break;

                case SslProtocols.Tls11:
                    protocolSessionsOpen = ref _sessionsOpenTls11;
                    handshakeDurationCounter = _handshakeDurationTls11Counter;
                    break;
#pragma warning restore SYSLIB0039

                case SslProtocols.Tls12:
                    protocolSessionsOpen = ref _sessionsOpenTls12;
                    handshakeDurationCounter = _handshakeDurationTls12Counter;
                    break;

                case SslProtocols.Tls13:
                    protocolSessionsOpen = ref _sessionsOpenTls13;
                    handshakeDurationCounter = _handshakeDurationTls13Counter;
                    break;
            }

            if (connectionOpen)
            {
                Interlocked.Increment(ref protocolSessionsOpen);
                Interlocked.Increment(ref _sessionsOpen);
            }

            double duration = Stopwatch.GetElapsedTime(startingTimestamp).TotalMilliseconds;
            handshakeDurationCounter?.WriteMetric(duration);
            _handshakeDurationCounter!.WriteMetric(duration);

            HandshakeStop(protocol);
        }

        [NonEvent]
        public void ConnectionClosed(SslProtocols protocol)
        {
            long count = 0;

            switch (protocol)
            {
#pragma warning disable SYSLIB0039 // TLS 1.0 and 1.1 are obsolete
                case SslProtocols.Tls:
                    count = Interlocked.Decrement(ref _sessionsOpenTls10);
                    break;

                case SslProtocols.Tls11:
                    count = Interlocked.Decrement(ref _sessionsOpenTls11);
                    break;
#pragma warning restore SYSLIB0039

                case SslProtocols.Tls12:
                    count = Interlocked.Decrement(ref _sessionsOpenTls12);
                    break;

                case SslProtocols.Tls13:
                    count = Interlocked.Decrement(ref _sessionsOpenTls13);
                    break;
            }

            Debug.Assert(count >= 0);

            count = Interlocked.Decrement(ref _sessionsOpen);
            Debug.Assert(count >= 0);
        }


        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
        [NonEvent]
        private unsafe void WriteEvent(int eventId, SslProtocols arg1)
        {
            if (IsEnabled())
            {
                var data = new EventData
                {
                    DataPointer = (IntPtr)(&arg1),
                    Size = sizeof(SslProtocols)
                };

                WriteEventCore(eventId, eventDataCount: 1, &data);
            }
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:UnrecognizedReflectionPattern",
                   Justification = EventSourceSuppressMessage)]
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
