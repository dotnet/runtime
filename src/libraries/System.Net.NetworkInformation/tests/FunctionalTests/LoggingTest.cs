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
                Assert.Equal("Private.InternalDiagnostics.System.Net.NetworkInformation", EventSource.GetName(esType));
                Assert.Equal(Guid.Parse("e090a35b-1033-5de3-89e3-01cde7c158ce"), EventSource.GetGuid(esType));

                Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
            }
        }
    }
}
