// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Negative tests for PipeOptions.CurrentUserOnly in Unix.
    /// </summary>
    public class NamedPipeTest_CurrentUserOnly_Unix
    {
        [Theory]
        [OuterLoop("Needs sudo access")]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        [InlineData(PipeOptions.None, PipeOptions.None)]
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.CurrentUserOnly)]
        public async Task Connection_UnderDifferentUsers_BehavesAsExpected(
            PipeOptions serverPipeOptions, PipeOptions clientPipeOptions)
        {
            // Use an absolute path, otherwise, the test can fail if the remote invoker and test runner have
            // different working and/or temp directories. 
            string pipeName = "/tmp/" + Path.GetRandomFileName(); 
            using (var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, serverPipeOptions | PipeOptions.Asynchronous))
            {
                Task serverTask = server.WaitForConnectionAsync(CancellationToken.None);

                using (RemoteExecutor.Invoke(
                    new Func<string, string, int>(ConnectClientFromRemoteInvoker),
                    pipeName,
                    clientPipeOptions == PipeOptions.CurrentUserOnly ? "true" : "false",
                    new RemoteInvokeOptions { RunAsSudo = true }))
                {
                }

                if (serverPipeOptions == PipeOptions.CurrentUserOnly)
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => serverTask);
                else
                    await serverTask;
            }
        }

        private static int ConnectClientFromRemoteInvoker(string pipeName, string isCurrentUserOnly)
        {
            PipeOptions pipeOptions = bool.Parse(isCurrentUserOnly) ? PipeOptions.CurrentUserOnly : PipeOptions.None;
            using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, pipeOptions))
            {
                if (pipeOptions == PipeOptions.CurrentUserOnly)
                    Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                else
                    client.Connect();
            }

            return RemoteExecutor.SuccessExitCode;
        }
    }
}
