// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Security.Tests
{
    public static class LoggingTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(SslStream).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);
            Assert.Equal("Microsoft-System-Net-Security", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("066c0e27-a02d-5a98-9a4d-078cc3b1a896"), EventSource.GetGuid(esType));
        }
    }
}
