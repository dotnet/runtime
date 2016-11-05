// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Sockets.Tests;
using System.Net.Test.Common;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    [Trait("Perf", "true")]
    public class SocketPerformanceAsyncTests
    {
        private readonly ITestOutputHelper _log;

        public SocketPerformanceAsyncTests(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        [ActiveIssue(13349, TestPlatforms.OSX)]
        [OuterLoop]
        [Fact]
        public void SocketPerformance_SingleSocketClientAsync_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 10000;
            int bufferSize = 256;
            int socket_instances = 1;

            var test = new SocketPerformanceTests(_log);

            // Run in Stress mode no expected time to complete.
            test.ClientServerTest(
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances);
        }

        [ActiveIssue(13349, TestPlatforms.OSX)]
        [OuterLoop]
        [Fact]
        public void SocketPerformance_MultipleSocketClientAsync_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 2000;
            int bufferSize = 256;
            int socket_instances = 500;

            var test = new SocketPerformanceTests(_log);

            // Run in Stress mode no expected time to complete.
            test.ClientServerTest(
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances);
        }
    }
}
