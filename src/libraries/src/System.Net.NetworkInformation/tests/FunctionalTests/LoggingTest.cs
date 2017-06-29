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
            Type esType = typeof(NetworkChange).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-NetworkInformation", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("b8e42167-0eb2-5e39-97b5-acaca593d3a2"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
