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
    public partial class NamedPipeTest_RunAsClient : RemoteExecutorTestBase
    {
        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]  // Uses P/Invokes
        public async Task RunAsClient_Windows()
        {
            string pipeName = Path.GetRandomFileName();
            using (var server = new NamedPipeServerStream(pipeName))
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.None, TokenImpersonationLevel.Impersonation))
            {
                Task serverTask = server.WaitForConnectionAsync();

                client.Connect();
                await serverTask;

                bool ran = false;
                server.RunAsClient(() => ran = true);
                Assert.True(ran, "Expected delegate to have been invoked");
            }
        }
    }
}
