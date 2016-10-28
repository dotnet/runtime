// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public static class LoggingTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(IPAddress).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);
            Assert.Equal("Microsoft-System-Net-Primitives", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("a9f9e4e1-0cf5-5005-b530-3d37959d5e84"), EventSource.GetGuid(esType));
        }
    }
}
