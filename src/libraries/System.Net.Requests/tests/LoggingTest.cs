// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Tests
{
    public class LoggingTest
    {
        [Fact]
        [SkipOnCoreClr("System.Net.Tests are flaky", ~RuntimeConfiguration.Release)]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(WebRequest).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("Private.InternalDiagnostics.System.Net.Requests", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("de972c9f-4457-5dc5-e37b-aaf8033eb3a9"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
