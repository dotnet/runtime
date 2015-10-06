// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [Fact]
        [ActiveIssue(3635)] // disabling perf tests until we have appropriate infrastructure with which to run them
        public void SocketPerformance_SingleSocketClientAsync_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 5000;
            int bufferSize = 256;
            int socket_instances = 1;
            long expectedMilliseconds = 5000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        [ActiveIssue(3635)] // disabling perf tests until we have appropriate infrastructure with which to run them
        public void SocketPerformance_MultipleSocketClientAsync_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 10000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }
    }
}
