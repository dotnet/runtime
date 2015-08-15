// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace InterProcessCommunication.Tests
{
    public class SemaphoreTests : RemoteExecutorTestBase
    {
        [PlatformSpecific(PlatformID.Windows)] // names aren't supported on Unix
        [Fact]
        public void PingPong()
        {
            // Create names for the two semaphores
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            // Create the two semaphores and the other process with which to synchronize
            using (var inbound = new Semaphore(1, 1, inboundName))
            using (var outbound = new Semaphore(0, 1, outboundName))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, outboundName, inboundName))
            {
                // Repeatedly wait for count in one semaphore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }
        }

        private static int PingPong_OtherProcess(string inboundName, string outboundName)
        {
            // Open the two semaphores
            using (var inbound = Semaphore.OpenExisting(inboundName))
            using (var outbound = Semaphore.OpenExisting(outboundName))
            {
                // Repeatedly wait for count in one sempahore and then release count into the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    outbound.Release();
                }
            }

            return SuccessExitCode;
        }

    }
}
