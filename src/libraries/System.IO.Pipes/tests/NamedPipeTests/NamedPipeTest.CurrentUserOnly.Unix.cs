// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Abstractions;

namespace System.IO.Pipes.Tests
{
    /// <summary>
    /// Negative tests for PipeOptions.CurrentUserOnly in Unix.
    /// </summary>
    public class NamedPipeTest_CurrentUserOnly_Unix
    {
        private readonly ITestOutputHelper _output;

        public NamedPipeTest_CurrentUserOnly_Unix(ITestOutputHelper output)
        {
            _output = output;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [OuterLoop("Needs sudo access")]
        [InlineData(PipeOptions.None, PipeOptions.None, PipeDirection.In)]
        [InlineData(PipeOptions.None, PipeOptions.None, PipeDirection.InOut)]
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly, PipeDirection.In)]
        [InlineData(PipeOptions.None, PipeOptions.CurrentUserOnly, PipeDirection.InOut)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None, PipeDirection.In)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.None, PipeDirection.InOut)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.CurrentUserOnly, PipeDirection.In)]
        [InlineData(PipeOptions.CurrentUserOnly, PipeOptions.CurrentUserOnly, PipeDirection.InOut)]
        public async Task Connection_UnderDifferentUsers_BehavesAsExpected(
            PipeOptions serverPipeOptions, PipeOptions clientPipeOptions, PipeDirection clientPipeDirection)
        {
            bool isRoot = Environment.IsPrivilegedProcess;
            if (clientPipeOptions == PipeOptions.CurrentUserOnly && isRoot)
            {
                throw new SkipTestException("Current user is root, RemoteExecutor is unable to use a different user for CurrentUserOnly.");
            }

            // Use an absolute path, otherwise, the test can fail if the remote invoker and test runner have
            // different working and/or temp directories.
            string pipeName = "/tmp/" + Path.GetRandomFileName();

            _output.WriteLine("Starting as {0} on '{1}'", Environment.UserName, pipeName);

            using (var server = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, serverPipeOptions | PipeOptions.Asynchronous))
            {
                Task serverTask = server.WaitForConnectionAsync(CancellationToken.None);

                using (RemoteExecutor.Invoke(
                    new Action<string, string, string>(ConnectClientFromRemoteInvoker),
                    pipeName,
                    clientPipeOptions == PipeOptions.CurrentUserOnly ? "true" : "false",
                    clientPipeDirection == PipeDirection.In ? "true" : "false",
                    new RemoteInvokeOptions { RunAsSudo = true }))
                {
                }

                if (serverPipeOptions == PipeOptions.CurrentUserOnly && !isRoot)
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(() => serverTask);
                else
                    await serverTask;
            }
        }

        private static void ConnectClientFromRemoteInvoker(string pipeName, string isCurrentUserOnly, string isReadOnly)
        {
            PipeOptions pipeOptions = bool.Parse(isCurrentUserOnly) ? PipeOptions.CurrentUserOnly : PipeOptions.None;
            PipeDirection pipeDirection = bool.Parse(isReadOnly) ? PipeDirection.In : PipeDirection.InOut;

            using (var client = new NamedPipeClientStream(".", pipeName, pipeDirection, pipeOptions))
            {
                if (pipeOptions == PipeOptions.CurrentUserOnly)
                    Assert.Throws<UnauthorizedAccessException>(() => client.Connect());
                else
                    client.Connect();
            }
        }
    }
}
