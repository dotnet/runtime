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
        public void Muxer_activation_of_Build_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

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
        public void Muxer_activation_of_Build_Output_Portable_DLL_with_DepsJson_having_Assembly_with_Different_File_Extension_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;

            // Change *.dll to *.exe
            var appDll = fixture.TestProject.AppDll;
            var appExe = appDll.Replace(".dll", ".exe");
            File.Copy(appDll, appExe, true);
            File.Delete(appDll);

            dotnet.Exec("exec", appExe)
                .CaptureStdErr()
                .Execute(expectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining("has already been found but with a different file extension");
        }

        [Fact]
        public void Muxer_activation_of_Apps_with_AltDirectorySeparatorChar()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            dotnet.Exec(appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void Muxer_activation_of_Publish_Output_Portable_DLL_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

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
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void AppHost_FrameworkDependent_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost();

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            // Verify running with the default working directory
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(builtDotnet, sharedTestState.RepoDirectories.BuildArchitecture)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);


            // Verify running from within the working directory
            Command.Create(appExe)
                .WorkingDirectory(fixture.TestProject.OutputDirectory)
                .DotNetRoot(builtDotnet, sharedTestState.RepoDirectories.BuildArchitecture)
                .MultilevelLookup(false)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_FrameworkDependent_GlobalLocation_Succeeds(bool useRegisteredLocation)
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost();

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                string architecture = fixture.RepoDirProvider.BuildArchitecture;
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
                    .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion)
                    .And.NotHaveStdErr();

                // Verify running from within the working directory
                Command.Create(appExe)
                    .CaptureStdErr()
                    .CaptureStdOut()
                    .MultilevelLookup(false)
                    .WorkingDirectory(fixture.TestProject.OutputDirectory)
                    .ApplyRegisteredInstallLocationOverride(registeredInstallLocationOverride)
                    .EnvironmentVariable(Constants.TestOnlyEnvironmentVariables.DefaultInstallPath, useRegisteredLocation ? null : builtDotnet)
                    .DotNetRoot(null)
                    .Execute()
                    .Should().Pass()
                    .And.HaveStdOutContaining("Hello World")
                    .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion)
                    .And.NotHaveStdErr();
            }
        }

        [Fact]
        public void RuntimeConfig_FilePath_Breaks_MAX_PATH_Threshold()
        {
            var project = sharedTestState.PortableAppFixture_Published
                .Copy();

            var appExeName = Path.GetFileName(project.TestProject.AppExe);
            var outputDir = project.TestProject.OutputDirectory;

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
            foreach (var file in Directory.GetFiles(outputDir, "*.*", SearchOption.TopDirectoryOnly))
                File.Copy(file, Path.Combine(newDir, Path.GetFileName(file)), true);

            Command.Create(appExe)
                .DotNetRoot(project.BuiltDotnet.BinPath)
                .EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        [Fact]
        public void ComputedTPADoesntEndWithPathSeparator()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec(appDll)
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
                    .DotNetRoot(sharedTestState.BuiltDotNet.BinPath, sharedTestState.RepoDirectories.BuildArchitecture);
            }
            else
            {
                command = sharedTestState.BuiltDotNet.Exec(sharedTestState.MockApp.AppDll);
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
                    .DotNetRoot(sharedTestState.BuiltDotNet.BinPath, sharedTestState.RepoDirectories.BuildArchitecture);
            }
            else
            {
                command = sharedTestState.BuiltDotNet.Exec(app.AppDll);
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
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost();

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
                    expectedStdErr = $"&apphost_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                    expectedUrlQuery = "missing_runtime=true&";
                }
                else
                {
                    invalidDotNet = new DotNetBuilder(invalidDotNet, sharedTestState.RepoDirectories.BuiltDotnet, "missingFramework")
                        .Build()
                        .BinPath;

                    expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure;
                    expectedStdErr = $"Framework: '{Constants.MicrosoftNETCoreApp}', " +
                        $"version '{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}' ({fixture.RepoDirProvider.BuildArchitecture})";
                    expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                }

                CommandResult result = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Execute(expectedToFail: true);

                result.Should().Fail()
                    .And.HaveStdErrContaining($"https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining($"&rid={RepoDirectoriesProvider.Default.BuildRID}")
                    .And.HaveStdErrContaining(expectedStdErr);

                // Some Unix systems will have 8 bit exit codes.
                Assert.True(result.ExitCode == expectedErrorCode || result.ExitCode == (expectedErrorCode & 0xFF));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_FrameworkDependent_MissingRuntimeFramework_ErrorReportedInDialog()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost(isWindowsGui: true);

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);

                string expectedErrorCode;
                string expectedUrlQuery;
                invalidDotNet = new DotNetBuilder(invalidDotNet, sharedTestState.RepoDirectories.BuiltDotnet, "missingFramework")
                    .Build()
                    .BinPath;

                expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Start();

                WindowsUtils.WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                string expectedMissingFramework = $"'{Constants.MicrosoftNETCoreApp}', version '{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}' ({fixture.RepoDirProvider.BuildArchitecture})";
                var result = command.WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining("&gui=true")
                    .And.HaveStdErrContaining($"&rid={RepoDirectoriesProvider.Default.BuildRID}")
                    .And.HaveStdErrMatching($"details: (?>.|\\s)*{System.Text.RegularExpressions.Regex.Escape(expectedMissingFramework)}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void AppHost_GUI_MissingRuntime_ErrorReportedInDialog()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost(isWindowsGui: true);

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
                    .And.HaveStdErrContaining($"&apphost_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void AppHost_GUI_NoCustomErrorWriter_FrameworkMissing_ErrorReportedInDialog()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost(isWindowsGui: true);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                string expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");

                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, sharedTestState.RepoDirectories.BuiltDotnet, "mockhostfxrFrameworkMissingFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0));
                var dotnet = dotnetBuilder.Build();

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath, sharedTestState.RepoDirectories.BuildArchitecture)
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
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            fixture.TestProject.BuiltApp.CreateAppHost(isWindowsGui: true);

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

        private string MoveRuntimeConfigToSubdirectory(TestProjectFixture testProjectFixture)
        {
            var subdirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "r");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destRuntimeConfig = Path.Combine(subdirectory, Path.GetFileName(testProjectFixture.TestProject.RuntimeConfigJson));

            if (File.Exists(destRuntimeConfig))
            {
                File.Delete(destRuntimeConfig);
            }
            File.Move(testProjectFixture.TestProject.RuntimeConfigJson, destRuntimeConfig);

            return destRuntimeConfig;
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PortableAppFixture_Built { get; }
            public TestProjectFixture PortableAppFixture_Published { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }
            public DotNetCli BuiltDotNet { get; }

            public TestApp MockApp { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                BuiltDotNet = new DotNetCli(RepoDirectories.BuiltDotnet);

                PortableAppFixture_Built = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                PortableAppFixture_Published = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject(extraArgs: "/p:UseAppHost=true");

                MockApp = new TestApp(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "portableAppActivation")), "App");
                Directory.CreateDirectory(MockApp.Location);
                File.WriteAllText(MockApp.AppDll, string.Empty);
                MockApp.CreateAppHost(copyResources: false);
            }

            public void Dispose()
            {
                PortableAppFixture_Built.Dispose();
                PortableAppFixture_Published.Dispose();
                MockApp.Dispose();
            }
        }
    }
}
