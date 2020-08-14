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
            Type esType = typeof(NetworkChange).Assembly.GetType("System.Net.NetEventSource", throwOnError: false, ignoreCase: false);
            if (esType != null)
            {
                Assert.Equal("System.Net.NetworkInformation.InternalDiagnostics", EventSource.GetName(esType));
                Assert.Equal(Guid.Parse("7582bbd8-72f0-59b1-1b87-3a2a334eb61e"), EventSource.GetGuid(esType));

                Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
            }
        }
    }
}
