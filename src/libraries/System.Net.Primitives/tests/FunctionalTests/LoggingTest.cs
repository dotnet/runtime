// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class LoggingTest
    {
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50571", TestPlatforms.Android)]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(IPAddress).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.Primitives", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("534f3517-0a04-520f-9d69-4778dd119fe1"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteExecutor.Invoke(() =>
            {
                using (var listener = new TestEventListener("Private.InternalDiagnostics.System.Net.Primitives", EventLevel.Verbose))
                {
                    var events = new ConcurrentQueue<EventWrittenEventArgs>();
                    listener.RunWithCallback(events.Enqueue, () =>
                    {
                        // Invoke a test that'll cause some events to be generated
                        CredentialCacheTest.Add_HostPortAuthenticationTypeCredential_Success();
                    });
                    Assert.DoesNotContain(events, ev => ev.EventId == 0); // errors from the EventSource itself
                    Assert.InRange(events.Count, 1, int.MaxValue);
                }
            }).Dispose();
        }
    }
}
