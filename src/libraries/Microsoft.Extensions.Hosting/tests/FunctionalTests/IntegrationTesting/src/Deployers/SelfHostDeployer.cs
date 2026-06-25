// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
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
        internal event DataReceivedEventHandler OutputReceived;

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
                    Version version = Environment.Version;
                    var targetFramework = DeploymentParameters.TargetFramework
                        ?? (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? "net481" : $"net{version.Major}.{version.Minor}");
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
                var hostExitTokenSource = new CancellationTokenSource();

                await StartHostWithRetryAsync(startInfo, executableName, started, hostExitTokenSource);

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
                    await started.Task.WaitAsync(TimeSpan.FromMinutes(10));
                }

                return hostExitTokenSource.Token;
            }
        }

        // Launching the host process can fail transiently on constrained CI/Helix machines (for example a
        // failed fork or a momentarily unavailable executable). Retry a few times before giving up so a
        // one-off launch failure doesn't fail the test; the final failure is rethrown with its real cause.
        private async Task StartHostWithRetryAsync(ProcessStartInfo startInfo, string executableName, TaskCompletionSource<object> started, CancellationTokenSource hostExitTokenSource)
        {
            const int MaxAttempts = 3;
            TimeSpan retryDelay = TimeSpan.FromSeconds(2);

            for (int attempt = 1; ; attempt++)
            {
                var process = new Process() { StartInfo = startInfo };
                process.EnableRaisingEvents = true;
                process.OutputDataReceived += (sender, dataArgs) =>
                {
                    if (string.Equals(dataArgs.Data, ApplicationStartedMessage))
                    {
                        started.TrySetResult(null);
                    }

                    OutputReceived?.Invoke(sender, dataArgs);
                };
                process.Exited += (sender, e) =>
                {
                    Logger.LogInformation("host process ID {pid} shut down", process.Id);

                    // If TrySetResult was called above, this will just silently fail to set the new state, which is what we want
                    started.TrySetException(new Exception($"Command exited unexpectedly with exit code: {process.ExitCode}"));

                    TriggerHostShutdown(hostExitTokenSource);
                };

                HostProcess = process;

                try
                {
                    process.StartAndCaptureOutAndErrToLogger(executableName, Logger);
                    return;
                }
                catch (Exception ex) when (attempt < MaxAttempts)
                {
                    Logger.LogWarning("Attempt {attempt} of {maxAttempts} to start the host process failed; retrying in {delaySeconds}s. Exception: {exception}",
                        attempt, MaxAttempts, retryDelay.TotalSeconds, ex.ToString());
                    process.Dispose();
                    await Task.Delay(retryDelay);
                }
                catch (Exception ex)
                {
                    Logger.LogError("Failed to start the host process after {maxAttempts} attempts. Exception: {exception}", MaxAttempts, ex.ToString());
                    throw;
                }
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
