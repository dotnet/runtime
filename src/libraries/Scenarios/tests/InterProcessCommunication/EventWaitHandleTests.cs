// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace InterProcessCommunication.Tests
{
    public class EventWaitHandleTests : RemoteExecutorTestBase
    {
        [PlatformSpecific(PlatformID.Windows)] // names aren't supported on Unix
        [Theory]
        [InlineData(EventResetMode.ManualReset)]
        [InlineData(EventResetMode.AutoReset)]
        public void PingPong(EventResetMode mode)
        {
            // Create names for the two events
            string outboundName = Guid.NewGuid().ToString("N");
            string inboundName = Guid.NewGuid().ToString("N");

            // Create the two events and the other process with which to synchronize
            using (var inbound = new EventWaitHandle(true, mode, inboundName))
            using (var outbound = new EventWaitHandle(false, mode, outboundName))
            using (var remote = RemoteInvoke(PingPong_OtherProcess, mode.ToString(), outboundName, inboundName))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }
        }

        private static int PingPong_OtherProcess(string modeName, string inboundName, string outboundName)
        {
            EventResetMode mode = (EventResetMode)Enum.Parse(typeof(EventResetMode), modeName);

            // Open the two events
            using (var inbound = EventWaitHandle.OpenExisting(inboundName))
            using (var outbound = EventWaitHandle.OpenExisting(outboundName))
            {
                // Repeatedly wait for one event and then set the other
                for (int i = 0; i < 10; i++)
                {
                    Assert.True(inbound.WaitOne(FailWaitTimeoutMilliseconds));
                    if (mode == EventResetMode.ManualReset)
                    {
                        inbound.Reset();
                    }
                    outbound.Set();
                }
            }

            return SuccessExitCode;
        }

    }
}
