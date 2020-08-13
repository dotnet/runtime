// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting.IntegrationTesting;
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
        private readonly ITestOutputHelper _output;

        public ShutdownTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34090")]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task ShutdownTestRun()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestRun), "Run");
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/34090")]
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

            // TODO refactor deployers to not depend on source code
            // see https://github.com/dotnet/extensions/issues/1697 and https://github.com/dotnet/aspnetcore/issues/10268
#pragma warning disable 0618
            var applicationPath = string.Empty; // disabled for now
#pragma warning restore 0618

            var deploymentParameters = new DeploymentParameters(
                applicationPath,
                RuntimeFlavor.CoreClr,
                RuntimeArchitecture.x64)
            {
                TargetFramework = Tfm.NetCoreApp50,
                ApplicationType = ApplicationType.Portable,
                PublishApplicationBeforeDeployment = true,
                StatusMessagesEnabled = false
            };

            deploymentParameters.EnvironmentVariables["DOTNET_STARTMECHANIC"] = shutdownMechanic;

            using (var deployer = new SelfHostDeployer(deploymentParameters, xunitTestLoggerFactory))
            {
                var result = await deployer.DeployAsync();

                var started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                var output = string.Empty;
                deployer.HostProcess.OutputDataReceived += (sender, args) =>
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

                await started.Task.TimeoutAfter(TimeSpan.FromSeconds(60));

                SendShutdownSignal(deployer.HostProcess);

                await completed.Task.TimeoutAfter(TimeSpan.FromSeconds(60));

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
            var startInfo = new ProcessStartInfo
            {
                FileName = "kill",
                Arguments = processId.ToString(),
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);
            WaitForExitOrKill(process);
        }

        private static void WaitForExitOrKill(Process process)
        {
            process.WaitForExit(1000);
            if (!process.HasExited)
            {
                process.Kill();
            }

            Assert.Equal(0, process.ExitCode);
        }
    }
}
