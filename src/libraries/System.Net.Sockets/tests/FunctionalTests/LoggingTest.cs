// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
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
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50568", TestPlatforms.Android)]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Socket).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.Sockets", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("ae391de7-a2cb-557c-dd34-fe00d0b98c7f"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(async () =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Sockets", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Invoke several tests to execute code paths while tracing is enabled

                        await new SendReceive_Sync(null).SendRecv_Stream_TCP(IPAddress.Loopback, false);
                        await new SendReceive_Sync(null).SendRecv_Stream_TCP(IPAddress.Loopback, true);

                        await new SendReceive_Task(null).SendRecv_Stream_TCP(IPAddress.Loopback, false);
                        await new SendReceive_Task(null).SendRecv_Stream_TCP(IPAddress.Loopback, true);

                        await new SendReceive_Eap(null).SendRecv_Stream_TCP(IPAddress.Loopback, false);
                        await new SendReceive_Eap(null).SendRecv_Stream_TCP(IPAddress.Loopback, true);

                        await new SendReceive_Apm(null).SendRecv_Stream_TCP(IPAddress.Loopback, false);
                        await new SendReceive_Apm(null).SendRecv_Stream_TCP(IPAddress.Loopback, true);

                        await new NetworkStreamTest().CopyToAsync_AllDataCopied(4096, true);
                        await new NetworkStreamTest().Timeout_Roundtrips();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }).Dispose();
        }
    }
}
