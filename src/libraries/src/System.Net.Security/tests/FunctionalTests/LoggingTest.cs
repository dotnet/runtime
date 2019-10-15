// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Security.Tests
{
    public class LoggingTest
    {
        [Fact]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(SslStream).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Security", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("066c0e27-a02d-5a98-9a4d-078cc3b1a896"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [Fact]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Security", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke tests that'll cause some events to be generated
                        var test = new SslStreamStreamToStreamTest_Async();
                        test.SslStream_StreamToStream_Authentication_Success().GetAwaiter().GetResult();
                        test.SslStream_StreamToStream_Successive_ClientWrite_Success().GetAwaiter().GetResult();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
                return RemoteExecutor.SuccessExitCode;
            }).Dispose();
        }
    }
}
