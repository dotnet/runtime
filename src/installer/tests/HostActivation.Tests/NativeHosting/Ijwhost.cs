// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

using Microsoft.DotNet.Cli.Build.Framework;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.NativeHosting
{
    [PlatformSpecific(TestPlatforms.Windows)] // IJW is only supported on Windows
    public class Ijwhost : IClassFixture<Ijwhost.SharedTestState>
    {
        private readonly SharedTestState sharedState;

        public Ijwhost(SharedTestState sharedTestState)
        {
            sharedState = sharedTestState;
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void LoadLibrary(bool no_runtimeconfig)
        {
            // make a copy of a portion of the shared state because we will modify it
            using (var app = sharedState.IjwApp.Copy())
            {
                string [] args = {
                    "ijwhost",
                    app.AppDll,
                    "NativeEntryPoint"
                };
                if (no_runtimeconfig)
                {
                    File.Delete(app.RuntimeConfigJson);
                }

                CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.RepoDirectories.BuiltDotnet)
                    .Execute();

                if (no_runtimeconfig)
                {
                    result.Should().Fail()
                        .And.HaveStdErrContaining($"Expected active runtime context because runtimeconfig.json [{app.RuntimeConfigJson}] does not exist.");
                }
                else
                {
                    result.Should().Pass()
                        .And.HaveStdOutContaining("[C++/CLI] NativeEntryPoint: calling managed class")
                        .And.HaveStdOutContaining("[C++/CLI] ManagedClass: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext");
                }
            }
        }

        [Fact]
        public void LoadLibraryWithoutRuntimeConfigButActiveRuntime()
        {
            // make a copy of a portion of the shared state because we will modify it
            using (var app = sharedState.IjwApp.Copy())
            {
                // construct runtimeconfig.json
                var startupConfigPath = Path.Combine(Path.GetDirectoryName(app.RuntimeConfigJson),"host.runtimeconfig.json");
                string [] args = {
                    "ijwhost",
                    app.AppDll,
                    "NativeEntryPoint",
                    sharedState.HostFxrPath, // optional 4th and 5th arguments that tell nativehost to start the runtime before loading the C++/CLI library
                    startupConfigPath
                };

                File.Move(app.RuntimeConfigJson, startupConfigPath);

                CommandResult result = sharedState.CreateNativeHostCommand(args, sharedState.RepoDirectories.BuiltDotnet)
                    .Execute();

                result.Should().Pass()
                    .And.HaveStdOutContaining("[C++/CLI] NativeEntryPoint: calling managed class")
                    .And.HaveStdOutContaining("[C++/CLI] ManagedClass: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ManagedHost(bool selfContained)
        {
            string [] args = {
                "ijwhost",
                sharedState.IjwApp.AppDll,
                "NativeEntryPoint"
            };
            TestProjectFixture fixture = selfContained ? sharedState.ManagedHostFixture_SelfContained : sharedState.ManagedHostFixture_FrameworkDependent;
            CommandResult result = Command.Create(fixture.TestProject.AppExe, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(fixture.BuiltDotnet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("[C++/CLI] NativeEntryPoint: calling managed class")
                .And.HaveStdOutContaining("[C++/CLI] ManagedClass: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext")
                .And.ExecuteSelfContained(selfContained);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public string HostFxrPath { get; }
            public TestProjectFixture ManagedHostFixture_FrameworkDependent { get; }
            public TestProjectFixture ManagedHostFixture_SelfContained { get; }
            public TestApp IjwApp {get;}

            public SharedTestState()
            {
                HostFxrPath = TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath;
                string folder = Path.Combine(BaseDirectory, "ijw");
                IjwApp = new TestApp(folder, "ijw");
                // Copy over ijwhost
                string ijwhostName = "ijwhost.dll";
                File.Copy(Path.Combine(RepoDirectories.HostArtifacts, ijwhostName), Path.Combine(folder, ijwhostName));

                // Copy over the C++/CLI test library
                string ijwLibraryName = "ijw.dll";
                File.Copy(Path.Combine(RepoDirectories.HostTestArtifacts, ijwLibraryName), Path.Combine(folder, ijwLibraryName));

                // Create a runtimeconfig.json for the C++/CLI test library
                new RuntimeConfig(Path.Combine(folder, "ijw.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion))
                    .Save();

                ManagedHostFixture_FrameworkDependent = new TestProjectFixture("ManagedHost", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(selfContained: false);

                ManagedHostFixture_SelfContained = new TestProjectFixture("ManagedHost", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(selfContained: true);
            }

            protected override void Dispose(bool disposing)
            {
                ManagedHostFixture_FrameworkDependent.Dispose();
                ManagedHostFixture_SelfContained.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
