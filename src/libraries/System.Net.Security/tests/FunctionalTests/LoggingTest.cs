// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
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
        [SkipOnPlatform(TestPlatforms.iOS | TestPlatforms.tvOS, "X509 certificate store is not supported on iOS or tvOS.")] // Match SslStream_StreamToStream_Authentication_Success
        public async Task EventSource_EventsRaisedAsExpected()
        {
            await RemoteExecutor.Invoke(async () =>
            {
                try
                {
                    using var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Security", EventLevel.Verbose);
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    await listener.RunWithCallbackAsync(events.Enqueue, async () =>
                    {
                        // Invoke tests that'll cause some events to be generated
                        var test = new SslStreamStreamToStreamTest_Async();
                        await test.SslStream_StreamToStream_Authentication_Success();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
                catch (SkipTestException)
                {
                    // Don't throw inside RemoteExecutor if SslStream_StreamToStream_Authentication_Success chose to skip the test
                }
            }).DisposeAsync();
        }
    }
}
