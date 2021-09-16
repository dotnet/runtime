// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
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

            Assert.Equal("Private.InternalDiagnostics.System.Net.Security", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("a0d627f0-c0f5-5a45-558a-6634a894c155"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            if (PlatformDetection.IsWindows10Version22000OrGreater)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/58927")]
                throw new SkipTestException("Unstable on Windows 11");
            }

            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Security", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke tests that'll cause some events to be generated
                        var test = new SslStreamStreamToStreamTest_Async();
                        test.SslStream_StreamToStream_Authentication_Success().GetAwaiter().GetResult();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }).Dispose();
        }
    }
}
