// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using System.Reflection;
using Xunit;

namespace System.Net.WebSockets.Tests
{
    public class LoggingTest
    {
        [Fact]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(ClientWebSocket).GetTypeInfo().Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-WebSockets-Client", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("71cddde3-cf58-52d5-094f-927828a09337"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, "assemblyPathToIncludeInManifest"));
        }
    }
}
