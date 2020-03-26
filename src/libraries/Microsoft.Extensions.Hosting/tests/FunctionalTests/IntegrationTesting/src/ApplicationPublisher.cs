// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    public class ApplicationPublisher
    {
        public string ApplicationPath { get; }

        public ApplicationPublisher(string applicationPath)
        {
            ApplicationPath = applicationPath;
        }

        public static readonly string DotnetCommandName = "dotnet";

        public virtual Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var publishDirectory = CreateTempDirectory();
            using (logger.BeginScope("dotnet-publish"))
            {
                if (string.IsNullOrEmpty(deploymentParameters.TargetFramework))
                {
                    throw new Exception($"A target framework must be specified in the deployment parameters for applications that require publishing before deployment");
                }

                var parameters = $"publish "
                                 + $" --output \"{publishDirectory.FullName}\""
                                 + $" --framework {deploymentParameters.TargetFramework}"
                                 + $" --configuration {deploymentParameters.Configuration}"
                                 // avoids triggering builds of dependencies of the test app which could cause issues like https://github.com/dotnet/arcade/issues/2941
                                 + $" --no-dependencies"
                                 + $" /p:TargetArchitecture={deploymentParameters.RuntimeArchitecture}"
                                 + " --no-restore";

                if (deploymentParameters.ApplicationType == ApplicationType.Standalone)
                {
                    parameters += $" --runtime {GetRuntimeIdentifier(deploymentParameters)}";
                }
                else
                {
                    // Workaround for https://github.com/aspnet/websdk/issues/422
                    parameters += " -p:UseAppHost=false";
                }

                parameters += $" {deploymentParameters.AdditionalPublishParameters}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = DotnetCommandName,
                    Arguments = parameters,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = deploymentParameters.ApplicationPath,
                };

                ProcessHelpers.AddEnvironmentVariablesToProcess(startInfo, deploymentParameters.PublishEnvironmentVariables, logger);

                var hostProcess = new Process() { StartInfo = startInfo };

                logger.LogInformation($"Executing command {DotnetCommandName} {parameters}");

                hostProcess.StartAndCaptureOutAndErrToLogger("dotnet-publish", logger);

                // A timeout is passed to Process.WaitForExit() for two reasons:
                //
                // 1. When process output is read asynchronously, WaitForExit() without a timeout blocks until child processes
                //    are killed, which can cause hangs due to MSBuild NodeReuse child processes started by dotnet.exe.
                //    With a timeout, WaitForExit() returns when the parent process is killed and ignores child processes.
                //    https://stackoverflow.com/a/37983587/102052
                //
                // 2. If "dotnet publish" does hang indefinitely for some reason, tests should fail fast with an error message.
                const int timeoutMinutes = 5;
                if (hostProcess.WaitForExit(milliseconds: timeoutMinutes * 60 * 1000))
                {
                    if (hostProcess.ExitCode != 0)
                    {
                        var message = $"{DotnetCommandName} publish exited with exit code : {hostProcess.ExitCode}";
                        logger.LogError(message);
                        throw new Exception(message);
                    }
                }
                else
                {
                    var message = $"{DotnetCommandName} publish failed to exit after {timeoutMinutes} minutes";
                    logger.LogError(message);
                    throw new Exception(message);
                }

                logger.LogInformation($"{DotnetCommandName} publish finished with exit code : {hostProcess.ExitCode}");
            }

            return Task.FromResult(new PublishedApplication(publishDirectory.FullName, logger));
        }

        private static string GetRuntimeIdentifier(DeploymentParameters deploymentParameters)
        {
            var architecture = deploymentParameters.RuntimeArchitecture;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win-" + architecture;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux-" + architecture;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-" + architecture;
            }
            throw new InvalidOperationException("Unrecognized operation system platform");
        }

        protected static DirectoryInfo CreateTempDirectory()
        {
            var tempPath = Path.GetTempPath() + Guid.NewGuid().ToString("N");
            var target = new DirectoryInfo(tempPath);
            target.Create();
            return target;
        }
    }
}
