// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.Tracing;
using Xunit;

namespace System.Net.Tests
{
    public class LoggingTest
    {
        [Fact]
        [SkipOnCoreClr("System.Net.Tests are flaky", RuntimeConfiguration.Checked)]
        public void EventSource_ExistsWithCorrectId()
        {
            Type esType = typeof(WebRequest).Assembly.GetType("System.Net.NetEventSource", throwOnError: true, ignoreCase: false);
            Assert.NotNull(esType);

            Assert.Equal("System.Net.Requests.InternalDiagnostics", EventSource.GetName(esType));
            Assert.Equal(Guid.Parse("51eafc09-2b9b-5c9b-16dd-98cbab55a00d"), EventSource.GetGuid(esType));

            Assert.NotEmpty(EventSource.GenerateManifest(esType, esType.Assembly.Location));
        }
    }
}
