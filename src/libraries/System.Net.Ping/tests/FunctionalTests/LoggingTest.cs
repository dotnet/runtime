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
                Assert.Equal("System.Net.Ping.InternalDiagnostics", EventSource.GetName(esType));
                Assert.Equal(Guid.Parse("fcf5966e-222a-59ec-8e5a-4bb038152090"), EventSource.GetGuid(esType));
                Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
            }
        }
    }
}
