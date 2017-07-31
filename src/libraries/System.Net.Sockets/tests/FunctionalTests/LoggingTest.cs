// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Sockets.Tests
{
    public class LoggingTest : RemoteExecutorTestBase
    {
        [Fact]
        [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
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
        [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteInvoke(() =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Sockets", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke several tests to execute code paths while tracing is enabled

                        new SendReceiveSync().SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveSync().SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveTask().SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveTask().SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveEap().SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveEap().SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new SendReceiveApm().SendRecv_Stream_TCP(IPAddress.Loopback, false).GetAwaiter();
                        new SendReceiveApm().SendRecv_Stream_TCP(IPAddress.Loopback, true).GetAwaiter();

                        new NetworkStreamTest().CopyToAsync_AllDataCopied(4096).GetAwaiter().GetResult();
                        new NetworkStreamTest().Timeout_ValidData_Roundtrips().GetAwaiter().GetResult();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
                return SuccessExitCode;
            }).Dispose();
        }
    }
}
