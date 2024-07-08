// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

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

                CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
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
                    TestContext.BuiltDotNet.GreatestVersionHostFxrFilePath, // optional 4th and 5th arguments that tell nativehost to start the runtime before loading the C++/CLI library
                    startupConfigPath
                };

                File.Move(app.RuntimeConfigJson, startupConfigPath);

                CommandResult result = sharedState.CreateNativeHostCommand(args, TestContext.BuiltDotNet.BinPath)
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
            TestApp app = selfContained ? sharedState.ManagedHost_SelfContained : sharedState.ManagedHost_FrameworkDependent;
            CommandResult result = Command.Create(app.AppExe, args)
                .EnableTracingAndCaptureOutputs()
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .MultilevelLookup(false)
                .Execute();

            result.Should().Pass()
                .And.HaveStdOutContaining("[C++/CLI] NativeEntryPoint: calling managed class")
                .And.HaveStdOutContaining("[C++/CLI] ManagedClass: AssemblyLoadContext = \"Default\" System.Runtime.Loader.DefaultAssemblyLoadContext")
                .And.ExecuteSelfContained(selfContained);
        }

        public class SharedTestState : SharedTestStateBase
        {
            public TestApp ManagedHost_FrameworkDependent { get; }
            public TestApp ManagedHost_SelfContained { get; }
            public TestApp IjwApp {get;}

            public SharedTestState()
            {
                string folder = Path.Combine(BaseDirectory, "ijw");
                IjwApp = new TestApp(folder, "ijw");
                // Copy over ijwhost
                string ijwhostName = "ijwhost.dll";
                File.Copy(Path.Combine(RepoDirectoriesProvider.Default.HostArtifacts, ijwhostName), Path.Combine(folder, ijwhostName));

                // Copy over the C++/CLI test library
                string ijwLibraryName = "ijw.dll";
                File.Copy(Path.Combine(RepoDirectoriesProvider.Default.HostTestArtifacts, ijwLibraryName), Path.Combine(folder, ijwLibraryName));

                // Create a runtimeconfig.json for the C++/CLI test library
                new RuntimeConfig(Path.Combine(folder, "ijw.runtimeconfig.json"))
                    .WithFramework(new RuntimeConfig.Framework(Constants.MicrosoftNETCoreApp, TestContext.MicrosoftNETCoreAppVersion))
                    .Save();

                ManagedHost_FrameworkDependent = TestApp.CreateFromBuiltAssets("ManagedHost");
                ManagedHost_FrameworkDependent.CreateAppHost();

                ManagedHost_SelfContained = TestApp.CreateFromBuiltAssets("ManagedHost");
                ManagedHost_SelfContained.PopulateSelfContained(TestApp.MockedComponent.None);
                ManagedHost_FrameworkDependent.CreateAppHost();
            }

            protected override void Dispose(bool disposing)
            {
                ManagedHost_FrameworkDependent?.Dispose();
                ManagedHost_SelfContained?.Dispose();

                base.Dispose(disposing);
            }
        }
    }
}
