// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.Extensions.Hosting.IntegrationTesting;
using Microsoft.Extensions.Logging.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Hosting.FunctionalTests
{
    public class ShutdownTests : LoggedTest
    {
        private static readonly string StartedMessage = "Started";
        private static readonly string CompletionMessage = "Stopping firing\n" +
                                                            "Stopping end\n" +
                                                            "Stopped firing\n" +
                                                            "Stopped end";

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
        public async Task ShutdownTestRun()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestRun), "Run");
        }

        [ConditionalFact]
        [OSSkipCondition(OperatingSystems.Windows | OperatingSystems.MacOSX)]
        public async Task ShutdownTestWaitForShutdown()
        {
            await ExecuteShutdownTest(nameof(ShutdownTestWaitForShutdown), "WaitForShutdown");
        }

        private async Task ExecuteShutdownTest(string testName, string shutdownMechanic)
        {
            using (StartLog(out var loggerFactory))
            {
                var logger = loggerFactory.CreateLogger(testName);

                // TODO refactor deployers to not depend on source code
                // see https://github.com/aspnet/Extensions/issues/1697 and https://github.com/aspnet/AspNetCore/issues/10268
#pragma warning disable 0618
                var applicationPath = Path.Combine(TestPathUtilities.GetSolutionRootDirectory("Extensions"),
                    "src", "Hosting", "test", "testassets", "Microsoft.Extensions.Hosting.TestApp");
#pragma warning restore 0618

                var deploymentParameters = new DeploymentParameters(
                    applicationPath,
                    RuntimeFlavor.CoreClr,
                    RuntimeArchitecture.x64)
                {
                    TargetFramework = Tfm.NetCoreApp30,
                    ApplicationType = ApplicationType.Portable,
                    PublishApplicationBeforeDeployment = true,
                    StatusMessagesEnabled = false
                };

                deploymentParameters.EnvironmentVariables["DOTNET_STARTMECHANIC"] = shutdownMechanic;

                using (var deployer = new SelfHostDeployer(deploymentParameters, loggerFactory))
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
