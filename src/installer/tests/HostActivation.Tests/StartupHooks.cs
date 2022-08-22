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

        // Run the app with a startup hook
        [Fact]
        public void Muxer_activation_of_StartupHook_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookWithNonPublicMethodFixture = sharedTestState.StartupHookWithNonPublicMethodFixture.Copy();
            var startupHookWithNonPublicMethodDll = startupHookWithNonPublicMethodFixture.TestProject.AppDll;

            // Simple startup hook
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");

            // Non-public Initialize method
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithNonPublicMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook with non-public method");

            // Ensure startup hook tracing works
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrContaining("Property STARTUP_HOOKS = " + startupHookDll)
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");

            // Startup hook in type that has an additional overload of Initialize with a different signature
            startupHookFixture = sharedTestState.StartupHookWithOverloadFixture.Copy();
            startupHookDll = startupHookFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook with overload! Input: 123")
                .And.HaveStdOutContaining("Hello World");
        }

        // Run the app with multiple startup hooks
        [Fact]
        public void Muxer_activation_of_Multiple_StartupHooks_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHook2Fixture = sharedTestState.StartupHookWithDependencyFixture.Copy();
            var startupHook2Dll = startupHook2Fixture.TestProject.AppDll;

            // Multiple startup hooks
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHook2Dll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello from startup hook with dependency!")
                .And.HaveStdOutContaining("Hello World");
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
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
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
                .And.HaveStdOutContaining("Hello World");
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

        // Different variants of the startup hook variable format
        [Fact]
        public void Muxer_activation_of_StartupHook_VariableVariants()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHook2Fixture = sharedTestState.StartupHookWithDependencyFixture.Copy();
            var startupHook2Dll = startupHook2Fixture.TestProject.AppDll;

            // Missing entries in the hook
            var startupHookVar = startupHookDll + Path.PathSeparator + Path.PathSeparator + startupHook2Dll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello from startup hook with dependency!")
                .And.HaveStdOutContaining("Hello World");

            // Whitespace is invalid
            startupHookVar = startupHookDll + Path.PathSeparator + " " + Path.PathSeparator + startupHook2Dll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("System.ArgumentException: The startup hook simple assembly name ' ' is invalid.");

            // Leading separator
            startupHookVar = Path.PathSeparator + startupHookDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");

            // Trailing separator
            startupHookVar = startupHookDll + Path.PathSeparator + startupHook2Dll + Path.PathSeparator;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello from startup hook with dependency!")
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_With_Invalid_Simple_Name_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var relativeAssemblyPath = $".{Path.DirectorySeparatorChar}Assembly";

            var expectedError = "System.ArgumentException: The startup hook simple assembly name '{0}' is invalid.";

            // With directory separator
            var startupHookVar = relativeAssemblyPath;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.NotHaveStdErrContaining("--->");

            // With alternative directory separator
            startupHookVar = $".{Path.AltDirectorySeparatorChar}Assembly";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.NotHaveStdErrContaining("--->");

            // With comma
            startupHookVar = $"Assembly,version=1.0.0.0";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.NotHaveStdErrContaining("--->");

            // With space
            startupHookVar = $"Assembly version";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.NotHaveStdErrContaining("--->");

            // With .dll suffix
            startupHookVar = $".{Path.AltDirectorySeparatorChar}Assembly.DLl";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.NotHaveStdErrContaining("--->");

            // With invalid name
            startupHookVar = $"Assembly=Name";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.HaveStdErrContaining("---> System.IO.FileLoadException: The given assembly name was invalid.");

            // Relative path error is caught before any hooks run
            startupHookVar = startupHookDll + Path.PathSeparator + relativeAssemblyPath;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, relativeAssemblyPath))
                .And.NotHaveStdOutContaining("Hello from startup hook!");
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Assembly_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.ArgumentException: Startup hook assembly '{0}' failed to load.";

            // With file path which doesn't exist
            var startupHookVar = startupHookDll + ".missing.dll";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.HaveStdErrContaining($"---> System.IO.FileNotFoundException: Could not load file or assembly '{startupHookVar}'. The system cannot find the file specified.");

            // With simple name which won't resolve
            startupHookVar = "MissingAssembly";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookVar))
                .And.HaveStdErrContaining($"---> System.IO.FileNotFoundException: Could not load file or assembly '{startupHookVar}");
        }

        [Fact]
        public void Muxer_activation_of_StartupHook_WithSimpleAssemblyName_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;
            var startupHookAssemblyName = Path.GetFileNameWithoutExtension(startupHookDll);

            File.Copy(startupHookDll, Path.Combine(fixture.TestProject.BuiltApp.Location, Path.GetFileName(startupHookDll)));

            SharedFramework.AddReferenceToDepsJson(
                fixture.TestProject.DepsJson,
                $"{fixture.TestProject.AssemblyName}/1.0.0",
                startupHookAssemblyName,
                "1.0.0");

            fixture.BuiltDotnet.Exec(fixture.TestProject.AppDll)
                .EnvironmentVariable(startupHookVarName, startupHookAssemblyName)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdOutContaining("Hello World");
        }

        // Run the app with missing startup hook assembly
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;
            var startupHookMissingDll = Path.Combine(Path.GetDirectoryName(startupHookDll), "StartupHookMissing.dll");

            var expectedError = "System.IO.FileNotFoundException: Could not load file or assembly '{0}'.";

            // Missing dll is detected with appropriate error
            var startupHookVar = startupHookMissingDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, Path.GetFullPath(startupHookMissingDll)));

            // Missing dll is detected after previous hooks run
            startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdErrContaining(string.Format(expectedError, Path.GetFullPath((startupHookMissingDll))));
        }

        // Run the app with an invalid startup hook assembly
        [Fact]
        public void Muxer_activation_of_Invalid_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookInvalidAssembly = sharedTestState.StartupHookStartupHookInvalidAssemblyFixture.Copy();
            var startupHookInvalidAssemblyDll = Path.Combine(Path.GetDirectoryName(startupHookInvalidAssembly.TestProject.AppDll), "StartupHookInvalidAssembly.dll");

            var expectedError = "System.BadImageFormatException";

            // Dll load gives meaningful error message
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookInvalidAssemblyDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(expectedError);

            // Dll load error happens after previous hooks run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookInvalidAssemblyDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(expectedError);
        }

        // Run the app with the startup hook type missing
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Type_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookMissingTypeFixture = sharedTestState.StartupHookWithoutStartupHookTypeFixture.Copy();
            var startupHookMissingTypeDll = startupHookMissingTypeFixture.TestProject.AppDll;

            // Missing type is detected
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookMissingTypeDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook' from assembly 'StartupHook");

            // Missing type is detected after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingTypeDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook' from assembly 'StartupHookWithoutStartupHookType");
        }


        // Run the app with a startup hook that doesn't have any Initialize method
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Method_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookMissingMethodFixture = sharedTestState.StartupHookWithoutInitializeMethodFixture.Copy();
            var startupHookMissingMethodDll = startupHookMissingMethodFixture.TestProject.AppDll;

            var expectedError = "System.MissingMethodException: Method 'StartupHook.Initialize' not found.";

            // No Initialize method
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookMissingMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(expectedError);

            // Missing Initialize method is caught after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingMethodDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdErrContaining(expectedError);
        }

        // Run the app with startup hook that has no static void Initialize() method
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Incorrect_Method_Signature_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.StartupHookFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.ArgumentException: The signature of the startup hook 'StartupHook.Initialize' in assembly '{0}' was invalid. It must be 'public static void Initialize()'.";

            // Initialize is an instance method
            var startupHookWithInstanceMethodFixture = sharedTestState.StartupHookWithInstanceMethodFixture.Copy();
            var startupHookWithInstanceMethodDll = startupHookWithInstanceMethodFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithInstanceMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookWithInstanceMethodDll));

            // Initialize method takes parameters
            var startupHookWithParameterFixture = sharedTestState.StartupHookWithParameterFixture.Copy();
            var startupHookWithParameterDll = startupHookWithParameterFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithParameterDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookWithParameterDll));

            // Initialize method has non-void return type
            var startupHookWithReturnTypeFixture = sharedTestState.StartupHookWithReturnTypeFixture.Copy();
            var startupHookWithReturnTypeDll = startupHookWithReturnTypeFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithReturnTypeDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookWithReturnTypeDll));

            // Initialize method that has multiple methods with an incorrect signature
            var startupHookWithMultipleIncorrectSignaturesFixture = sharedTestState.StartupHookWithMultipleIncorrectSignaturesFixture.Copy();
            var startupHookWithMultipleIncorrectSignaturesDll = startupHookWithMultipleIncorrectSignaturesFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithMultipleIncorrectSignaturesDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookWithMultipleIncorrectSignaturesDll));

            // Signature problem is caught after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookWithMultipleIncorrectSignaturesDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdOutContaining("Hello from startup hook!")
                .And.HaveStdErrContaining(string.Format(expectedError, startupHookWithMultipleIncorrectSignaturesDll));
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
            public TestProjectFixture StartupHookWithOverloadFixture { get; }
            // Missing startup hook type (no StartupHook type defined)
            public TestProjectFixture StartupHookWithoutStartupHookTypeFixture { get; }
            // Missing startup hook method (no Initialize method defined)
            public TestProjectFixture StartupHookWithoutInitializeMethodFixture { get; }
            // Invalid startup hook assembly
            public TestProjectFixture StartupHookStartupHookInvalidAssemblyFixture { get; }
            // Invalid startup hooks (incorrect signatures)
            public TestProjectFixture StartupHookWithNonPublicMethodFixture { get; }
            public TestProjectFixture StartupHookWithInstanceMethodFixture { get; }
            public TestProjectFixture StartupHookWithParameterFixture { get; }
            public TestProjectFixture StartupHookWithReturnTypeFixture { get; }
            public TestProjectFixture StartupHookWithMultipleIncorrectSignaturesFixture { get; }
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
                StartupHookWithOverloadFixture = new TestProjectFixture("StartupHookWithOverload", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                // Missing startup hook type (no StartupHook type defined)
                StartupHookWithoutStartupHookTypeFixture = new TestProjectFixture("StartupHookWithoutStartupHookType", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                // Missing startup hook method (no Initialize method defined)
                StartupHookWithoutInitializeMethodFixture = new TestProjectFixture("StartupHookWithoutInitializeMethod", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                // Invalid startup hook assembly
                StartupHookStartupHookInvalidAssemblyFixture = new TestProjectFixture("StartupHookFake", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                // Invalid startup hooks (incorrect signatures)
                StartupHookWithNonPublicMethodFixture = new TestProjectFixture("StartupHookWithNonPublicMethod", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                StartupHookWithInstanceMethodFixture = new TestProjectFixture("StartupHookWithInstanceMethod", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                StartupHookWithParameterFixture = new TestProjectFixture("StartupHookWithParameter", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                StartupHookWithReturnTypeFixture = new TestProjectFixture("StartupHookWithReturnType", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();
                StartupHookWithMultipleIncorrectSignaturesFixture = new TestProjectFixture("StartupHookWithMultipleIncorrectSignatures", RepoDirectories)
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
                StartupHookWithOverloadFixture.Dispose();
                // Missing startup hook type (no StartupHook type defined)
                StartupHookWithoutStartupHookTypeFixture.Dispose();
                // Missing startup hook method (no Initialize method defined)
                StartupHookWithoutInitializeMethodFixture.Dispose();
                // Invalid startup hook assembly
                StartupHookStartupHookInvalidAssemblyFixture.Dispose();
                // Invalid startup hooks (incorrect signatures)
                StartupHookWithNonPublicMethodFixture.Dispose();
                StartupHookWithInstanceMethodFixture.Dispose();
                StartupHookWithParameterFixture.Dispose();
                StartupHookWithReturnTypeFixture.Dispose();
                StartupHookWithMultipleIncorrectSignaturesFixture.Dispose();
                // Valid startup hooks with incorrect behavior
                StartupHookWithDependencyFixture.Dispose();

                // Startup hook with an assembly resolver
                StartupHookWithAssemblyResolver.Dispose();
            }
        }
    }
}
