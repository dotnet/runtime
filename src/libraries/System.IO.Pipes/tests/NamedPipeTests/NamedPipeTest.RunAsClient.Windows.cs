// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Pipes.Tests
{
    public partial class NamedPipeTest_RunAsClient
    {
        [Theory]
        [InlineData(TokenImpersonationLevel.None)]
        [InlineData(TokenImpersonationLevel.Anonymous)]
        [InlineData(TokenImpersonationLevel.Identification)]
        [InlineData(TokenImpersonationLevel.Impersonation)]
        [InlineData(TokenImpersonationLevel.Delegation)]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public async Task RunAsClient_Windows(TokenImpersonationLevel tokenImpersonationLevel)
        {
            string pipeName = PipeStreamConformanceTests.GetUniquePipeName();
            using (var server = new NamedPipeServerStream(pipeName))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, tokenImpersonationLevel))
            {
                Task serverTask = server.WaitForConnectionAsync();

                client.Connect();
                await serverTask;

                bool ran = false;
                if (tokenImpersonationLevel == TokenImpersonationLevel.None)
                {
                    Assert.Throws<IOException>(() => server.RunAsClient(() => ran = true));
                    Assert.False(ran, "Expected delegate to not have been invoked");
                }
                else
                {
                    server.RunAsClient(() => ran = true);
                    Assert.True(ran, "Expected delegate to have been invoked");
                }
            }
        }

        [Theory]
        [InlineData(TokenImpersonationLevel.Identification, false)]
        [InlineData(TokenImpersonationLevel.Identification, true)]
        [InlineData(TokenImpersonationLevel.Impersonation, false)]
        [InlineData(TokenImpersonationLevel.Impersonation, true)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task RunAsClient_Nested_RestoresOuterIdentity(TokenImpersonationLevel innerImpersonationLevel, bool throwFromInner)
        {
            string outerPipeName = GetUniquePipeName();
            string innerPipeName = GetUniquePipeName();
            using var outerServer = new NamedPipeServerStream(outerPipeName);
            using var outerClient = new NamedPipeClientStream(".", outerPipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation);
            using var innerServer = new NamedPipeServerStream(innerPipeName);
            using var innerClient = new NamedPipeClientStream(".", innerPipeName, PipeDirection.InOut, PipeOptions.None, innerImpersonationLevel);

            Task outerConnection = outerServer.WaitForConnectionAsync();
            outerClient.Connect();
            await outerConnection;

            Task innerConnection = innerServer.WaitForConnectionAsync();
            innerClient.Connect();
            await innerConnection;

            TokenImpersonationLevel outerLevel = TokenImpersonationLevel.None;
            TokenImpersonationLevel innerLevel = TokenImpersonationLevel.None;
            TokenImpersonationLevel afterInnerLevel = TokenImpersonationLevel.None;
            bool innerRan = false;

            outerServer.RunAsClient(() =>
            {
                outerLevel = GetWindowsImpersonationLevel();

                PipeStreamImpersonationWorker innerWorker = () =>
                {
                    innerRan = true;
                    innerLevel = GetWindowsImpersonationLevel();
                    if (throwFromInner)
                    {
                        throw new InvalidOperationException();
                    }
                };

                if (throwFromInner)
                {
                    Assert.Throws<InvalidOperationException>(() => innerServer.RunAsClient(innerWorker));
                }
                else
                {
                    innerServer.RunAsClient(innerWorker);
                }

                afterInnerLevel = GetWindowsImpersonationLevel();
            });

            Assert.True(innerRan);
            Assert.Equal(TokenImpersonationLevel.Impersonation, outerLevel);
            Assert.Equal(innerImpersonationLevel, innerLevel);
            Assert.Equal(TokenImpersonationLevel.Impersonation, afterInnerLevel);
            Assert.Equal(TokenImpersonationLevel.None, GetWindowsImpersonationLevel());
        }

        private static TokenImpersonationLevel GetWindowsImpersonationLevel()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            return identity.ImpersonationLevel;
        }

        private static string GetUniquePipeName()
        {
            if (PlatformDetection.IsInAppContainer)
            {
                return @"LOCAL\" + Path.GetRandomFileName();
            }
            return Path.GetRandomFileName();
        }
    }
}
