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

        public static bool IsRemoteExecutorSupportedAndPrivilegedProcess => RemoteExecutor.IsSupported && PlatformDetection.IsPrivilegedProcess;

        [ConditionalFact(typeof(NamedPipeTest_RunAsClient), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/0")]
        public void RunAsClient_Unix()
        {
            string pipeName = Path.GetRandomFileName();
            uint pairID = (uint)(Math.Abs(new Random(5125123).Next()));
            RemoteExecutor.Invoke(new Action<string, string>(ServerConnectAsId), pipeName, pairID.ToString()).Dispose();
        }

        [ConditionalTheory(typeof(NamedPipeTest_RunAsClient), nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void RunAsClient_Nested_RestoresOuterIdentity(bool innerUsesServerIdentity, bool throwFromInner)
        {
            RemoteExecutor.Invoke(static async (innerUsesServerIdentityString, throwFromInnerString) =>
            {
                const uint ClientUserId = 65534;
                uint serverUserId = geteuid();
                Assert.NotEqual(serverUserId, ClientUserId);
                uint innerUserId = bool.Parse(innerUsesServerIdentityString) ? serverUserId : ClientUserId;
                bool throwFromInner = bool.Parse(throwFromInnerString);

                string outerPipeName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                string innerPipeName = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                using var outer = new NamedPipeServerStream(outerPipeName, PipeDirection.Out);
                using var inner = new NamedPipeServerStream(innerPipeName, PipeDirection.Out);

                // Allow the unprivileged clients to connect to sockets created by the server user.
                const UnixFileMode SocketMode = UnixFileMode.UserRead | UnixFileMode.UserWrite |
                    UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;
                File.SetUnixFileMode(outerPipeName, SocketMode);
                File.SetUnixFileMode(innerPipeName, SocketMode);
                using (RemoteExecutor.Invoke(new Action<string, string>(ClientConnectAsID), outerPipeName, ClientUserId.ToString()))
                using (RemoteExecutor.Invoke(new Action<string, string>(ClientConnectAsID), innerPipeName, innerUserId.ToString()))
                {
                    await outer.WaitForConnectionAsync().WaitAsync(TimeSpan.FromSeconds(30));
                    await inner.WaitForConnectionAsync().WaitAsync(TimeSpan.FromSeconds(30));
                }

                bool innerRan = false;
                outer.RunAsClient(() =>
                {
                    Assert.Equal(ClientUserId, geteuid());
                    PipeStreamImpersonationWorker innerWorker = () =>
                    {
                        innerRan = true;
                        Assert.Equal(innerUserId, geteuid());
                        if (throwFromInner)
                        {
                            throw new InvalidOperationException();
                        }
                    };

                    if (throwFromInner)
                    {
                        Assert.Throws<InvalidOperationException>(() => inner.RunAsClient(innerWorker));
                    }
                    else
                    {
                        inner.RunAsClient(innerWorker);
                    }

                    Assert.Equal(ClientUserId, geteuid());
                });

                Assert.True(innerRan);
                Assert.Equal(serverUserId, geteuid());
            }, innerUsesServerIdentity.ToString(), throwFromInner.ToString()).Dispose();
        }

        private static void ServerConnectAsId(string pipeName, string pairIDString)
        {
            uint pairID = uint.Parse(pairIDString);
            Assert.NotEqual(-1, seteuid(pairID));
            using (var outbound = new NamedPipeServerStream(pipeName, PipeDirection.Out))
            using (var handle = RemoteExecutor.Invoke(new Action<string, string>(ClientConnectAsID), pipeName, pairIDString))
            {
                // Connect as the unprivileged user, but RunAsClient as the superuser
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
