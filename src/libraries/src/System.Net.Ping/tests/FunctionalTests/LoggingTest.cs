// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.NetworkInformation.Tests
{
    public class LoggingTest
    {
        [Fact]
        [ActiveIssue(20470, TargetFrameworkMonikers.UapAot)]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "NetEventSource is only part of .NET Core.")]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(Ping).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Ping", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("a771ec4a-7260-59ce-0475-db257437ed8c"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
