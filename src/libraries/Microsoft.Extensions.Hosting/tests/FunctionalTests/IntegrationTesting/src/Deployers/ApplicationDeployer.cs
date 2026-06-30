// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting.IntegrationTesting
{
    /// <summary>
    /// Abstract base class of all deployers with implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IDisposable
    {
        public static readonly string DotnetCommandName = "dotnet";

        private readonly Stopwatch _stopwatch = new Stopwatch();

        private PublishedApplication _publishedApplication;

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
        {
            DeploymentParameters = deploymentParameters;
            LoggerFactory = loggerFactory;
            Logger = LoggerFactory.CreateLogger(GetType().FullName);

            ValidateParameters();
        }

        private void ValidateParameters()
        {
            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.None && !string.IsNullOrEmpty(DeploymentParameters.TargetFramework))
            {
                DeploymentParameters.RuntimeFlavor = GetRuntimeFlavor(DeploymentParameters.TargetFramework);
            }

            if (DeploymentParameters.ApplicationPublisher == null)
            {
                if (string.IsNullOrEmpty(DeploymentParameters.ApplicationPath))
                {
                    throw new ArgumentException("ApplicationPath cannot be null.");
                }

                if (!Directory.Exists(DeploymentParameters.ApplicationPath))
                {
                    throw new DirectoryNotFoundException(string.Format("Application path {0} does not exist.", DeploymentParameters.ApplicationPath));
                }

                if (string.IsNullOrEmpty(DeploymentParameters.ApplicationName))
                {
                    DeploymentParameters.ApplicationName = new DirectoryInfo(DeploymentParameters.ApplicationPath).Name;
                }
            }
        }

        private RuntimeFlavor GetRuntimeFlavor(string tfm)
        {
            if (tfm.ToLowerInvariant().StartsWith("net4"))
            {
                return RuntimeFlavor.Clr;
            }
            return RuntimeFlavor.CoreClr;
        }

        protected DeploymentParameters DeploymentParameters { get; }

        protected ILoggerFactory LoggerFactory { get; }

        protected ILogger Logger { get; }

        public abstract Task<DeploymentResult> DeployAsync();

        protected void DotnetPublish(string publishRoot = null)
        {
            var publisher = DeploymentParameters.ApplicationPublisher ?? new ApplicationPublisher(DeploymentParameters.ApplicationPath);
            _publishedApplication = publisher.Publish(DeploymentParameters, Logger).GetAwaiter().GetResult();
            DeploymentParameters.PublishedApplicationRootPath = _publishedApplication.Path;
        }

        protected void CleanPublishedOutput()
        {
            using (Logger.BeginScope("CleanPublishedOutput"))
            {
                if (DeploymentParameters.PreservePublishedApplicationForDebugging)
                {
                    Logger.LogWarning(
                        "Skipping deleting the locally published folder as property " +
                        $"'{nameof(DeploymentParameters.PreservePublishedApplicationForDebugging)}' is set to 'true'.");
                }
                else
                {
                    _publishedApplication?.Dispose();
                }
            }
        }

        protected string GetDotNetExeForArchitecture()
        {
            var executableName = GetHostDotNetExecutable();
            // We expect x64 dotnet.exe to be on the path but we have to go searching for the x86 version.
            if (DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
            {
                executableName = DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture);
                if (!File.Exists(executableName))
                {
                    throw new Exception($"Unable to find '{executableName}'.'");
                }
            }

            return executableName;
        }

        // Mirrors dotnet/arcade's RemoteExecutor host resolution. The runtime libraries Helix harness runs
        // tests against the testhost via $RUNTIME_PATH/dotnet by absolute path and doesn't add dotnet to PATH
        // (that is only done for workload tests), so launching the host by the bare command name can fail on
        // machines without a global dotnet. Use the host running this test, and when that isn't dotnet (for
        // example an apphost-based testhost) resolve the muxer next to the running shared framework.
        private static string GetHostDotNetExecutable()
        {
            string hostName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet";

            string hostRunner = Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(hostRunner) && string.Equals(Path.GetFileName(hostRunner), hostName, StringComparison.OrdinalIgnoreCase))
            {
                return hostRunner;
            }

            // The running host isn't dotnet (e.g. an apphost). dotnet is located three directories above the
            // runtime directory, for example:
            //   runtime -> <root>/shared/Microsoft.NETCore.App/<version>
            //   dotnet  -> <root>/dotnet
            // This also works for a locally built runtime/testhost.
            var runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            if (!string.IsNullOrEmpty(runtimeDirectory))
            {
                string dotnetRoot = Path.GetFullPath(Path.Combine(runtimeDirectory, "..", "..", ".."));
                string muxer = Path.Combine(dotnetRoot, hostName);
                if (File.Exists(muxer))
                {
                    return muxer;
                }
            }

            return DotnetCommandName;
        }

        protected void ShutDownIfAnyHostProcess(Process hostProcess)
        {
            if (hostProcess is not null && IsRunning(hostProcess))
            {
                Logger.LogInformation("Attempting to cancel process {0}", hostProcess.Id);

                // Shutdown the host process.
                hostProcess.KillTree();
                if (IsRunning(hostProcess))
                {
                    Logger.LogWarning("Unable to terminate the host process with process Id '{processId}'", hostProcess.Id);
                }
                else
                {
                    Logger.LogInformation("Successfully terminated host process with process Id '{processId}'", hostProcess.Id);
                }
            }
            else
            {
                Logger.LogWarning("Host process already exited or never started successfully.");
            }
        }

        // Process.HasExited throws InvalidOperationException ("No process is associated with this object")
        // when the process was never started (and also after the Process has been disposed, which disassociates
        // it). Treat that as "not running" so shutdown cleanup stays non-throwing rather than masking the
        // original start failure with a misleading exception.
        private static bool IsRunning(Process hostProcess)
        {
            try
            {
                return !hostProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        protected void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo, IDictionary<string, string> environmentVariables)
        {
            var environment = startInfo.Environment;
            ProcessHelpers.SetEnvironmentVariable(environment, "ASPNETCORE_ENVIRONMENT", DeploymentParameters.EnvironmentName, Logger);
            ProcessHelpers.AddEnvironmentVariablesToProcess(startInfo, environmentVariables, Logger);
        }

        protected void InvokeUserApplicationCleanup()
        {
            using (Logger.BeginScope("UserAdditionalCleanup"))
            {
                if (DeploymentParameters.UserAdditionalCleanup != null)
                {
                    // User cleanup.
                    try
                    {
                        DeploymentParameters.UserAdditionalCleanup(DeploymentParameters);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogWarning("User cleanup code failed with exception : {exception}", exception.Message);
                    }
                }
            }
        }

        protected void TriggerHostShutdown(CancellationTokenSource hostShutdownSource)
        {
            Logger.LogInformation("Host process shutting down.");
            try
            {
                hostShutdownSource.Cancel();
            }
            catch (Exception)
            {
                // Suppress errors.
            }
        }

        protected void StartTimer()
        {
            Logger.LogInformation($"Deploying {DeploymentParameters.ToString()}");
            _stopwatch.Start();
        }

        protected void StopTimer()
        {
            _stopwatch.Stop();
            Logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", _stopwatch.Elapsed.TotalSeconds);
        }

        public abstract void Dispose();
    }
}
