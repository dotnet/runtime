// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Tests
{
    public class LoggingTest
    {
        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.Mono, "NetEventSource is only part of .NET Core.")]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(WebRequest).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Microsoft-System-Net-Requests", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("3763dc7e-7046-5576-9041-5616e21cc2cf"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
