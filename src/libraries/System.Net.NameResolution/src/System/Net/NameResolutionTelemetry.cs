// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace System.Net
{
    [EventSource(Name = "System.Net.NameResolution")]
    internal sealed class NameResolutionTelemetry : EventSource
    {
        public static readonly NameResolutionTelemetry Log = new NameResolutionTelemetry();

        public static new bool IsEnabled => Log.IsEnabled();

        private const int ResolutionStartEventId = 1;
        private const int ResolutionSuccessEventId = 2;
        private const int ResolutionFailureEventId = 3;

        private PollingCounter? _lookupsRequestedCounter;
        private EventCounter? _lookupsDuration;

        private long _lookupsRequested;

        protected override void OnEventCommand(EventCommandEventArgs command)
        {
            if (command.Command == EventCommand.Enable)
            {
                // The cumulative number of name resolution requests started since the process started.
                _lookupsRequestedCounter ??= new PollingCounter("dns-lookups-requested", this, () => Interlocked.Read(ref _lookupsRequested))
                {
                    DisplayName = "DNS Lookups Requested"
                };

                _lookupsDuration ??= new EventCounter("dns-lookups-duration", this)
                {
                    DisplayName = "Average DNS Lookup Duration",
                    DisplayUnits = "ms"
                };
            }
        }

        private const int MaxIPFormattedLength = 128;

        [Event(ResolutionStartEventId, Level = EventLevel.Informational)]
        public ValueStopwatch ResolutionStart(string hostNameOrAddress)
        {
            Debug.Assert(hostNameOrAddress != null);

            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);
                WriteEvent(ResolutionStartEventId, hostNameOrAddress);
                return ValueStopwatch.StartNew();
            }

            return default;
        }

        [NonEvent]
        public ValueStopwatch ResolutionStart(IPAddress address)
        {
            Debug.Assert(address != null);

            if (IsEnabled())
            {
                Interlocked.Increment(ref _lookupsRequested);
                WriteEvent(ResolutionStartEventId, FormatIPAddressNullTerminated(address, stackalloc char[MaxIPFormattedLength]));
                return ValueStopwatch.StartNew();
            }

            return default;
        }

        [NonEvent]
        public void AfterResolution(string hostNameOrAddress, ValueStopwatch stopwatch, bool successful)
        {
            if (stopwatch.IsActive)
            {
                double duration = stopwatch.GetElapsedTime().TotalMilliseconds;

                _lookupsDuration!.WriteMetric(duration);

                if (successful)
                {
                    ResolutionSuccess(hostNameOrAddress, duration);
                }
                else
                {
                    ResolutionFailure(hostNameOrAddress, duration);
                }
            }
        }

        [NonEvent]
        public void AfterResolution(IPAddress address, ValueStopwatch stopwatch, bool successful)
        {
            if (stopwatch.IsActive)
            {
                double duration = stopwatch.GetElapsedTime().TotalMilliseconds;

                _lookupsDuration!.WriteMetric(duration);

                WriteEvent(
                    successful ? ResolutionSuccessEventId : ResolutionFailureEventId,
                    FormatIPAddressNullTerminated(address, stackalloc char[MaxIPFormattedLength]),
                    duration);
            }
        }


        [Event(ResolutionSuccessEventId, Level = EventLevel.Informational)]
        private void ResolutionSuccess(string hostNameOrAddress, double duration) => WriteEvent(ResolutionSuccessEventId, hostNameOrAddress, duration);

        [Event(ResolutionFailureEventId, Level = EventLevel.Informational)]
        private void ResolutionFailure(string hostNameOrAddress, double duration) => WriteEvent(ResolutionFailureEventId, hostNameOrAddress, duration);


        [NonEvent]
        private static Span<char> FormatIPAddressNullTerminated(IPAddress address, Span<char> destination)
        {
            Debug.Assert(address != null);

            bool success = address.TryFormat(destination, out int charsWritten);
            Debug.Assert(success);

            Debug.Assert(charsWritten < destination.Length);
            destination[charsWritten] = '\0';

            return destination.Slice(0, charsWritten + 1);
        }


        [NonEvent]
        private unsafe void WriteEvent(int eventId, Span<char> arg1)
        {
            Debug.Assert(!arg1.IsEmpty && arg1[^1] == '\0', "Expecting a null-terminated ROS<char>");

            if (IsEnabled())
            {
                fixed (char* arg1Ptr = &MemoryMarshal.GetReference(arg1))
                {
                    EventData descr = new EventData
                    {
                        DataPointer = (IntPtr)(arg1Ptr),
                        Size = arg1.Length * sizeof(char)
                    };

                    WriteEventCore(eventId, eventDataCount: 1, &descr);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, string? arg1, double arg2)
        {
            if (IsEnabled())
            {
                arg1 ??= "";

                fixed (char* arg1Ptr = arg1)
                {
                    const int NumEventDatas = 2;
                    EventData* descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(arg1Ptr),
                        Size = (arg1.Length + 1) * sizeof(char)
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg2),
                        Size = sizeof(double)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }

        [NonEvent]
        private unsafe void WriteEvent(int eventId, Span<char> arg1, double arg2)
        {
            Debug.Assert(!arg1.IsEmpty && arg1[^1] == '\0', "Expecting a null-terminated ROS<char>");

            if (IsEnabled())
            {
                fixed (char* arg1Ptr = &MemoryMarshal.GetReference(arg1))
                {
                    const int NumEventDatas = 2;
                    EventData* descrs = stackalloc EventData[NumEventDatas];

                    descrs[0] = new EventData
                    {
                        DataPointer = (IntPtr)(arg1Ptr),
                        Size = arg1.Length * sizeof(char)
                    };
                    descrs[1] = new EventData
                    {
                        DataPointer = (IntPtr)(&arg2),
                        Size = sizeof(double)
                    };

                    WriteEventCore(eventId, NumEventDatas, descrs);
                }
            }
        }
    }
}
