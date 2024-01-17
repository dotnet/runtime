// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

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
            var app = sharedTestState.App.Copy();
            var startupHookDll = sharedTestState.StartupHook.AppDll;

            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithProperty(startupHookRuntimeConfigName, startupHookDll)
                .Save();

            // RuntimeConfig defined startup hook
            TestContext.BuiltDotNet.Exec(app.AppDll)
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
            var app = sharedTestState.App.Copy();
            var startupHookDll = sharedTestState.StartupHook.AppDll;

            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithProperty(startupHookRuntimeConfigName, startupHookDll)
                .Save();

            var startupHook2 = sharedTestState.StartupHookWithAssemblyResolver;
            var startupHook2Dll = startupHook2.AppDll;

            // include any char to counter output from other threads such as in #57243
            const string wildcardPattern = @"[\r\n\s.]*";

            // RuntimeConfig and Environment startup hooks in expected order
            TestContext.BuiltDotNet.Exec(app.AppDll)
                .EnvironmentVariable(startupHookVarName, startupHook2Dll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutMatching($"Hello from startup hook in {startupHook2.AssemblyName}!" +
                                        wildcardPattern +
                                        $"Hello from startup hook!" +
                                        wildcardPattern +
                                        "Hello World");
        }

        // Empty startup hook variable
        [Fact]
        public void Muxer_activation_of_Empty_StartupHook_Variable_Succeeds()
        {
            var startupHookVar = "";
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
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
            var startupHookDll = sharedTestState.StartupHookWithAssemblyResolver.AppDll;

            // Startup hook has a dependency not on the TPA list
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
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
            var startupHookDll = sharedTestState.StartupHookWithAssemblyResolver.AppDll;

            // Startup hook with assembly resolver results in use of injected dependency
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll, "load_shared_library")
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                // Indicate that the startup hook should add an assembly resolver
                .EnvironmentVariable("TEST_STARTUPHOOK_ADD_RESOLVER", true.ToString())
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Resolving SharedLibrary in startup hook")
                .And.HaveStdOutContaining("SharedLibrary.SharedType.Value = SharedLibrary");
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_With_IsSupported_False()
        {
            var app = sharedTestState.App.Copy();
            var startupHookDll = sharedTestState.StartupHook.AppDll;

            RuntimeConfig.FromFile(app.RuntimeConfigJson)
                .WithProperty(startupHookSupport, "false")
                .Save();

            // Startup hooks are not executed when the StartupHookSupport
            // feature switch is set to false.
            TestContext.BuiltDotNet.Exec(app.AppDll)
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
            // Entry point application
            public TestApp App { get; }

            // Correct startup hook
            public TestApp StartupHook { get; }

            // Startup hook that can be configured to add an assembly resolver or use a dependency
            public TestApp StartupHookWithAssemblyResolver { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                StartupHook = TestApp.CreateFromBuiltAssets("StartupHook");
                StartupHookWithAssemblyResolver = TestApp.CreateFromBuiltAssets("StartupHookWithAssemblyResolver");
            }

            public void Dispose()
            {
                App?.Dispose();
                StartupHook?.Dispose();
                StartupHookWithAssemblyResolver?.Dispose();
            }
        }
    }
}
