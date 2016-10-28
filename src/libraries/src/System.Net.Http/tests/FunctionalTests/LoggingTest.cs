// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Http.Tests
{
    public static class LoggingTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(HttpClient).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);
            Assert.Equal("Microsoft-System-Net-Http", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("bdd9a83e-1929-5482-0d73-2fe5e1c0e16d"), EventSource.GetGuid(esType));
        }
    }
}
