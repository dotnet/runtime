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
        private const int DummyOSXPerfIssue = 123456;

        private const int TestPortBase = 9300;
        private readonly ITestOutputHelper _log;

        public SocketPerformanceAPMTests(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();
        }

        [Fact]
        public void SocketPerformance_SingleSocketClientAPM_LocalHostServerAPM()
        {
            int port = TestPortBase + 0;
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 5000;
            int bufferSize = 256;
            int socket_instances = 1;
            long expectedMilliseconds = 5000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        [ActiveIssue(DummyOSXPerfIssue, PlatformID.OSX)]
        public void SocketPerformance_MultipleSocketClientAPM_LocalHostServerAPM()
        {
            int port = TestPortBase + 1;
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 50000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        public void SocketPerformance_SingleSocketClientAPM_LocalHostServerAsync()
        {
            int port = TestPortBase + 2;
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 5000;
            int bufferSize = 256;
            int socket_instances = 1;
            long expectedMilliseconds = 5000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        [ActiveIssue(DummyOSXPerfIssue, PlatformID.OSX)]
        public void SocketPerformance_MultipleSocketClientAPM_LocalHostServerAsync()
        {
            int port = TestPortBase + 3;
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.APM;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 30000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        public void SocketPerformance_SingleSocketClientAsync_LocalHostServerAPM()
        {
            int port = TestPortBase + 4;
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 5000;
            int bufferSize = 256;
            int socket_instances = 1;
            long expectedMilliseconds = 5000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }

        [Fact]
        [ActiveIssue(DummyOSXPerfIssue, PlatformID.OSX)]
        public void SocketPerformance_MultipleSocketClientAsync_LocalHostServerAPM()
        {
            int port = TestPortBase + 5;
            SocketImplementationType serverType = SocketImplementationType.APM;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 1000;
            int bufferSize = 256;
            int socket_instances = 100;
            long expectedMilliseconds = 30000;

            var test = new SocketPerformanceTests(_log);

            test.ClientServerTest(
                port,
                serverType,
                clientType,
                iterations,
                bufferSize,
                socket_instances,
                expectedMilliseconds);
        }
    }
}
