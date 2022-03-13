// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Mail.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "SmtpClient is not supported on Browser")]
    public class LoggingTest
    {
        [Fact]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(SmtpClient).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.Mail", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("6fff04ac-5aab-530c-03b3-b1b32d2538e9"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Mail", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke a test that'll cause some events to be generated
                        new SmtpClientTest().TestMailDelivery();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }).Dispose();
        }
    }
}
