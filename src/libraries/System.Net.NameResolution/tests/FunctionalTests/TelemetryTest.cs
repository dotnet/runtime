// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Sdk;

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
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    await Dns.GetHostEntryAsync(ValidHostName);
                    await Dns.GetHostAddressesAsync(ValidHostName);

                    Dns.GetHostEntry(ValidHostName);
                    Dns.GetHostAddresses(ValidHostName);

                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidHostName, null, null));
                    Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(ValidHostName, null, null));
                });

                VerifyEvents(events, ValidHostName, 6);
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
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostEntryAsync(InvalidHostName));
                    await Assert.ThrowsAnyAsync<SocketException>(async () => await Dns.GetHostAddressesAsync(InvalidHostName));

                    Assert.ThrowsAny<SocketException>(() => Dns.GetHostEntry(InvalidHostName));
                    Assert.ThrowsAny<SocketException>(() => Dns.GetHostAddresses(InvalidHostName));

                    Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostEntry(Dns.BeginGetHostEntry(InvalidHostName, null, null)));
                    Assert.ThrowsAny<SocketException>(() => Dns.EndGetHostAddresses(Dns.BeginGetHostAddresses(InvalidHostName, null, null)));
                });

                VerifyEvents(events, InvalidHostName, 6, shouldHaveFailures: true);
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
                listener.AddActivityTracking();

                var events = new ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)>();
                await listener.RunWithCallbackAsync(e => events.Enqueue((e, e.ActivityId)), async () =>
                {
                    IPAddress ipAddress = IPAddress.Parse(ValidIPAddress);

                    await Dns.GetHostEntryAsync(ValidIPAddress);
                    await Dns.GetHostEntryAsync(ipAddress);

                    Dns.GetHostEntry(ValidIPAddress);
                    Dns.GetHostEntry(ipAddress);

                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ValidIPAddress, null, null));
                    Dns.EndGetHostEntry(Dns.BeginGetHostEntry(ipAddress, null, null));
                });

                // Each GetHostEntry over an IP will yield 2 resolutions
                VerifyEvents(events, ValidIPAddress, 12, isHostEntryForIp: true);
            }).Dispose();
        }

        private static void VerifyEvents(ConcurrentQueue<(EventWrittenEventArgs Event, Guid ActivityId)> events, string hostname, int expectedNumber, bool shouldHaveFailures = false, bool isHostEntryForIp = false)
        {
            Assert.DoesNotContain(events, e => e.Event.EventId == 0); // errors from the EventSource itself

            (EventWrittenEventArgs Event, Guid ActivityId)[] starts = events.Where(e => e.Event.EventName == "ResolutionStart").ToArray();
            Assert.Equal(expectedNumber, starts.Length);

            int expectedHostnameStarts = isHostEntryForIp ? expectedNumber / 2 : expectedNumber;
            Assert.Equal(expectedHostnameStarts, starts.Count(s => Assert.Single(s.Event.Payload).ToString() == hostname));

            (EventWrittenEventArgs Event, Guid ActivityId)[] stops = events.Where(e => e.Event.EventName == "ResolutionStop").ToArray();
            Assert.Equal(expectedNumber, stops.Length);

            for (int i = 0; i < starts.Length; i++)
            {
                Assert.NotEqual(Guid.Empty, starts[i].ActivityId);
                Assert.Equal(starts[i].ActivityId, stops[i].ActivityId);
            }

            if (shouldHaveFailures)
            {
                (EventWrittenEventArgs Event, Guid ActivityId)[] failures = events.Where(e => e.Event.EventName == "ResolutionFailed").ToArray();
                Assert.Equal(expectedNumber, failures.Length);

                for (int i = 0; i < starts.Length; i++)
                {
                    Assert.Equal(starts[i].ActivityId, failures[i].ActivityId);
                }
            }
            else
            {
                Assert.DoesNotContain(events, e => e.Event.EventName == "ResolutionFailed");
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public static void ResolutionsWaitingOnQueue_ResolutionStartCalledBeforeEnqueued()
        {
            // Some platforms (non-Windows) don't have proper support for GetAddrInfoAsync.
            // Instead we perform async-over-sync with a per-host queue.
            // This test ensures that ResolutionStart events are written before waiting on the queue.

            // We do this by blocking the first ResolutionStart event.
            // If the event was logged after waiting on the queue, the second request would never complete.
            RemoteExecutor.Invoke(async () =>
            {
                using var listener = new TestEventListener("System.Net.NameResolution", EventLevel.Informational);
                listener.AddActivityTracking();

                TaskCompletionSource firstResolutionStart = new(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource secondResolutionStop = new(TaskCreationOptions.RunContinuationsAsynchronously);

                List<(string EventName, Guid ActivityId)> events = new();

                bool? callbackWaitTimedOut = null;

                await listener.RunWithCallbackAsync(e =>
                {
                    if (e.EventName == "ResolutionStart" || e.EventName == "ResolutionStop")
                    {
                        events.Add((e.EventName, e.ActivityId));
                    }

                    if (e.EventName == "ResolutionStart" && firstResolutionStart.TrySetResult())
                    {
                        callbackWaitTimedOut = !secondResolutionStop.Task.Wait(TimeSpan.FromSeconds(15));
                    }
                },
                async () =>
                {
                    Task first = DoResolutionAsync();

                    await firstResolutionStart.Task.WaitAsync(TimeSpan.FromSeconds(30));

                    Task second = DoResolutionAsync();

                    await Task.WhenAny(first, second).WaitAsync(TimeSpan.FromSeconds(30));
                    Assert.False(first.IsCompleted);

                    await second;
                    secondResolutionStop.SetResult();

                    await first.WaitAsync(TimeSpan.FromSeconds(30));

                    static Task DoResolutionAsync()
                    {
                        return Task.Run(async () =>
                        {
                            try
                            {
                                await Dns.GetHostAddressesAsync("microsoft.com");
                            }
                            catch { } // We don't care if the request failed, just that events were written properly
                        });
                    }
                });

                Assert.Equal(4, events.Count);
                Assert.Equal("ResolutionStart", events[0].EventName);
                Assert.Equal("ResolutionStart", events[1].EventName);
                Assert.Equal("ResolutionStop", events[2].EventName);
                Assert.Equal("ResolutionStop", events[3].EventName);
                Assert.All(events, e => Assert.NotEqual(Guid.Empty, e.ActivityId));
                Assert.Equal(events[0].ActivityId, events[3].ActivityId);
                Assert.Equal(events[1].ActivityId, events[2].ActivityId);
                Assert.NotEqual(events[0].ActivityId, events[1].ActivityId);

                Assert.False(callbackWaitTimedOut);
            }).Dispose();
        }
    }
}
