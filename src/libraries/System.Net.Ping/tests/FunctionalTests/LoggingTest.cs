// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class LoggingTest
    {
        [Fact]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Ping).Assembly.GetType("System.Net.NetEventSource", throwOnError: false, ignoreCase: false);
            if (esType != null)
            {
                Assert.Equal("Private.InternalDiagnostics.System.Net.Ping", EventSource.GetName(esType));
                Assert.Equal(Guid.Parse("37627c1f-34a9-539e-ab8c-84303b966b74"), EventSource.GetGuid(esType));
                Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
            }
        }
    }
}
