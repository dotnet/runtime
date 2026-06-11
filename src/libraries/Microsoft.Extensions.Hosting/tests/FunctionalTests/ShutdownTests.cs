// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting.IntegrationTesting;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Test;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class ShutdownTests
    {
        private static readonly string StartedMessage = "Started";
        private static readonly string CompletionMessage = "Stopping firing\n" +
                                                            "Stopping end\n" +
                                                            "Stopped firing\n" +
                                                            "Stopped end";
        private static readonly TimeSpan s_shutdownExitTimeout = TimeSpan.FromSeconds(30);
        private readonly ITestOutputHelper _output;

        public ShutdownTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task ShutdownTestRun()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestRun), "Run");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task ShutdownTestWaitForShutdown()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestWaitForShutdown), "WaitForShutdown");
        }

        private async Task ExecuteShutdownTest(string testName, string shutdownMechanic)
        {
            var xunitTestLoggerFactory = TestLoggerBuilder.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Trace);
                builder.AddXunit(_output);
            });

            string applicationPath = AppContext.BaseDirectory;

            Version version = Environment.Version;
            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                ApplicationName = "Microsoft.Extensions.Hosting.TestApp",
                TargetFramework = $"net{version.Major}.{version.Minor}",
                ApplicationType = ApplicationType.Portable,
                PublishApplicationBeforeDeployment = true,
                StatusMessagesEnabled = false
            };
            deploymentParameters.ApplicationPublisher = new ExistingOutputApplicationPublisher(applicationPath);

            deploymentParameters.EnvironmentVariables["DOTNET_STARTMECHANIC"] = shutdownMechanic;

            using (var deployer = new SelfHostDeployer(deploymentParameters, xunitTestLoggerFactory))
            {
                var started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var output = string.Empty;
                deployer.OutputReceived += (sender, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data) && args.Data.StartsWith(StartedMessage))
                    {
                        output += args.Data.Substring(StartedMessage.Length) + '\n';
                        started.TrySetResult(0);
                    }
                    else
                    {
                        output += args.Data + '\n';
                    }

                    if (output.Contains(CompletionMessage))
                    {
                        completed.TrySetResult(0);
                    }
                };

                await deployer.DeployAsync();

                await started.Task.WaitAsync(TimeSpan.FromSeconds(180));

                SendShutdownSignal(deployer.HostProcess);

                await completed.Task.WaitAsync(TimeSpan.FromSeconds(180));

                WaitForExitOrKill(deployer.HostProcess);

                output = output.Trim('\n');

                Assert.Equal(CompletionMessage, output);
            }
        }

        private void SendShutdownSignal(Process hostProcess)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                SendSIGINT(hostProcess.Id);
            }
            /*
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                SendCtlC(hostProcess);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
            }
            */
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        private static void SendSIGINT(int processId)
        {
            if (kill(processId, ProcessExtensions.SigIntSignalNumber) != 0)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        private static void WaitForExitOrKill(Process process)
        {
            process.WaitForExit((int)s_shutdownExitTimeout.TotalMilliseconds);
            if (!process.HasExited)
            {
                process.Kill();
                // Wait for the process to actually exit after Kill() before accessing ExitCode
                if (!process.WaitForExit(5000))
                {
                    throw new InvalidOperationException($"Process {process.Id} did not exit within timeout after Kill()");
                }
            }

            Assert.Equal(0, process.ExitCode);
        }

        private sealed class ExistingOutputApplicationPublisher : ApplicationPublisher
        {
            public ExistingOutputApplicationPublisher(string applicationPath)
                : base(applicationPath)
            {
            }

            public override Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
                => Task.FromResult<PublishedApplication>(new BorrowedPublishedApplication(ApplicationPath, logger));

            // Wraps a path that is borrowed (not owned) from the test output directory.
            // Dispose is intentionally a no-op to prevent deleting AppContext.BaseDirectory.
            private sealed class BorrowedPublishedApplication : PublishedApplication
            {
                public BorrowedPublishedApplication(string path, ILogger logger) : base(path, logger) { }

                public override void Dispose() { }
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);
    }
}
