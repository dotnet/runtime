// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Tests
{
    public class LoggingTest
    {
        public readonly ITestOutputHelper _output;

        public LoggingTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Socket).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Sockets", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("e03c0352-f9c9-56ff-0ea7-b94ba8cabc6b"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [OuterLoop]
        [Fact]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Sockets", EventLevel.Verbose))
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

                        new NetworkStreamTest().CopyToAsync_AllDataCopied(4096).GetAwaiter().GetResult();
                        new NetworkStreamTest().Timeout_ValidData_Roundtrips().GetAwaiter().GetResult();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }
    }
}
