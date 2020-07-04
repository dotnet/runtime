// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public class NamedPipeTest_RunAsClient
    {
        [DllImport("libc", SetLastError = true)]
        internal static extern unsafe int seteuid(uint euid);

        [DllImport("libc", SetLastError = true)]
        internal static extern unsafe uint geteuid();

        public static bool IsSuperUserAndRemoteExecutorSupported => geteuid() == 0 && RemoteExecutor.IsSupported;

        [ConditionalFact(nameof(IsSuperUserAndRemoteExecutorSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)]  // Uses P/Invokes
        [ActiveIssue("https://github.com/dotnet/runtime/issues/0")]
        public void RunAsClient_Unix()
        {
            string pipeName = Path.GetRandomFileName();
            uint pairID = (uint)(Math.Abs(new Random(5125123).Next()));
            RemoteExecutor.Invoke(new Action<string, string>(ServerConnectAsId), pipeName, pairID.ToString()).Dispose();
        }

        private static void ServerConnectAsId(string pipeName, string pairIDString)
        {
            uint pairID = uint.Parse(pairIDString);
            Assert.NotEqual(-1, seteuid(pairID));
            using (var outbound = new NamedPipeServerStream(pipeName, PipeDirection.Out))
            using (var handle = RemoteExecutor.Invoke(new Action<string, string>(ClientConnectAsID), pipeName, pairIDString))
            {
                // Connect as the unpriveleged user, but RunAsClient as the superuser
                outbound.WaitForConnection();
                Assert.NotEqual(-1, seteuid(0));

                bool ran = false;
                uint ranAs = 0;
                outbound.RunAsClient(() => {
                    ran = true;
                    ranAs = geteuid();
                });
                Assert.True(ran, "Expected delegate to have been invoked");
                Assert.Equal(pairID, ranAs);
            }
        }

        private static void ClientConnectAsID(string pipeName, string pairIDString)
        {
            uint pairID = uint.Parse(pairIDString);
            using (var inbound = new NamedPipeClientStream(".", pipeName, PipeDirection.In))
            {
                Assert.NotEqual(-1, seteuid(pairID));
                inbound.Connect();
            }
        }
    }
}
