// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;

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
            bool? multiLevelLookup = false)
        {
            using (DotNetCliExtensions.DotNetCliCustomizer dotnetCustomizer = settings.DotnetCustomizer == null ? null : dotnet.Customize())
            {
                settings.DotnetCustomizer?.Invoke(dotnetCustomizer);

                if (app is not null)
                {
                    if (settings.RuntimeConfigCustomizer != null)
                    {
                        settings.RuntimeConfigCustomizer(RuntimeConfig.Path(app.RuntimeConfigJson)).Save();
                    }

                    settings.WithCommandLine(app.AppDll);
                }

                Command command = dotnet.Exec(settings.CommandLine.First(), settings.CommandLine.Skip(1).ToArray());

                if (settings.WorkingDirectory != null)
                {
                    command = command.WorkingDirectory(settings.WorkingDirectory);
                }

                CommandResult result = command
                    .EnableTracingAndCaptureOutputs()
                    .MultilevelLookup(multiLevelLookup)
                    .Environment(settings.Environment)
                    .Execute();

                resultAction?.Invoke(result);

                return result;
            }
        }

        protected CommandResult RunSelfContainedTest(
            TestApp app,
            TestSettings settings)
        {
            if (settings.RuntimeConfigCustomizer != null)
            {
                settings.RuntimeConfigCustomizer(RuntimeConfig.Path(app.RuntimeConfigJson)).Save();
            }

            settings.WithCommandLine(app.AppDll);

            Command command = Command.Create(app.AppExe, settings.CommandLine);

            if (settings.WorkingDirectory != null)
            {
                command = command.WorkingDirectory(settings.WorkingDirectory);
            }

            CommandResult result = command
                .EnableTracingAndCaptureOutputs()
                .Environment(settings.Environment)
                .Execute();

            return result;
        }

        public class SharedTestStateBase : IDisposable
        {
            private readonly TestArtifact _baseDirArtifact;

            public SharedTestStateBase()
            {
                _baseDirArtifact = TestArtifact.Create("frameworkResolution");
            }

            public DotNetBuilder DotNet(string name)
            {
                return new DotNetBuilder(_baseDirArtifact.Location, TestContext.BuiltDotNet.BinPath, name);
            }

            public TestApp CreateFrameworkReferenceApp()
            {
                // Prepare the app mock - we're not going to run anything really, so we just need the basic files
                string testAppDir = Path.Combine(_baseDirArtifact.Location, "FrameworkReferenceApp");
                Directory.CreateDirectory(testAppDir);

                // ./FrameworkReferenceApp.dll
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.dll"), string.Empty);

                // ./FrameworkReferenceApp.runtimeconfig.json
                File.WriteAllText(Path.Combine(testAppDir, "FrameworkReferenceApp.runtimeconfig.json"), "{}");

                return new TestApp(testAppDir);
            }

            public TestApp CreateSelfContainedAppWithMockHostPolicy()
            {
                string testAppDir = Path.Combine(_baseDirArtifact.Location, "SelfContainedApp");
                TestApp testApp = new TestApp(testAppDir);
                testApp.PopulateSelfContained(TestApp.MockedComponent.HostPolicy);

                // ./SelfContainedApp.exe
                testApp.CreateAppHost(copyResources: false);

                return testApp;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _baseDirArtifact.Dispose();
                }
            }
        }
    }
}
