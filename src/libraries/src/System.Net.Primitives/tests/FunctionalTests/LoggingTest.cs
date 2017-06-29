// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class LoggingTest : RemoteExecutorTestBase
    {
        [Fact]
        [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(IPAddress).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Primitives", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("a9f9e4e1-0cf5-5005-b530-3d37959d5e84"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }

        [Fact]
        [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
        public void EventSource_EventsRaisedAsExpected()
        {
            RemoteInvoke(() =>
            {
                using (var listener = new TestEventListener("Microsoft-System-Net-Primitives", EventLevel.Verbose))
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
                return SuccessExitCode;
            }).Dispose();
        }
    }
}
