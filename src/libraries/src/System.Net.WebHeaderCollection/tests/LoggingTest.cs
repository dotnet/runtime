// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Tests
{
    public class WebHeaderCollectionLoggingTest
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono, "NetEventSource is only part of .NET Core")]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(WebHeaderCollection).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-WebHeaderCollection", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("fd36452f-9f2b-5850-d212-6c436231e3dc"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
