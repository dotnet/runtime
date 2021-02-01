// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    public class TelemetryTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Dns).Assembly.GetType("System.Net.NameResolutionTelemetry", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.NameResolution", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("4b326142-bfb5-5ed3-8585-7714181d14b0"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EventSource_ResolveValidHostName_LogsStartStop()
        {
            RemoteExecutor.Invoke(async () =>
            {
                const string ValidHostName = "microsoft.com";

                using var listener = new TestEventListener("System.Net.NameResolution", EventLevel.Informational);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    await Dns.GetHostEntryAsync(ValidHostName);
                    await Dns.GetHostAddressesAsync(ValidHostName);

                    Dns.GetHostEntry(ValidHostName);
                    Dns.GetHostAddresses(ValidHostName);

                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidHostName, null, null));
                    Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(ValidHostName, null, null));
                });

                Assert.DoesNotContain(events, e => e.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs[] starts = events.Where(e => e.EventName == "ResolutionStart").ToArray();
                Assert.Equal(6, starts.Length);
                Assert.All(starts, s => Assert.Equal(ValidHostName, Assert.Single(s.Payload).ToString()));

                EventWrittenEventArgs[] stops = events.Where(e => e.EventName == "ResolutionStop").ToArray();
                Assert.Equal(6, stops.Length);

                Assert.DoesNotContain(events, e => e.EventName == "ResolutionFailed");
            }).Dispose();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EventSource_ResolveInvalidHostName_LogsStartFailureStop()
        {
            RemoteExecutor.Invoke(async () =>
            {
                const string InvalidHostName = "invalid...example.com";

                using var listener = new TestEventListener("System.Net.NameResolution", EventLevel.Informational);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostEntryAsync(InvalidHostName));
                    await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostAddressesAsync(InvalidHostName));

                    Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(InvalidHostName));
                    Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(InvalidHostName));

                    Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(Dns.BeginGetHostEntry(InvalidHostName, null, null)));
                    Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(InvalidHostName, null, null)));
                });

                Assert.DoesNotContain(events, e => e.EventId == 0); // errors from the EventSource itself

                EventWrittenEventArgs[] starts = events.Where(e => e.EventName == "ResolutionStart").ToArray();
                Assert.Equal(6, starts.Length);
                Assert.All(starts, s => Assert.Equal(InvalidHostName, Assert.Single(s.Payload).ToString()));

                EventWrittenEventArgs[] failures = events.Where(e => e.EventName == "ResolutionFailed").ToArray();
                Assert.Equal(6, failures.Length);

                EventWrittenEventArgs[] stops = events.Where(e => e.EventName == "ResolutionStop").ToArray();
                Assert.Equal(6, stops.Length);
            }).Dispose();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void EventSource_GetHostEntryForIP_LogsStartStop()
        {
            RemoteExecutor.Invoke(async () =>
            {
                const string ValidIPAddress = "8.8.4.4";

                using var listener = new TestEventListener("System.Net.NameResolution", EventLevel.Informational);

                var events = new ConcurrentQueue<EventWrittenEventArgs>();
                await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                {
                    IPAddress ipAddress = IPAddress.Parse(ValidIPAddress);

                    await Dns.GetHostEntryAsync(ValidIPAddress);
                    await Dns.GetHostEntryAsync(ipAddress);

                    Dns.GetHostEntry(ValidIPAddress);
                    Dns.GetHostEntry(ipAddress);

                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidIPAddress, null, null));
                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ipAddress, null, null));
                });

                Assert.DoesNotContain(events, e => e.EventId == 0); // errors from the EventSource itself

                // Each GetHostEntry over an IP will yield 2 resolutions
                EventWrittenEventArgs[] starts = events.Where(e => e.EventName == "ResolutionStart").ToArray();
                Assert.Equal(12, starts.Length);
                Assert.Equal(6, starts.Count(s => Assert.Single(s.Payload).ToString() == ValidIPAddress));

                EventWrittenEventArgs[] stops = events.Where(e => e.EventName == "ResolutionStop").ToArray();
                Assert.Equal(12, stops.Length);

                Assert.DoesNotContain(events, e => e.EventName == "ResolutionFailed");
            }).Dispose();
        }
    }
}
