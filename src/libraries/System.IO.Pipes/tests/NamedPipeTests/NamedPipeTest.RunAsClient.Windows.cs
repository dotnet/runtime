// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        [ActiveIssue(22271, TargetFrameworkMonikers.UapNotUapAot)]
        public async Task RunAsClient_Windows(TokenImpersonationLevel tokenImpersonationLevel)
        {
            string pipeName = GetUniquePipeName();
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
