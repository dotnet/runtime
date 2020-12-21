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
                .Execute(fExpectedToFail: true)
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

        // https://github.com/dotnet/core-setup/issues/6914
        [Fact(Skip = "The 3.0 SDK copies NuGet references to the output by default now for executable projects, so this no longer fails.")]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_Without_AdditionalProbingPath_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--runtimeconfig", runtimeConfig, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .Should().Fail();
        }

        // https://github.com/dotnet/core-setup/issues/6914
        [Fact(Skip = "The 3.0 SDK copies NuGet references to the output by default now for executable projects, so this no longer fails.")]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Local_and_RuntimeConfig_Remote_With_AdditionalProbingPath_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var runtimeConfig = MoveRuntimeConfigToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;
            var additionalProbingPath = sharedTestState.RepoDirectories.NugetPackages;

            dotnet.Exec(
                    "exec",
                    "--runtimeconfig", runtimeConfig,
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World");
        }

        // https://github.com/dotnet/core-setup/issues/6914
        [Fact(Skip = "The 3.0 SDK copies NuGet references to the output by default now for executable projects, so the additional probing path is no longer needed.")]
        public void Muxer_Activation_With_Templated_AdditionalProbingPath_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var store_path = CreateAStore(fixture);
            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            var destRuntimeDevConfig = fixture.TestProject.RuntimeDevConfigJson;
            if (File.Exists(destRuntimeDevConfig))
            {
                File.Delete(destRuntimeDevConfig);
            }

            var additionalProbingPath = store_path + "/|arch|/|tfm|";

            dotnet.Exec(
                    "exec",
                    "--additionalprobingpath", additionalProbingPath,
                    appDll)
                .EnableTracingAndCaptureOutputs()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdErrContaining($"Adding tpa entry: {Path.Combine(store_path, fixture.RepoDirProvider.BuildArchitecture, fixture.Framework)}");
        }

        [Fact]
        public void Muxer_Exec_activation_of_Build_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            var depsJson = MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
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
        public void Muxer_Exec_activation_of_Publish_Output_Portable_DLL_with_DepsJson_Remote_and_RuntimeConfig_Local_Fails()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            var depsJson = MoveDepsJsonToSubdirectory(fixture);

            var dotnet = fixture.BuiltDotnet;
            var appDll = fixture.TestProject.AppDll;

            dotnet.Exec("exec", "--depsfile", depsJson, appDll)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .Should().Fail();
        }

        [Fact]
        public void AppHost_FrameworkDependent_Succeeds()
        {
            var fixture = sharedTestState.PortableAppFixture_Published
                .Copy();

            // Since SDK doesn't support building framework dependent apphost yet, emulate that behavior
            // by creating the executable from apphost.exe
            var appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            // Verify running with the default working directory
            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .DotNetRoot(builtDotnet)
                .MultilevelLookup(false)
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);


            // Verify running from within the working directory
            Command.Create(appExe)
                .WorkingDirectory(fixture.TestProject.OutputDirectory)
                .DotNetRoot(builtDotnet)
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

            // Since SDK doesn't support building framework dependent apphost yet, emulate that behavior
            // by creating the executable from apphost.exe
            var appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);

            // Get the framework location that was built
            string builtDotnet = fixture.BuiltDotnet.BinPath;

            using (var registeredInstallLocationOverride = new RegisteredInstallLocationOverride(appExe))
            {
                string architecture = fixture.CurrentRid.Split('-')[1];
                if (useRegisteredLocation)
                {
                    registeredInstallLocationOverride.SetInstallLocation(builtDotnet, architecture);
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
                    .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);

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
                    .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
            }
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
                    .DotNetRoot(sharedTestState.BuiltDotNet.BinPath);
            }
            else
            {
                command = sharedTestState.BuiltDotNet.Exec(sharedTestState.MockApp.AppDll);
            }

            string hostPolicyName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            command.EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The library '{hostPolicyName}' required to execute the application was not found")
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
                    .DotNetRoot(sharedTestState.BuiltDotNet.BinPath);
            }
            else
            {
                command = sharedTestState.BuiltDotNet.Exec(app.AppDll);
            }

            string hostPolicyName = RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy");
            command.EnableTracingAndCaptureOutputs()
                .MultilevelLookup(false)
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining($"The library '{hostPolicyName}' required to execute the application was not found")
                .And.HaveStdErrContaining("Failed to run as a self-contained app")
                .And.HaveStdErrContaining($"'{app.RuntimeConfigJson}' did not specify a framework");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_CLI_FrameworkDependent_MissingRuntimeFramework_ErrorReportedInDialog(bool missingHostfxr)
        {
            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "cliErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);

                string expectedErrorCode;
                string expectedUrlQuery;
                string expectedUrlParameter = null;
                if (missingHostfxr)
                {
                    expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x");
                    expectedUrlQuery = "missing_runtime=true&";
                    expectedUrlParameter = $"&apphost_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                }
                else
                {
                    invalidDotNet = new DotNetBuilder(invalidDotNet, sharedTestState.RepoDirectories.BuiltDotnet, "missingFramework")
                        .Build()
                        .BinPath;
                    expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                    expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                }

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Start();

                var result = command.WaitForExit(true)
                    .Should().Fail();

                result.And.HaveStdErrContaining($"- https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}");
                if (expectedUrlParameter != null)
                {
                    result.And.HaveStdErrContaining(expectedUrlParameter);
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void AppHost_GUI_FrameworkDependent_MissingRuntimeFramework_ErrorReportedInDialog(bool missingHostfxr)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // GUI app host is only supported on Windows.
                return;
            }

            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appExe);

            string invalidDotNet = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(invalidDotNet))
            {
                Directory.CreateDirectory(invalidDotNet);

                string expectedErrorCode;
                string expectedUrlQuery;
                string expectedUrlParameter = null;
                if (missingHostfxr)
                {
                    expectedErrorCode = Constants.ErrorCode.CoreHostLibMissingFailure.ToString("x");
                    expectedUrlQuery = "missing_runtime=true&";
                    expectedUrlParameter = $"&apphost_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                }
                else
                {
                    invalidDotNet = new DotNetBuilder(invalidDotNet, sharedTestState.RepoDirectories.BuiltDotnet, "missingFramework")
                        .Build()
                        .BinPath;
                    expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");
                    expectedUrlQuery = $"framework={Constants.MicrosoftNETCoreApp}&framework_version={sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}";
                }

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(invalidDotNet)
                    .MultilevelLookup(false)
                    .Start();

                WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                var result = command.WaitForExit(true)
                    .Should().Fail();

                result.And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining($"url: 'https://aka.ms/dotnet-core-applaunch?{expectedUrlQuery}")
                    .And.HaveStdErrContaining("&gui=true");

                if (expectedUrlParameter != null)
                {
                    result.And.HaveStdErrContaining(expectedUrlParameter);
                }
            }
        }

        [Fact]
        public void AppHost_GUI_NoCustomErrorWriter_FrameworkMissing_ErrorReportedInDialog()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appExe);

            string dotnetWithMockHostFxr = SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "guiErrors"));
            using (new TestArtifact(dotnetWithMockHostFxr))
            {
                Directory.CreateDirectory(dotnetWithMockHostFxr);
                string expectedErrorCode = Constants.ErrorCode.FrameworkMissingFailure.ToString("x");

                var dotnetBuilder = new DotNetBuilder(dotnetWithMockHostFxr, sharedTestState.RepoDirectories.BuiltDotnet, "hostfxrFrameworkMissingFailure")
                    .RemoveHostFxr()
                    .AddMockHostFxr(new Version(2, 2, 0));
                var dotnet = dotnetBuilder.Build();

                Command command = Command.Create(appExe)
                    .EnableTracingAndCaptureOutputs()
                    .DotNetRoot(dotnet.BinPath)
                    .MultilevelLookup(false)
                    .Start();

                WaitForPopupFromProcess(command.Process);
                command.Process.Kill();

                command.WaitForExit(true)
                    .Should().Fail()
                    .And.HaveStdErrContaining($"Showing error dialog for application: '{Path.GetFileName(appExe)}' - error code: 0x{expectedErrorCode}")
                    .And.HaveStdErrContaining("To run this application, you need to install a newer version of .NET");
            }
        }

        [Fact]
        public void AppHost_GUI_FrameworkDependent_DisabledGUIErrors_DialogNotShown()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // GUI app host is only supported on Windows.
                return;
            }

            var fixture = sharedTestState.PortableAppFixture_Built
                .Copy();

            string appExe = fixture.TestProject.AppExe;
            File.Copy(sharedTestState.BuiltAppHost, appExe, overwrite: true);
            AppHostExtensions.BindAppHost(appExe);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appExe);

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

        private string MoveDepsJsonToSubdirectory(TestProjectFixture testProjectFixture)
        {
            var subdirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "d");
            if (!Directory.Exists(subdirectory))
            {
                Directory.CreateDirectory(subdirectory);
            }

            var destDepsJson = Path.Combine(subdirectory, Path.GetFileName(testProjectFixture.TestProject.DepsJson));

            if (File.Exists(destDepsJson))
            {
                File.Delete(destDepsJson);
            }
            File.Move(testProjectFixture.TestProject.DepsJson, destDepsJson);

            return destDepsJson;
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

        private string CreateAStore(TestProjectFixture testProjectFixture)
        {
            var storeoutputDirectory = Path.Combine(testProjectFixture.TestProject.ProjectDirectory, "store");
            if (!Directory.Exists(storeoutputDirectory))
            {
                Directory.CreateDirectory(storeoutputDirectory);
            }

            testProjectFixture.StoreProject(outputDirectory: storeoutputDirectory);

            return storeoutputDirectory;
        }

