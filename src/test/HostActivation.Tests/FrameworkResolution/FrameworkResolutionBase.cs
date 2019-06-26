// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.IO;
using System.Linq;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.FrameworkResolution
{
    public abstract partial class FrameworkResolutionBase
    {
        protected const string MicrosoftNETCoreApp = "Microsoft.NETCore.App";

        public static class ResolvedFramework
        {
            public const string NotFound = "[not found]";
            public const string FailedToReconcile = "[failed to reconcile]";
        }

        protected CommandResult RunTest(
            DotNetCli dotnet,
            TestApp app,
            TestSettings settings,
            Action<CommandResult> resultAction = null,
            bool multiLevelLookup = false)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = settings.DotnetCustomizer == null ? null : dotnet.Customize())
            {
                settings.DotnetCustomizer?.Invoke(dotnetCustomizer);

                if (settings.RuntimeConfigCustomizer != null)
                {
                    settings.RuntimeConfigCustomizer(RuntimeConfig.Path(app.RuntimeConfigJson)).Save();
                }

                settings.WithCommandLine(app.AppDll);

                Command command = dotnet.Exec(settings.CommandLine.First(), settings.CommandLine.Skip(1).ToArray());

                if (settings.WorkingDirectory != null)
                {
                    command = command.WorkingDirectory(settings.WorkingDirectory);
                }

                CommandResult result = command
                    .EnvironmentVariable("COREHOST_TRACE", "1")
                    .EnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", multiLevelLookup ? "1" : "0")
                    .Environment(settings.Environment)
                    .CaptureStdOut()
                    .CaptureStdErr()
                    .Execute();

                resultAction?.Invoke(result);

                return result;
            }
        }

        public class SharedTestStateBase : IDisposable
        {
            private readonly string _builtDotnet;
            private readonly string _baseDir;

            public SharedTestStateBase()
            {
                _builtDotnet = Path.Combine(TestArtifact.TestArtifactsPath, "sharedFrameworkPublish");

                string baseDir = Path.Combine(TestArtifact.TestArtifactsPath, "frameworkResolution");
                _baseDir = SharedFramework.CalculateUniqueTestDirectory(baseDir);
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(_baseDir, _builtDotnet, name);
            }

            public TestApp CreateFrameworkReferenceApp()
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                string testAppDir = Path.Combine(_baseDir, "FrameworkReferenceApp");
                Directory.CreateDirectory(testAppDir);

                // ./FrameworkReferenceApp.dll
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.dll"), string.Empty);

                // ./FrameworkReferenceApp.runtimeconfig.json
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.runtimeconfig.json"), "{}");

                return new TestApp(testAppDir);
            }

            public void Dispose()
            {
                if (!TestArtifact.PreserveTestRuns() && Directory.Exists(_baseDir))
                {
                    Directory.Delete(_baseDir, true);
                }
            }
        }
    }
}
