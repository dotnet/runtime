// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets.Tests;
using System.Net.Test.Common;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.Sockets.Performance.Tests
{
    [Trait("Perf", "true")]
    public class SocketPerformanceAPMTests
    {
        // NOTE: when enabled these tests will probably still fail on OS X due to #4009.

        private readonly ITestOutputHelper _log;

        public SocketPerformanceAPMTests(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        [ActiveIssue(3635)] // disabling perf tests until we have appropriate infrastructure with which to run them
        public void SocketPerformance_SingleSocketClientAPM_LocalHostServerAPM()
        {
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.APM;
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
        public void SocketPerformance_MultipleSocketClientAPM_LocalHostServerAPM()
        {
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 50000;

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
        public void SocketPerformance_SingleSocketClientAPM_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.APM;
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
        public void SocketPerformance_MultipleSocketClientAPM_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 30000;

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
        public void SocketPerformance_SingleSocketClientAsync_LocalHostServerAPM()
        {
            SocketImplementationType serverType = SocketImplementationType.APM;
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
        public void SocketPerformance_MultipleSocketClientAsync_LocalHostServerAPM()
        {
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 30000;

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
