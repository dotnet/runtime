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

            var startupHookFixture = sharedTestState.StartupHookFixture;
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

            var startupHookFixture = sharedTestState.StartupHookFixture;
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            RuntimeConfig.FromFile(fixture.TestProject.RuntimeConfigJson)
                .WithProperty(startupHookRuntimeConfigName, startupHookDll)
                .Save();

            var startupHook2Fixture = sharedTestState.StartupHookWithAssemblyResolver;
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
                .And.HaveStdOutMatching($"Hello from startup hook in {startupHook2Fixture.TestProject.AssemblyName}!" +
                                        wildcardPattern +
                                        $"Hello from startup hook!" +
                                        wildcardPattern +
                                        "Hello World");
        }

        // Empty startup hook variable
        [Fact]
        public void Muxer_activation_of_Empty_StartupHook_Variable_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture;
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
            var fixture = sharedTestState.PortableAppFixture;
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookWithAssemblyResolver;
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Startup hook has a dependency not on the TPA list
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                // Indicate that the startup hook should try to use a dependency
                .EnvironmentVariable("TEST_STARTUPHOOK_USE_DEPENDENCY", true.ToString())
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("System.IO.FileNotFoundException: Could not load file or assembly 'SharedLibrary");
        }

        // Run startup hook that adds an assembly resolver
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Assembly_Resolver()
        {
            var fixture = sharedTestState.PortableAppFixture;
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookWithAssemblyResolver;
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Startup hook with assembly resolver results in use of injected dependency
            dotnet.Exec(appDll, "load_shared_library")
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                // Indicate that the startup hook should add an assembly resolver
                .EnvironmentVariable("TEST_STARTUPHOOK_ADD_RESOLVER", true.ToString())
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Resolving SharedLibrary in startup hook")
                .And.HaveStdOutContaining("SharedLibrary.SharedType.Value=2");
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_With_IsSupported_False()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture;
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
            // Entry point project
            public TestProjectFixture PortableAppFixture { get; }

            // Correct startup hook
            public TestProjectFixture StartupHookFixture { get; }

            // Startup hook that can be configured to add an assembly resolver or use a dependency
            public TestProjectFixture StartupHookWithAssemblyResolver { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                // Entry point project
                PortableAppFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                // Correct startup hook
                StartupHookFixture = new TestProjectFixture("StartupHook", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                // Startup hook that can be configured to add an assembly resolver or use a dependency
                StartupHookWithAssemblyResolver = new TestProjectFixture("StartupHookWithAssemblyResolver", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
            }

            public void Dispose()
            {
                PortableAppFixture.Dispose();
                StartupHookFixture.Dispose();
                StartupHookWithAssemblyResolver.Dispose();
            }
        }
    }
}
