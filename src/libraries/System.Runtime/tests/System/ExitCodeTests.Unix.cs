// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public class ExitCodeTests
    {
        private const int SIGTERM = 15;

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);

        [Theory]
        [InlineData(null)]
        [InlineData(0)]
        [InlineData(42)]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // SIGTERM signal.
        public void SigTermExitCode(int? exitCodeOnSigterm)
        {
            Action<string> action = (string sigTermExitCode) =>
            {
                if (!string.IsNullOrEmpty(sigTermExitCode))
                {
                    AppDomain.CurrentDomain.ProcessExit += delegate
                    {
                        Environment.ExitCode = int.Parse(sigTermExitCode);
                    };
                }

                Console.WriteLine("Application started");

                // Wait for SIGTERM
                System.Threading.Thread.Sleep(int.MaxValue);
            };

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.RedirectStandardOutput = true;
            using (RemoteInvokeHandle remoteExecution = RemoteExecutor.Invoke(action, exitCodeOnSigterm?.ToString() ?? string.Empty, options))
            {
                Process process = remoteExecution.Process;

                // Wait for the process to start and register the ProcessExit handler
                string processOutput = process.StandardOutput.ReadLine();
                Assert.Equal("Application started", processOutput);

                // Send SIGTERM
                int rv = kill(process.Id, SIGTERM);
                Assert.Equal(0, rv);

                // Process exits in a timely manner
                bool exited = process.WaitForExit(RemoteExecutor.FailWaitTimeoutMilliseconds);
                Assert.True(exited);

                // Check exit code
                Assert.Equal(exitCodeOnSigterm ?? 128 + SIGTERM, process.ExitCode);
            }
        }
    }
}
