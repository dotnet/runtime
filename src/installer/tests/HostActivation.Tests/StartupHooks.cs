// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class StartupHooks : IClassFixture<StartupHooks.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private string startupHookVarName = "DOTNET_STARTUP_HOOKS";
        private string startupHookRuntimeConfigName = "STARTUP_HOOKS";
        private string startupHookSupport = "System.StartupHookProvider.IsSupported";

        public StartupHooks(StartupHooks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_activation_of_RuntimeConfig_StartupHook_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(startupHookRuntimeConfigName, startupHookDll)
                .Save();

            // RuntimeConfig defined startup hook
            dotnet.Exec(appDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining($"Property STARTUP_HOOKS = {startupHookDll}")
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_activation_of_RuntimeConfig_And_Environment_StartupHooks_SucceedsInExpectedOrder()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(startupHookRuntimeConfigName, startupHookDll)
                .Save();

            var startupHook2Fixture = sharedTestState.StartupHookWithDependencyFixture.Copy();
            var startupHook2Dll = startupHook2Fixture.TestProject.AppDll;

            // include any char to counter output from other threads such as in #57243
            const string wildcardPattern = @"[\r\n\s.]*";

            // RuntimeConfig and Environment startup hooks in expected order
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHook2Dll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutMatching("Hello from startup hook with dependency!" +
                                        wildcardPattern +
                                        "Hello from startup hook!" +
                                        wildcardPattern +
                                        "Hello World");
        }

        // Empty startup hook variable
        [Fact]
        public void Muxer_activation_of_Empty_StartupHook_Variable_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookVar = "";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.NotHaveStdErr();
        }

        // Run the app with a startup hook assembly that depends on assemblies not on the TPA list
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Dependencies_Fails()
        {
            var fixture = sharedTestState.PortableAppWithExceptionFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookWithDependencyFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Startup hook has a dependency not on the TPA list
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("System.IO.FileNotFoundException: Could not load file or assembly 'Newtonsoft.Json");
        }

        private static void RemoveLibraryFromDepsJson(string depsJsonPath, string libraryName)
        {
            DependencyContext context;
            using (FileStream fileStream = File.Open(depsJsonPath, FileMode.Open))
            {
                using (DependencyContextJsonReader reader = new DependencyContextJsonReader())
                {
                    context = reader.Read(fileStream);
                }
            }

            context = new DependencyContext(context.Target,
                context.CompilationOptions,
                context.CompileLibraries,
                context.RuntimeLibraries.Select(lib => new RuntimeLibrary(
                    lib.Type,
                    lib.Name,
                    lib.Version,
                    lib.Hash,
                    lib.RuntimeAssemblyGroups.Select(assemblyGroup => new RuntimeAssetGroup(
                        assemblyGroup.Runtime,
                        assemblyGroup.RuntimeFiles.Where(f => !f.Path.EndsWith("SharedLibrary.dll")))).ToList().AsReadOnly(),
                    lib.NativeLibraryGroups,
                    lib.ResourceAssemblies,
                    lib.Dependencies,
                    lib.Serviceable,
                    lib.Path,
                    lib.HashPath,
                    lib.RuntimeStoreManifestName)),
                context.RuntimeGraph);

            using (FileStream fileStream = File.Open(depsJsonPath, FileMode.Truncate, FileAccess.Write))
            {
                DependencyContextWriter writer = new DependencyContextWriter();
                writer.Write(context, fileStream);
            }
        }

        // Run startup hook that adds an assembly resolver
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Assembly_Resolver()
        {
            var fixture = sharedTestState.PortableAppWithMissingRefFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var appDepsJson = Path.Combine(Path.GetDirectoryName(appDll), Path.GetFileNameWithoutExtension(appDll) + ".deps.json");
            RemoveLibraryFromDepsJson(appDepsJson, "SharedLibrary.dll");

            var startupHookFixture = sharedTestState.StartupHookWithAssemblyResolver.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // No startup hook results in failure due to missing app dependency
            dotnet.Exec(appDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("FileNotFoundException: Could not load file or assembly 'SharedLibrary");

            // Startup hook with assembly resolver results in use of injected dependency (which has value 2)
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.ExitWith(2);
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_With_IsSupported_False()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(startupHookSupport, "false")
                .Save();

            // Startup hooks are not executed when the StartupHookSupport
            // feature switch is set to false.
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.NotHaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");
        }

        public class SharedTestState : IDisposable
        {
            // Entry point projects
            public TestProjectFixture PortableAppFixture { get; }
            public TestProjectFixture PortableAppWithExceptionFixture { get; }
            // Entry point with missing reference assembly
            public TestProjectFixture PortableAppWithMissingRefFixture { get; }

            // Correct startup hooks
            public TestProjectFixture StartupHookFixture { get; }

            // Valid startup hooks with incorrect behavior
            public TestProjectFixture StartupHookWithDependencyFixture { get; }

            // Startup hook with an assembly resolver
            public TestProjectFixture StartupHookWithAssemblyResolver { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                // Entry point projects
                PortableAppFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                PortableAppWithExceptionFixture = new TestProjectFixture("PortableAppWithException", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                // Entry point with missing reference assembly
                PortableAppWithMissingRefFixture = new TestProjectFixture("PortableAppWithMissingRef", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                // Correct startup hooks
                StartupHookFixture = new TestProjectFixture("StartupHook", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                // Valid startup hooks with incorrect behavior
                StartupHookWithDependencyFixture = new TestProjectFixture("StartupHookWithDependency", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                // Startup hook with an assembly resolver
                StartupHookWithAssemblyResolver = new TestProjectFixture("StartupHookWithAssemblyResolver", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            public void Dispose()
            {
                // Entry point projects
                PortableAppFixture.Dispose();
                PortableAppWithExceptionFixture.Dispose();
                // Entry point with missing reference assembly
                PortableAppWithMissingRefFixture.Dispose();

                // Correct startup hooks
                StartupHookFixture.Dispose();

                // Valid startup hooks with incorrect behavior
                StartupHookWithDependencyFixture.Dispose();

                // Startup hook with an assembly resolver
                StartupHookWithAssemblyResolver.Dispose();
            }
        }
    }
}
