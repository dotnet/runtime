// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
        private readonly int _iterations = 1;

        public SocketPerformanceAsyncTests(ITestOutputHelper output)
        {
            _log = TestLogging.GetInstance();

            string env = Environment.GetEnvironmentVariable("SOCKETSTRESS_ITERATIONS");
            if (env != null)
            {
                _iterations = int.Parse(env);
            }
        }

        [ActiveIssue(13349, TestPlatforms.OSX)]
        [OuterLoop]
        [Fact]
        public void SocketPerformance_MultipleSocketClientAsync_LocalHostServerAsync()
        {
            SocketImplementationType serverType = SocketImplementationType.Async;
            SocketImplementationType clientType = SocketImplementationType.Async;
            int iterations = 200 * _iterations;
            int bufferSize = 256;
            int socket_instances = 20;

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
