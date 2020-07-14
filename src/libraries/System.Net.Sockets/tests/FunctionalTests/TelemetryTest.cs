// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class TelemetryTest
    {
        public readonly ITestOutputHelper _output;

        public TelemetryTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Socket).Assembly.GetType("System.Net.Sockets.SocketsTelemetry", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.Sockets", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("d5b2e7d4-b6ec-50ae-7cde-af89427ad21f"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("System.Net.Sockets", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke several tests to execute code paths while tracing is enabled

                        new SendReceiveSync(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveSync(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveTask(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveTask(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveEap(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveEap(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveApm(null).SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveApm(null).SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new NetworkStreamTest().CopyToAsync_AllDataCopied(4096, true).GetAwaiter().GetResult();
                        new NetworkStreamTest().Timeout_ValidData_Roundtrips().GetAwaiter().GetResult();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }).Dispose();
        }
    }
}
