// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    /// <summary>
    /// Deployer for WebListener and Kestrel.
    /// </summary>
    public class SelfHostDeployer : ApplicationDeployer
    {
        private const string ApplicationStartedMessage = "Application started. Press Ctrl+C to shut down.";

        public Process HostProcess { get; private set; }

        public SelfHostDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
            : base(deploymentParameters, loggerFactory)
        {
        }

        public override async Task<DeploymentResult> DeployAsync()
        {
            using (Logger.BeginScope("SelfHost.Deploy"))
            {
                // Start timer
                StartTimer();

                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr
                        && DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86)
                {
                    // Publish is required to rebuild for the right bitness
                    DeploymentParameters.PublishApplicationBeforeDeployment = true;
                }

                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr
                        && DeploymentParameters.ApplicationType == ApplicationType.Standalone)
                {
                    // Publish is required to get the correct files in the output directory
                    DeploymentParameters.PublishApplicationBeforeDeployment = true;
                }

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    DotnetPublish();
                }

                // Launch the host process.
                var hostExitToken = await StartSelfHostAsync();

                Logger.LogInformation("Application ready");

                return new DeploymentResult(
                    LoggerFactory,
                    DeploymentParameters,
                    contentRoot: DeploymentParameters.PublishApplicationBeforeDeployment ? DeploymentParameters.PublishedApplicationRootPath : DeploymentParameters.ApplicationPath,
                    hostShutdownToken: hostExitToken);
            }
        }

        protected async Task<CancellationToken> StartSelfHostAsync()
        {
            using (Logger.BeginScope("StartSelfHost"))
            {
                var executableName = string.Empty;
                var executableArgs = string.Empty;
                var workingDirectory = string.Empty;
                var executableExtension = DeploymentParameters.ApplicationType == ApplicationType.Portable ? ".dll"
                    : (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".exe" : "");

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    workingDirectory = DeploymentParameters.PublishedApplicationRootPath;
                }
                else
                {
                    // Core+Standalone always publishes. This must be Clr+Standalone or Core+Portable.
                    // Run from the pre-built bin/{config}/{tfm} directory.
                    var targetFramework = DeploymentParameters.TargetFramework
                        ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? Tfm.Net461 : Tfm.NetCoreApp22);
                    workingDirectory = Path.Combine(DeploymentParameters.ApplicationPath, "bin", DeploymentParameters.Configuration, targetFramework);
                    // CurrentDirectory will point to bin/{config}/{tfm}, but the config and static files aren't copied, point to the app base instead.
                    DeploymentParameters.EnvironmentVariables["DOTNET_CONTENTROOT"] = DeploymentParameters.ApplicationPath;
                }

                var executable = Path.Combine(workingDirectory, DeploymentParameters.ApplicationName + executableExtension);

                if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr && DeploymentParameters.ApplicationType == ApplicationType.Portable)
                {
                    executableName = GetDotNetExeForArchitecture();
                    executableArgs = executable;
                }
                else
                {
                    executableName = executable;
                }

                Logger.LogInformation($"Executing {executableName} {executableArgs}");

                var startInfo = new ProcessStartInfo
                {
                    FileName = executableName,
                    Arguments = executableArgs,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    // Trying a work around for https://github.com/aspnet/Hosting/issues/140.
                    RedirectStandardInput = true,
                    WorkingDirectory = workingDirectory
                };

                AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.EnvironmentVariables);

                var started = new TaskCompletionSource<object>();

                HostProcess = new Process() { StartInfo = startInfo };
                HostProcess.EnableRaisingEvents = true;
                HostProcess.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (string.Equals(dataArgs.Data, ApplicationStartedMessage))
                    {
                        started.TrySetResult(null);
                    }
                };
                var hostExitTokenSource = new CancellationTokenSource();
                HostProcess.Exited += (sender, e) =>
                {
                    Logger.LogInformation("host process ID {pid} shut down", HostProcess.Id);

                    // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                    started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {HostProcess.ExitCode}"));

                    TriggerHostShutdown(hostExitTokenSource);
                };

                try
                {
                    HostProcess.StartAndCaptureOutAndErrToLogger(executableName, Logger);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Error occurred while starting the process. Exception: {exception}", ex.ToString());
                }

                if (HostProcess.HasExited)
                {
                    Logger.LogError("Host process {processName} {pid} exited with code {exitCode} or failed to start.", startInfo.FileName, HostProcess.Id, HostProcess.ExitCode);
                    throw new Exception("Failed to start host");
                }

                Logger.LogInformation("Started {fileName}. Process Id : {processId}", startInfo.FileName, HostProcess.Id);

                // Host may not write startup messages, in which case assume it started
                if (DeploymentParameters.StatusMessagesEnabled)
                {
                    // The timeout here is large, because we don't know how long the test could need
                    // We cover a lot of error cases above, but I want to make sure we eventually give up and don't hang the build
                    // just in case we missed one -anurse
                    await started.Task.TimeoutAfter(TimeSpan.FromMinutes(10));
                }

                return hostExitTokenSource.Token;
            }
        }

        public override void Dispose()
        {
            using (Logger.BeginScope("SelfHost.Dispose"))
            {
                ShutDownIfAnyHostProcess(HostProcess);

                if (DeploymentParameters.PublishApplicationBeforeDeployment)
                {
                    CleanPublishedOutput();
                }

                InvokeUserApplicationCleanup();

                StopTimer();
            }
        }
    }
}
