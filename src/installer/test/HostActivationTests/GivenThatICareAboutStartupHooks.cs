// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Linq;
using Xunit;
using Microsoft.Extensions.DependencyModel;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation.StartupHooks
{
    public class GivenThatICareAboutStartupHooks : IClassFixture<GivenThatICareAboutStartupHooks.SharedTestState>
    {
        private SharedTestState sharedTestState;
        private string startupHookVarName = "DOTNET_STARTUP_HOOKS";

        public GivenThatICareAboutStartupHooks(GivenThatICareAboutStartupHooks.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        // Run the app with a startup hook
        [Fact]
        public void Muxer_activation_of_StartupHook_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookWithNonPublicMethodFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithNonPublicMethodProjectFixture.Copy();
            var startupHookWithNonPublicMethodDll = startupHookWithNonPublicMethodFixture.TestProject.AppDll;

            // Simple startup hook
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdOutContaining("Hello World");

            // Non-public Initialize method
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithNonPublicMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook with non-public method");

            // Ensure startup hook tracing works
            dotnet.Exec(appDll)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdErrContaining("Property STARTUP_HOOKS = " + startupHookDll)
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdOutContaining("Hello World");

            // Startup hook in type that has an additional overload of Initialize with a different signature
            startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithOverloadProjectFixture.Copy();
            startupHookDll = startupHookFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook with overload! Input: 123")
                .And
                .HaveStdOutContaining("Hello World");
        }

        // Run the app with multiple startup hooks
        [Fact]
        public void Muxer_activation_of_Multiple_StartupHooks_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHook2Fixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Copy();
            var startupHook2Dll = startupHook2Fixture.TestProject.AppDll;

            // Multiple startup hooks
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHook2Dll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdOutContaining("Hello from startup hook with dependency!")
                .And
                .HaveStdOutContaining("Hello World");
        }

        // Empty startup hook variable
        [Fact]
        public void Muxer_activation_of_Empty_StartupHook_Variable_Succeeds()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookVar = "";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should()
                .Pass()
                .And
                .HaveStdOutContaining("Hello World");
        }

        // Run the app with a startup hook assembly that depends on assemblies not on the TPA list
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Dependencies_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // Startup hook has a dependency not on the TPA list
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("System.IO.FileNotFoundException: Could not load file or assembly 'Newtonsoft.Json");
        }

        // Run the app with an invalid syntax in startup hook variable
        [Fact]
        public void Muxer_activation_of_Invalid_StartupHook_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var fakeAssembly = Path.GetFullPath("Assembly.dll");
            var fakeAssembly2 = Path.GetFullPath("Assembly2.dll");

            var expectedError = "System.ArgumentException: The syntax of the startup hook variable was invalid.";

            // Missing entries in the hook
            var startupHookVar = fakeAssembly + Path.PathSeparator + Path.PathSeparator + fakeAssembly2;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Leading separator
            startupHookVar = Path.PathSeparator + startupHookDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Trailing separator
            startupHookVar = fakeAssembly + Path.PathSeparator + fakeAssembly2 + Path.PathSeparator;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Syntax errors are caught before any hooks run
            startupHookVar = startupHookDll + Path.PathSeparator;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError)
                .And
                .NotHaveStdOutContaining("Hello from startup hook!");
        }

        // Run the app with a relative path to the startup hook assembly
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Relative_Path_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var relativeAssemblyPath = "Assembly.dll";

            var expectedError = "System.ArgumentException: Absolute path information is required.";

            // Relative path
            var startupHookVar = relativeAssemblyPath;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Relative path error is caught before any hooks run
            startupHookVar = startupHookDll + Path.PathSeparator + "Assembly.dll";
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError)
                .And
                .NotHaveStdOutContaining("Hello from startup hook!");
        }

        // Run the app with missing startup hook assembly
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;
            var startupHookMissingDll = Path.Combine(Path.GetDirectoryName(startupHookDll), "StartupHookMissing.dll");

            var expectedError = "System.IO.FileNotFoundException: Could not load file or assembly '{0}'.";

            // Missing dll is detected with appropriate error
            var startupHookVar = startupHookMissingDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, Path.GetFullPath(startupHookMissingDll)));

            // Missing dll is detected after previous hooks run
            startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining(String.Format(expectedError, Path.GetFullPath((startupHookMissingDll))));
        }

        // Run the app with an invalid startup hook assembly
        [Fact]
        public void Muxer_activation_of_Invalid_StartupHook_Assembly_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookInvalidAssembly = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithInvalidAssembly.Copy();
            var startupHookInvalidAssemblyDll = Path.Combine(Path.GetDirectoryName(startupHookInvalidAssembly.TestProject.AppDll), "StartupHookInvalidAssembly.dll");

            var expectedError = "System.BadImageFormatException";

            // Dll load gives meaningful error message
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookInvalidAssemblyDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Dll load error happens after previous hooks run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookInvalidAssemblyDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);
        }

        // Run the app with the startup hook type missing
        [Fact]
        public void Muxer_activation_of_Missing_StartupHook_Type_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookMissingTypeFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithoutStartupHookTypeProjectFixture.Copy();
            var startupHookMissingTypeDll = startupHookMissingTypeFixture.TestProject.AppDll;

            // Missing type is detected
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookMissingTypeDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook' from assembly 'StartupHook");

            // Missing type is detected after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingTypeDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining("System.TypeLoadException: Could not load type 'StartupHook' from assembly 'StartupHookWithoutStartupHookType");
        }


        // Run the app with a startup hook that doesn't have any Initialize method
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Missing_Method()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var startupHookMissingMethodFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithoutInitializeMethodProjectFixture.Copy();
            var startupHookMissingMethodDll = startupHookMissingMethodFixture.TestProject.AppDll;

            var expectedError = "System.MissingMethodException: Method 'StartupHook.Initialize' not found.";

            // No Initialize method
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookMissingMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(expectedError);

            // Missing Initialize method is caught after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookMissingMethodDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining(expectedError);
        }

        // Run the app with startup hook that has no static void Initialize() method
        [Fact]
        public void Muxer_activation_of_StartupHook_With_Incorrect_Method_Signature_Fails()
        {
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookProjectFixture.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            var expectedError = "System.ArgumentException: The signature of the startup hook 'StartupHook.Initialize' in assembly '{0}' was invalid. It must be 'public static void Initialize()'.";

            // Initialize is an instance method
            var startupHookWithInstanceMethodFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithInstanceMethodProjectFixture.Copy();
            var startupHookWithInstanceMethodDll = startupHookWithInstanceMethodFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithInstanceMethodDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, startupHookWithInstanceMethodDll));

            // Initialize method takes parameters
            var startupHookWithParameterFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithParameterProjectFixture.Copy();
            var startupHookWithParameterDll = startupHookWithParameterFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithParameterDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, startupHookWithParameterDll));

            // Initialize method has non-void return type
            var startupHookWithReturnTypeFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithReturnTypeProjectFixture.Copy();
            var startupHookWithReturnTypeDll = startupHookWithReturnTypeFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithReturnTypeDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, startupHookWithReturnTypeDll));

            // Initialize method that has multiple methods with an incorrect signature
            var startupHookWithMultipleIncorrectSignaturesFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithMultipleIncorrectSignaturesProjectFixture.Copy();
            var startupHookWithMultipleIncorrectSignaturesDll = startupHookWithMultipleIncorrectSignaturesFixture.TestProject.AppDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookWithMultipleIncorrectSignaturesDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining(String.Format(expectedError, startupHookWithMultipleIncorrectSignaturesDll));

            // Signature problem is caught after previous hooks have run
            var startupHookVar = startupHookDll + Path.PathSeparator + startupHookWithMultipleIncorrectSignaturesDll;
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookVar)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdOutContaining("Hello from startup hook!")
                .And
                .HaveStdErrContaining(String.Format(expectedError, startupHookWithMultipleIncorrectSignaturesDll));
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
            var fixture = sharedTestState.PreviouslyPublishedAndRestoredPortableAppWithMissingRefProjectFixture.Copy();
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var appDepsJson = Path.Combine(Path.GetDirectoryName(appDll), Path.GetFileNameWithoutExtension(appDll) + ".deps.json");
            RemoveLibraryFromDepsJson(appDepsJson, "SharedLibrary.dll");

            var startupHookFixture = sharedTestState.PreviouslyPublishedAndRestoredStartupHookWithAssemblyResolver.Copy();
            var startupHookDll = startupHookFixture.TestProject.AppDll;

            // No startup hook results in failure due to missing app dependency
            dotnet.Exec(appDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .HaveStdErrContaining("FileNotFoundException: Could not load file or assembly 'SharedLibrary");

            // Startup hook with assembly resolver results in use of injected dependency (which has value 2)
            dotnet.Exec(appDll)
                .EnvironmentVariable(startupHookVarName, startupHookDll)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute(fExpectedToFail: true)
                .Should()
                .Fail()
                .And
                .ExitWith(2);
        }

        public class SharedTestState : IDisposable
        {
            // Entry point projects
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture { get; set; }
            // Entry point with missing reference assembly
            public TestProjectFixture PreviouslyPublishedAndRestoredPortableAppWithMissingRefProjectFixture { get; set; }

            // Correct startup hooks
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithOverloadProjectFixture { get; set; }
            // Missing startup hook type (no StartupHook type defined)
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithoutStartupHookTypeProjectFixture { get; set; }
            // Missing startup hook method (no Initialize method defined)
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithoutInitializeMethodProjectFixture { get; set; }
            // Invalid startup hook assembly
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithInvalidAssembly { get; set; }
            // Invalid startup hooks (incorrect signatures)
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithNonPublicMethodProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithInstanceMethodProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithParameterProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithReturnTypeProjectFixture { get; set; }
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithMultipleIncorrectSignaturesProjectFixture { get; set; }
            // Valid startup hooks with incorrect behavior
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture { get; set; }

            // Startup hook with an assembly resolver
            public TestProjectFixture PreviouslyPublishedAndRestoredStartupHookWithAssemblyResolver { get; set; }

            public RepoDirectoriesProvider RepoDirectories { get; set; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                // Entry point projects
                PreviouslyPublishedAndRestoredPortableAppProjectFixture = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture = new TestProjectFixture("PortableAppWithException", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Entry point with missing reference assembly
                PreviouslyPublishedAndRestoredPortableAppWithMissingRefProjectFixture = new TestProjectFixture("PortableAppWithMissingRef", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                // Correct startup hooks
                PreviouslyPublishedAndRestoredStartupHookProjectFixture = new TestProjectFixture("StartupHook", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                PreviouslyPublishedAndRestoredStartupHookWithOverloadProjectFixture = new TestProjectFixture("StartupHookWithOverload", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Missing startup hook type (no StartupHook type defined)
                PreviouslyPublishedAndRestoredStartupHookWithoutStartupHookTypeProjectFixture = new TestProjectFixture("StartupHookWithoutStartupHookType", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Missing startup hook method (no Initialize method defined)
                PreviouslyPublishedAndRestoredStartupHookWithoutInitializeMethodProjectFixture = new TestProjectFixture("StartupHookWithoutInitializeMethod", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Invalid startup hook assembly
                PreviouslyPublishedAndRestoredStartupHookWithInvalidAssembly = new TestProjectFixture("StartupHookFake", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Invalid startup hooks (incorrect signatures)
                PreviouslyPublishedAndRestoredStartupHookWithNonPublicMethodProjectFixture = new TestProjectFixture("StartupHookWithNonPublicMethod", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                PreviouslyPublishedAndRestoredStartupHookWithInstanceMethodProjectFixture = new TestProjectFixture("StartupHookWithInstanceMethod", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                PreviouslyPublishedAndRestoredStartupHookWithParameterProjectFixture = new TestProjectFixture("StartupHookWithParameter", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                PreviouslyPublishedAndRestoredStartupHookWithReturnTypeProjectFixture = new TestProjectFixture("StartupHookWithReturnType", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                PreviouslyPublishedAndRestoredStartupHookWithMultipleIncorrectSignaturesProjectFixture = new TestProjectFixture("StartupHookWithMultipleIncorrectSignatures", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
                // Valid startup hooks with incorrect behavior
                PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture = new TestProjectFixture("StartupHookWithDependency", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();

                // Startup hook with an assembly resolver
                PreviouslyPublishedAndRestoredStartupHookWithAssemblyResolver = new TestProjectFixture("StartupHookWithAssemblyResolver", RepoDirectories)
                    .EnsureRestored(RepoDirectories.CorehostPackages)
                    .PublishProject();
            }

            public void Dispose()
            {
                // Entry point projects
                PreviouslyPublishedAndRestoredPortableAppProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredPortableAppWithExceptionProjectFixture.Dispose();
                // Entry point with missing reference assembly
                PreviouslyPublishedAndRestoredPortableAppWithMissingRefProjectFixture.Dispose();

                // Correct startup hooks
                PreviouslyPublishedAndRestoredStartupHookProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithOverloadProjectFixture.Dispose();
                // Missing startup hook type (no StartupHook type defined)
                PreviouslyPublishedAndRestoredStartupHookWithoutStartupHookTypeProjectFixture.Dispose();
                // Missing startup hook method (no Initialize method defined)
                PreviouslyPublishedAndRestoredStartupHookWithoutInitializeMethodProjectFixture.Dispose();
                // Invalid startup hook assembly
                PreviouslyPublishedAndRestoredStartupHookWithInvalidAssembly.Dispose();
                // Invalid startup hooks (incorrect signatures)
                PreviouslyPublishedAndRestoredStartupHookWithNonPublicMethodProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithInstanceMethodProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithParameterProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithReturnTypeProjectFixture.Dispose();
                PreviouslyPublishedAndRestoredStartupHookWithMultipleIncorrectSignaturesProjectFixture.Dispose();
                // Valid startup hooks with incorrect behavior
                PreviouslyPublishedAndRestoredStartupHookWithDependencyProjectFixture.Dispose();

                // Startup hook with an assembly resolver
                PreviouslyPublishedAndRestoredStartupHookWithAssemblyResolver.Dispose();
            }
        }
    }
}
