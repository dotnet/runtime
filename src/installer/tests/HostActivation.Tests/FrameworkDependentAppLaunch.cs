// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.Extensions.DependencyModel;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class FrameworkDependentAppLaunch : IClassFixture<FrameworkDependentAppLaunch.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public FrameworkDependentAppLaunch(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_Default()
        {
            var dotnet = HostTestContext.BuiltDotNet;
            var appDll = sharedTestState.App.AppDll;

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");

            dotnet.Exec("exec", appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_AssemblyWithDifferentFileExtension_Fails()
        {
            var app = sharedTestState.App.Copy();

            // Change *.dll to *.exe
            var appDll = app.AppDll;
            var appOtherExt = Path.ChangeExtension(appDll, ".other");
            File.Copy(appDll, appOtherExt, true);
            File.Delete(appDll);

            HostTestContext.BuiltDotNet.Exec(appOtherExt)
                .CaptureStdErr()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The application '{appOtherExt}' does not exist or is not a managed .dll or .exe");
        }

        [Fact]
        public void Muxer_AssemblyWithExeExtension()
        {
            var app = sharedTestState.App.Copy();

            // The host and runtime specifically allow .dll or .exe as extensions for managed assemblies
            // Validate that we can run an app where the managed entry assembly is an .exe
            var appDll = app.AppDll;
            var appExe = Path.ChangeExtension(appDll, ".exe");
            File.Copy(appDll, appExe, true);
            File.Delete(appDll);

            using (FileStream fileStream = File.Open(app.DepsJson, FileMode.Open, FileAccess.ReadWrite))
            using (DependencyContextJsonReader reader = new DependencyContextJsonReader())
            {
                DependencyContext context = reader.Read(fileStream);

                // Update the app .dll in the .deps.json to point at the .exe
                List<RuntimeLibrary> updatedRuntimeLibraries = new(context.RuntimeLibraries);
                RuntimeLibrary existing = updatedRuntimeLibraries.Find(l => l.Name == app.Name);
                updatedRuntimeLibraries.Remove(existing);

                List<RuntimeAssetGroup> updatedAssetGroups = new(existing.RuntimeAssemblyGroups);
                RuntimeAssetGroup existingGroup = existing.RuntimeAssemblyGroups.GetDefaultGroup();
                updatedAssetGroups.Remove(existingGroup);
                updatedAssetGroups.Add(
                    new RuntimeAssetGroup(
                        existingGroup.Runtime,
                        existingGroup.AssetPaths.Where(r => r != Path.GetFileName(app.AppDll)).Append(Path.GetFileName(appExe))));

                RuntimeLibrary updated = new RuntimeLibrary(
                    existing.Type,
                    existing.Name,
                    existing.Version,
                    existing.Hash,
                    updatedAssetGroups,
                    existing.NativeLibraryGroups,
                    existing.ResourceAssemblies,
                    existing.Dependencies,
                    existing.Serviceable);
                updatedRuntimeLibraries.Add(updated);

                DependencyContext newContext = new DependencyContext(
                    context.Target,
                    context.CompilationOptions,
                    context.CompileLibraries,
                    updatedRuntimeLibraries,
                    context.RuntimeGraph);

                fileStream.Seek(0, SeekOrigin.Begin);
                DependencyContextWriter writer = new DependencyContextWriter();
                writer.Write(newContext, fileStream);
                fileStream.SetLength(fileStream.Position);
            }

            HostTestContext.BuiltDotNet.Exec(appExe)
                .CaptureStdOut()
                .CaptureStdErr()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_NonAssemblyWithExeExtension()
        {
            var app = sharedTestState.App.Copy();

            // The host and runtime specifically allow .dll or .exe as extensions for assemblies to run
            // Use the app host as the non-managed assembly that we attempt to run
            app.CreateAppHost();
            string appExe = app.AppExe;
            if (!OperatingSystem.IsWindows())
            {
                appExe = Path.ChangeExtension(appExe, ".exe");
                File.Move(app.AppExe, appExe);
            }

            // If the app being run is not actually a managed assembly, it should come through as a load failure.
            HostTestContext.BuiltDotNet.Exec(appExe)
                .CaptureStdOut()
                .CaptureStdErr()
                .DisableDumps() // Expected to throw an exception
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("BadImageFormatException");
        }

        [Fact]
        public void Muxer_AltDirectorySeparatorChar()
        {
            var appDll = sharedTestState.App.AppDll.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            HostTestContext.BuiltDotNet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_SpecificRuntimeConfig()
        {
            var app = sharedTestState.App.Copy();

            // Move runtime config to a subdirectory
            var subdirectory = Path.Combine(app.Location, "r");
            Directory.CreateDirectory(subdirectory);
            var runtimeConfig = Path.Combine(subdirectory, Path.GetFileName(app.RuntimeConfigJson));
            File.Move(app.RuntimeConfigJson, runtimeConfig, overwrite: true);

            HostTestContext.BuiltDotNet.Exec("exec", "--runtimeconfig", runtimeConfig, app.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void AppHost()
        {
            string appExe = sharedTestState.App.AppExe;
            if (Binaries.CetCompat.IsSupported)
                Assert.True(Binaries.CetCompat.IsMarkedCompatible(appExe));

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(HostTestContext.BuiltDotNet.BinPath, HostTestContext.BuildArchitecture)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(HostTestContext.MicrosoftNETCoreAppVersion);
        }

        [ConditionalFact(typeof(Binaries.CetCompat), nameof(Binaries.CetCompat.IsSupported))]
        public void AppHost_DisableCetCompat()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(disableCetCompat: true);
            Assert.False(Binaries.CetCompat.IsMarkedCompatible(app.AppExe));

            Command.Create(app.AppExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(HostTestContext.BuiltDotNet.BinPath, HostTestContext.BuildArchitecture)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(HostTestContext.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void AppHost_DotNetRoot_DevicePath()
        {
            string appExe = sharedTestState.App.AppExe;

            string dotnetPath = $@"\\?\{HostTestContext.BuiltDotNet.BinPath}";
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(dotnetPath, HostTestContext.BuildArchitecture)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(HostTestContext.MicrosoftNETCoreAppVersion);

            dotnetPath = $@"\\.\{HostTestContext.BuiltDotNet.BinPath}";
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(dotnetPath, HostTestContext.BuildArchitecture)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(HostTestContext.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void RuntimeConfig_FilePath_Breaks_MAX_PATH_Threshold()
        {
            var appExeName = Path.GetFileName(sharedTestState.App.AppExe);

            // Move the portable app to a path such that the length of the executable's fullpath
            // is just 1 char behind MAX_PATH (260) so that the runtimeconfig(.dev).json files
            // break this threshold. This will cause hostfxr to normalize these paths -- here we
            // are checking that the updated paths are used.
            var tmp = Path.GetTempPath();
            var dirName = new string('a', 259 - tmp.Length - appExeName.Length - 1);
            var newDir = Path.Combine(tmp, dirName);
            var appExe = Path.Combine(newDir, appExeName);
            Debug.Assert(appExe.Length == 259);
            Directory.CreateDirectory(newDir);
            foreach (var file in Directory.GetFiles(sharedTestState.App.Location, "*.*", SearchOption.TopDirectoryOnly))
                File.Copy(file, Path.Combine(newDir, Path.GetFileName(file)), true);

            Command.Create(appExe)
                .DotNetRoot(HostTestContext.BuiltDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ComputedTPA_NoTrailingPathSeparator()
        {
            HostTestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdErrMatching($"Property TRUSTED_PLATFORM_ASSEMBLIES = .*[^{Path.PathSeparator}]$", System.Text.RegularExpressions.RegexOptions.Multiline);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MissingRuntimeConfig_Fails(bool useAppHost)
        {
            Command command;
            if (useAppHost)
            {
                command = Command.Create(sharedTestState.MockApp.AppExe)
                    .DotNetRoot(HostTestContext.BuiltDotNet.BinPath, HostTestContext.BuildArchitecture);
            }
            else
            {
                command = HostTestContext.BuiltDotNet.Exec(sharedTestState.MockApp.AppDll);
            }

            command.EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The library '{Binaries.HostPolicy.FileName}' required to execute the application was not found")
                .And.HaveStdErrContaining("Failed to run as a self-contained app")
                .And.HaveStdErrContaining($"'{sharedTestState.MockApp.RuntimeConfigJson}' was not found");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void MissingFrameworkInRuntimeConfig_Fails(bool useAppHost)
        {
            TestApp app = sharedTestState.MockApp.Copy();
            RuntimeConfig.FromFile(app.RuntimeConfigJson).Save();

            Command command;
            if (useAppHost)
            {
                command = Command.Create(app.AppExe)
                    .DotNetRoot(HostTestContext.BuiltDotNet.BinPath, HostTestContext.BuildArchitecture);
            }
            else
            {
                command = HostTestContext.BuiltDotNet.Exec(app.AppDll);
            }

            command.EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The library '{Binaries.HostPolicy.FileName}' required to execute the application was not found")
                .And.HaveStdErrContaining("Failed to run as a self-contained app")
                .And.HaveStdErrContaining($"'{app.RuntimeConfigJson}' did not specify a framework");
        }

        [Fact]
        public void MissingFrameworkName()
        {
            TestApp app = sharedTestState.MockApp.Copy();

            // Create a runtimeconfig.json with a framework that has no name property
            var framework = new RuntimeConfig.Framework(null, HostTestContext.MicrosoftNETCoreAppVersion);
            new RuntimeConfig(app.RuntimeConfigJson)
                .WithFramework(framework)
                .Save();

            Command.Create(app.AppExe)
                .DotNetRoot(HostTestContext.BuiltDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"No framework name specified: {framework.ToJson().ToJsonString(new JsonSerializerOptions { WriteIndented = false })}")
                .And.HaveStdErrContaining($"Invalid runtimeconfig.json [{app.RuntimeConfigJson}]");
        }

        [Fact]
        public void MissingFrameworkVersion()
        {
            TestApp app = sharedTestState.MockApp.Copy();

            // Create a runtimeconfig.json with a framework that has no version property
            new RuntimeConfig(app.RuntimeConfigJson)
                .WithFramework(Constants.MicrosoftNETCoreApp, null)
                .Save();

            Command.Create(app.AppExe)
                .DotNetRoot(HostTestContext.BuiltDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"Framework '{Constants.MicrosoftNETCoreApp}' is missing a version")
                .And.HaveStdErrContaining($"Invalid runtimeconfig.json [{app.RuntimeConfigJson}]");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_CLI_MissingRuntimeFramework_ErrorReportedInStdErr(bool missingHostfxr)
        {
            using (var invalidDotNet = TestArtifact.Create("cliErrors"))
            {
                string expectedUrlQuery;
                string expectedStdErr;
                int expectedErrorCode = 0;
                if (missingHostfxr)
                {
                    expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure;
                    expectedStdErr = $"&apphost_version={HostTestContext.MicrosoftNETCoreAppVersion}";
                    expectedUrlQuery = "missing_runtime=true&";
                }
                else
                {
                    new DotNetBuilder(invalidDotNet.Location, HostTestContext.BuiltDotNet.BinPath, null)
                        .Build();

                    expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure;
                    expectedStdErr = $"Framework: '{Constants.MicrosoftNETCoreApp}', " +
                        $"version '{HostTestContext.MicrosoftNETCoreAppVersion}' ({HostTestContext.BuildArchitecture})";
                    expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={HostTestContext.MicrosoftNETCoreAppVersion}";
                }

                CommandResult result = Command.Create(sharedTestState.App.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet.Location)
                    .MultilevelLookup(false)
                    .Execute();

                result.Should().Fail()
                    .And.HaveStdErrContaining($"https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining($"&rid={HostTestContext.BuildRID}")
                    .And.HaveStdErrContaining(expectedStdErr)
                    .And.ExitWith(expectedErrorCode);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_MissingRuntimeFramework_ErrorReportedInDialog()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            using (var invalidDotNet = TestArtifact.Create("guiMissingFramework"))
            {
                string expectedErrorCode;
                string expectedUrlQuery;
                new DotNetBuilder(invalidDotNet.Location, HostTestContext.BuiltDotNet.BinPath, null)
                    .Build();

                expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={HostTestContext.MicrosoftNETCoreAppVersion}";
                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet.Location)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                string expectedMissingFramework = $"'{Constants.MicrosoftNETCoreApp}', version '{HostTestContext.MicrosoftNETCoreAppVersion}' ({HostTestContext.BuildArchitecture})";
                var result = command.WaitForExit()
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining("&gui=true")
                    .And.HaveStdErrContaining($"&rid={HostTestContext.BuildRID}")
                    .And.HaveStdErrMatching($"details: (?>.|\\s)*{System.Text.RegularExpressions.Regex.Escape(expectedMissingFramework)}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void AppHost_GUI_MissingRuntime_ErrorReportedInDialog()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            using (var invalidDotNet = TestArtifact.Create("guiMissingRuntime"))
            {
                var command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet.Location)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                var expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x");
                var result = command.WaitForExit()
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?missing_runtime=true")
                    .And.HaveStdErrContaining("gui=true")
                    .And.HaveStdErrContaining($"&apphost_version={HostTestContext.MicrosoftNETCoreAppVersion}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_NoCustomErrorWriter_FrameworkMissing_ErrorReportedInDialog()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            // The mockhostfxrFrameworkMissingFailure folder name is used by mock hostfxr to return the appropriate error code
            using (var dotnetWithMockHostFxr = TestArtifact.Create("mockhostfxrFrameworkMissingFailure"))
            {
                var dotnet = new DotNetBuilder(dotnetWithMockHostFxr.Location, HostTestContext.BuiltDotNet.BinPath, null)
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0))
                    .Build();

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, HostTestContext.BuildArchitecture)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                string expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                command.WaitForExit()
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining("You must install or update .NET to run this application.")
                    .And.HaveStdErrContaining("App host version:")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_DisabledGUIErrors_DialogNotShown()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            using (var invalidDotNet = TestArtifact.Create("guiErrors"))
            {
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet.Location)
                    .MultilevelLookup(false)
                    .EnvironmentVariable(Constants.DisableGuiErrors.EnvironmentVariable, "1")
                    .Execute()
                    .Should().Fail()
                    .And.NotHaveStdErrContaining("Showing error dialog for application");
            }
        }

        public class SharedTestState : IDisposable
        {
            public TestApp App { get;  }
            public TestApp MockApp { get; }

            public SharedTestState()
            {
                App = TestApp.CreateFromBuiltAssets("HelloWorld");
                App.CreateAppHost();

                MockApp = TestApp.CreateEmpty(nameof(MockApp));
                File.WriteAllText(MockApp.AppDll, string.Empty);
                MockApp.CreateAppHost(copyResources: false);
            }

            public void Dispose()
            {
                App?.Dispose();
                MockApp?.Dispose();
            }
        }
    }
}
