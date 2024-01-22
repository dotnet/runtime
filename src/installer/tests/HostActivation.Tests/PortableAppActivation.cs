// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.DotNet.Cli.Build;
using Microsoft.DotNet.Cli.Build.Framework;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class PortableAppActivation : IClassFixture<PortableAppActivation.SharedTestState>
    {
        private readonly SharedTestState sharedTestState;

        public PortableAppActivation(SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Muxer_Default()
        {
            var dotnet = TestContext.BuiltDotNet;
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
            var appExe = Path.ChangeExtension(appDll, ".exe");
            File.Copy(appDll, appExe, true);
            File.Delete(appDll);

            TestContext.BuiltDotNet.Exec("exec", appExe)
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("has already been found but with a different file extension");
        }

        [Fact]
        public void Muxer_AltDirectorySeparatorChar()
        {
            var appDll = sharedTestState.App.AppDll.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            TestContext.BuiltDotNet.Exec(appDll)
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

            TestContext.BuiltDotNet.Exec("exec", "--runtimeconfig", runtimeConfig, app.AppDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void AppHost_FrameworkDependent_Succeeds()
        {
            string appExe = sharedTestState.App.AppExe;

            // Get the framework location that was built
            string builtDotnet = TestContext.BuiltDotNet.BinPath;

            // Verify running with the default working directory
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(builtDotnet, TestContext.BuildArchitecture)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);


            // Verify running from within the working directory
            Command.Create(appExe)
                .WorkingDirectory(sharedTestState.App.Location)
                .DotNetRoot(builtDotnet, TestContext.BuildArchitecture)
                .MultilevelLookup(false)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_FrameworkDependent_GlobalLocation_Succeeds(bool useRegisteredLocation)
        {
            string appExe = sharedTestState.App.AppExe;

            // Get the framework location that was built
            string builtDotnet = TestContext.BuiltDotNet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                string architecture = TestContext.BuildArchitecture;
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation(new (string, string)[] { (architecture, builtDotnet) });
                }

                // Verify running with the default working directory
                Command.Create(appExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .MultilevelLookup(false)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : builtDotnet)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion)
                    .And.NotHaveStdErr();

                // Verify running from within the working directory
                Command.Create(appExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .MultilevelLookup(false)
                    .WorkingDirectory(sharedTestState.App.Location)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : builtDotnet)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdOutContaining(TestContext.MicrosoftNETCoreAppVersion)
                    .And.NotHaveStdErr();
            }
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
                .DotNetRoot(TestContext.BuiltDotNet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ComputedTPA_NoTrailingPathSeparator()
        {
            TestContext.BuiltDotNet.Exec(sharedTestState.App.AppDll)
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
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath, TestContext.BuildArchitecture);
            }
            else
            {
                command = TestContext.BuiltDotNet.Exec(sharedTestState.MockApp.AppDll);
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
                    .DotNetRoot(TestContext.BuiltDotNet.BinPath, TestContext.BuildArchitecture);
            }
            else
            {
                command = TestContext.BuiltDotNet.Exec(app.AppDll);
            }

            command.EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The library '{Binaries.HostPolicy.FileName}' required to execute the application was not found")
                .And.HaveStdErrContaining("Failed to run as a self-contained app")
                .And.HaveStdErrContaining($"'{app.RuntimeConfigJson}' did not specify a framework");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_CLI_FrameworkDependent_MissingRuntimeFramework_ErrorReportedInStdErr(bool missingHostfxr)
        {
            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "cliErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);
                string expectedUrlQuery;
                string expectedStdErr;
                int expectedErrorCode = 0;
                if (missingHostfxr)
                {
                    expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure;
                    expectedStdErr = $"&apphost_version={TestContext.MicrosoftNETCoreAppVersion}";
                    expectedUrlQuery = "missing_runtime=true&";
                }
                else
                {
                    invalidDotNet = new DotNetBuilder(invalidDotNet, TestContext.BuiltDotNet.BinPath, "missingFramework")
                        .Build()
                        .BinPath;

                    expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure;
                    expectedStdErr = $"Framework: '{Constants.MicrosoftNETCoreApp}', " +
                        $"version '{TestContext.MicrosoftNETCoreAppVersion}' ({TestContext.BuildArchitecture})";
                    expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={TestContext.MicrosoftNETCoreAppVersion}";
                }

                CommandResult result = Command.Create(sharedTestState.App.AppExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Execute(expectedToFail: true);

                result.Should().Fail()
                    .And.HaveStdErrContaining($"https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining($"&rid={TestContext.BuildRID}")
                    .And.HaveStdErrContaining(expectedStdErr);

                // Some Unix systems will have 8 bit exit codes.
                Assert.True(result.ExitCode == expectedErrorCode || result.ExitCode == (expectedErrorCode & 0xFF));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_FrameworkDependent_MissingRuntimeFramework_ErrorReportedInDialog()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);

                string expectedErrorCode;
                string expectedUrlQuery;
                invalidDotNet = new DotNetBuilder(invalidDotNet, TestContext.BuiltDotNet.BinPath, "missingFramework")
                    .Build()
                    .BinPath;

                expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={TestContext.MicrosoftNETCoreAppVersion}";
                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                string expectedMissingFramework = $"'{Constants.MicrosoftNETCoreApp}', version '{TestContext.MicrosoftNETCoreAppVersion}' ({TestContext.BuildArchitecture})";
                var result = command.WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining("&gui=true")
                    .And.HaveStdErrContaining($"&rid={TestContext.BuildRID}")
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

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);
                var command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                var expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x");
                var result = command.WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?missing_runtime=true")
                    .And.HaveStdErrContaining("gui=true")
                    .And.HaveStdErrContaining($"&apphost_version={TestContext.MicrosoftNETCoreAppVersion}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_NoCustomErrorWriter_FrameworkMissing_ErrorReportedInDialog()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                string expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");

                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, TestContext.BuiltDotNet.BinPath, "mockhostfxrFrameworkMissingFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0));
                var dotnet = dotnetBuilder.Build();

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, TestContext.BuildArchitecture)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                command.WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining("You must install or update .NET to run this application.")
                    .And.HaveStdErrContaining("App host version:")
                    .And.HaveStdErrContaining("apphost_version=");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_FrameworkDependent_DisabledGUIErrors_DialogNotShown()
        {
            TestApp app = sharedTestState.App.Copy();
            app.CreateAppHost(isWindowsGui: true);
            string appExe = app.AppExe;

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);
                Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
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

                MockApp = new TestApp(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "portableAppActivation")), "App");
                Directory.CreateDirectory(MockApp.Location);
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
