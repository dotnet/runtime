// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Net.NameResolution.Tests
{
    using Configuration = System.Net.Test.Common.Configuration;

    [Collection(nameof(DisableParallelization))]
    public class LoggingTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Dns).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.NameResolution", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("460a591a-715b-5647-5264-944bef811147"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetHostEntry_InvalidHost_LogsError()
        {
            try
            {
                RemoteExecutor.Invoke(static () =>
                {
                    using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.NameResolution", EventLevel.Error))
                    {
                        var events = new ConcurrentQueue<EventWrittenEventArgs>();

                        listener.RunWithCallback(ev => events.Enqueue(ev), () =>
                        {
                            try
                            {
                                Dns.GetHostEntry(Configuration.Sockets.InvalidHost);
                                throw new SkipTestException("GetHostEntry should fail but it did not.");
                            }
                            catch (SocketException e) when (e.SocketErrorCode == SocketError.HostNotFound)
                            {
                            }
                            catch (Exception e)
                            {
                                throw new SkipTestException($"GetHostEntry failed unexpectedly: {e.Message}");
                            }
                        });

                        Assert.True(events.Count > 0, "events.Count should be > 0");
                        foreach (EventWrittenEventArgs ev in events)
                        {
                            Assert.True(ev.Payload.Count >= 3);
                            Assert.NotNull(ev.Payload[0]);
                            Assert.NotNull(ev.Payload[1]);
                            Assert.NotNull(ev.Payload[2]);
                        }
                    }
                }).Dispose();
            }
            catch (Exception ex) when (ex.ToString().Contains(nameof(SkipTestException), StringComparison.Ordinal))
            {
                throw new SkipTestException(ex.ToString());
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetHostEntryAsync_InvalidHost_LogsError()
        {
            try
            {
                RemoteExecutor.Invoke(static async () =>
                {
                    using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.NameResolution", EventLevel.Error))
                    {
                        var events = new ConcurrentQueue<EventWrittenEventArgs>();

                        await listener.RunWithCallbackAsync(ev => events.Enqueue(ev), async () =>
                        {
                            try
                            {
                                await Dns.GetHostEntryAsync(Configuration.Sockets.InvalidHost).ConfigureAwait(false);
                                throw new SkipTestException("GetHostEntryAsync should fail but it did not.");
                            }
                            catch (SocketException e) when (e.SocketErrorCode == SocketError.HostNotFound)
                            {
                                await WaitForErrorEventAsync(events);
                            }
                            catch (Exception e)
                            {
                                throw new SkipTestException($"GetHostEntryAsync failed unexpectedly: {e.Message}");
                            }
                        }).ConfigureAwait(false);

                        Assert.True(events.Count > 0, "events.Count should be > 0");
                        foreach (EventWrittenEventArgs ev in events)
                        {
                            Assert.True(ev.Payload.Count >= 3);
                            Assert.NotNull(ev.Payload[0]);
                            Assert.NotNull(ev.Payload[1]);
                            Assert.NotNull(ev.Payload[2]);
                        }
                    }
                }).Dispose();
            }
            catch (Exception ex) when (ex.ToString().Contains(nameof(SkipTestException), StringComparison.Ordinal))
            {
                throw new SkipTestException(ex.ToString());
            }

            static async Task WaitForErrorEventAsync(ConcurrentQueue<EventWrittenEventArgs> events)
            {
                DateTime startTime = DateTime.UtcNow;

                while (!events.Any(e => e.EventName == "ErrorMessage"))
                {
                    if (DateTime.UtcNow.Subtract(startTime) > TimeSpan.FromSeconds(30))
                        throw new TimeoutException("Timeout waiting for error event");

                    await Task.Delay(100);
                }
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void GetHostEntry_ValidName_NoErrors()
        {
            try
            {
                RemoteExecutor.Invoke(static () =>
                {
                    using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.NameResolution", EventLevel.Verbose))
                    {
                        var events = new ConcurrentQueue<EventWrittenEventArgs>();

                        listener.RunWithCallback(ev => events.Enqueue(ev), () =>
                        {
                            try
                            {
                                Dns.GetHostEntryAsync("localhost").GetAwaiter().GetResult();
                                Dns.GetHostEntryAsync(IPAddress.Loopback).GetAwaiter().GetResult();
                                Dns.GetHostEntry("localhost");
                                Dns.GetHostEntry(IPAddress.Loopback);
                            }
                            catch (Exception e)
                            {
                                throw new SkipTestException($"Localhost lookup failed unexpectedly: {e.Message}");
                            }
                        });

                        // We get some traces.
                        Assert.True(events.Count() > 0);
                        // No errors or warning for successful query.
                        Assert.True(events.Count(ev => (int)ev.Level > (int)EventLevel.Informational) == 0);
                    }
                }).Dispose();
            }
            catch (Exception ex) when (ex.ToString().Contains(nameof(SkipTestException), StringComparison.Ordinal))
            {
                throw new SkipTestException(ex.ToString());
            }
        }
    }
}