#if WINDOWS
        private delegate bool EnumThreadWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWindowsDelegate plfn, IntPtr lParam);

        private IntPtr WaitForPopupFromProcess(Process process, int timeout = 60000)
        {
            IntPtr windowHandle = IntPtr.Zero;
            int timeRemaining = timeout;
            while (timeRemaining > 0)
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    // We take the last window we find. There really should only be one at most anyways.
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => {
                            windowHandle = hWnd;
                            return true;
                        },
                        IntPtr.Zero);
                }

                if (windowHandle != IntPtr.Zero)
                {
                    break;
                }

                System.Threading.Thread.Sleep(100);
                timeRemaining -= 100;
            }

            // Do not fail if the window could be detected, sometimes the check is fragile and doesn't work.
            // Not worth the trouble trying to figure out why (only happens rarely in the CI system).
            // We will rely on product tracing in the failure case.
            return windowHandle;
        }
#else
        private IntPtr WaitForPopupFromProcess(Process process, int timeout = 60000)
        {
            throw new PlatformNotSupportedException();
        }
#endif

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture PortableAppFixture_Built { get; }
            public TestProjectFixture PortableAppFixture_Published { get; }

            public RepoDirectoriesProvider RepoDirectories { get; }
            public string BuiltAppHost { get; }
            public DotNetCli BuiltDotNet { get; }

            public TestApp MockApp { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();
                BuiltAppHost = Path.Combine(RepoDirectories.HostArtifacts, RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost"));
                BuiltDotNet = new DotNetCli(RepoDirectories.BuiltDotnet);

                PortableAppFixture_Built = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .BuildProject();

                PortableAppFixture_Published = new TestProjectFixture("PortableApp", RepoDirectories)
                    .EnsureRestored()
                    .PublishProject();

                MockApp = new TestApp(SharedFramework.CalculateUniqueTestDirectory(Path.Combine(TestArtifact.TestArtifactsPath, "portableAppActivation")), "App");
                Directory.CreateDirectory(MockApp.Location);
                File.WriteAllText(MockApp.AppDll, string.Empty);
                File.Copy(BuiltAppHost, MockApp.AppExe);
                AppHostExtensions.BindAppHost(MockApp.AppExe);
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
