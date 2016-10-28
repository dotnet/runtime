// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Mail.Tests
{
    public static class LoggingTest
    {
        [Fact]
        public static void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(SmtpClient).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);
            Assert.Equal("Microsoft-System-Net-Mail", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("42c8027b-f048-58d2-537d-a4a9d5ee7038"), EventSource.GetGuid(esType));
        }
    }
}
