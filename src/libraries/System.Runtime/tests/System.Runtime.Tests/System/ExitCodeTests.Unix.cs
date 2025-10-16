// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.AnyUnix)] // SIGTERM signal.
        public void SigTermExitCode()
        {
            Action action = () =>
            {
                AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                {
                    Assert.Fail("AppDomain.ProcessExit is not expected to be called when the process is killed by SIGTERM");
                };

                Console.WriteLine("Application started");

                // Wait for SIGTERM
                System.Threading.Thread.Sleep(int.MaxValue);
            };

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            options.StartInfo.RedirectStandardOutput = true;
            options.CheckExitCode = false;
            using (RemoteInvokeHandle remoteExecution = RemoteExecutor.Invoke(action, options))
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

                // Check that the exit code is 143 (128 + SIGTERM).
                Assert.Equal(143, process.ExitCode);
            }
        }
    }
}
