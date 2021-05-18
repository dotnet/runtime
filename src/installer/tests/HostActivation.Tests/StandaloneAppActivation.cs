// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
using Microsoft.NET.HostModel.AppHost;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Xunit;

namespace Microsoft.DotNet.CoreSetup.Test.HostActivation
{
    public class StandaloneAppActivation : IClassFixture<StandaloneAppActivation.SharedTestState>
    {
        private readonly string AppHostExeName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("apphost");

        private SharedTestState sharedTestState;

        public StandaloneAppActivation(StandaloneAppActivation.SharedTestState fixture)
        {
            sharedTestState = fixture;
        }

        [Fact]
        public void Running_Build_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Built
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_DepsJson_and_RuntimeConfig_Local_Succeeds()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_Unbound_AppHost_Fails()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            string builtAppHost = Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, AppHostExeName);
            File.Copy(builtAppHost, appExe, true);

            int exitCode = Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .ExitCode;

            if (OperatingSystem.IsWindows())
            {
                exitCode.Should().Be(-2147450731);
            }
            else
            {
                // Some Unix flavors filter exit code to ubyte.
                (exitCode & 0xFF).Should().Be(0x95);
            }
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_By_Renaming_dotnet_exe_Fails()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            string hostExeName = RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("dotnet");
            string builtHost = Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, hostExeName);
            File.Copy(builtHost, appExe, true);

            int exitCode = Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .ExitCode;

            if (OperatingSystem.IsWindows())
            {
                exitCode.Should().Be(-2147450748);
            }
            else
            {
                // Some Unix flavors filter exit code to ubyte.
                (exitCode & 0xFF).Should().Be(0x84);
            }
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_By_Renaming_apphost_exe_Succeeds()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var renamedAppExe = fixture.TestProject.AppExe + RuntimeInformationExtensions.GetExeFileNameForCurrentPlatform("renamed");

            File.Copy(appExe, renamedAppExe, true);

            Command.Create(renamedAppExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_With_Relative_Embedded_Path_Succeeds()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;

            // Move whole directory to a subdirectory
            string currentOutDir = fixture.TestProject.OutputDirectory;
            string relativeNewPath = "..";
            relativeNewPath = Path.Combine(relativeNewPath, "newDir");
            string newOutDir = Path.Combine(currentOutDir, relativeNewPath);
            Directory.Move(currentOutDir, newOutDir);

            // Move the apphost exe back to original location
            string appExeName = Path.GetFileName(appExe);
            string sourceAppExePath = Path.Combine(newOutDir, appExeName);
            Directory.CreateDirectory(Path.GetDirectoryName(appExe));
            File.Move(sourceAppExePath, appExe);

            // Modify the apphost to include relative path
            string appDll = fixture.TestProject.AppDll;
            string appDllName = Path.GetFileName(appDll);
            string relativeDllPath = Path.Combine(relativeNewPath, appDllName);
            BinaryUtils.SearchAndReplace(appExe, Encoding.UTF8.GetBytes(appDllName), Encoding.UTF8.GetBytes(relativeDllPath));

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_With_DOTNET_ROOT_Fails()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            var appExe = fixture.TestProject.AppExe;
            var appDll = fixture.TestProject.AppDll;

            // Move whole directory to a subdirectory
            string currentOutDir = fixture.TestProject.OutputDirectory;
            string relativeNewPath = "..";
            relativeNewPath = Path.Combine(relativeNewPath, "newDir2");
            string newOutDir = Path.Combine(currentOutDir, relativeNewPath);
            Directory.Move(currentOutDir, newOutDir);

            // Move the apphost exe and app dll back to original location
            string appExeName = Path.GetFileName(appExe);
            string sourceAppExePath = Path.Combine(newOutDir, appExeName);
            Directory.CreateDirectory(Path.GetDirectoryName(appExe));
            File.Move(sourceAppExePath, appExe);

            string appDllName = Path.GetFileName(appDll);
            string sourceAppDllPath = Path.Combine(newOutDir, appDllName);
            File.Move(sourceAppDllPath, appDll);

            // This verifies a self-contained apphost cannot use DOTNET_ROOT to reference a flat
            // self-contained layout since a flat layout of the shared framework is not supported.
            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("DOTNET_ROOT", newOutDir)
                .EnvironmentVariable("DOTNET_ROOT(x86)", newOutDir)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute(fExpectedToFail: true)
                .Should().Fail()
                .And.HaveStdErrContaining($"Using environment variable DOTNET_ROOT") // use the first part avoiding "(x86)" if present
                .And.HaveStdErrContaining($"=[{Path.GetFullPath(newOutDir)}] as runtime location.") // use the last part
                .And.HaveStdErrContaining("A fatal error occurred");
        }

        [Fact]
        public void Running_Publish_Output_Standalone_EXE_with_Bound_AppHost_Succeeds()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            UseBuiltAppHost(appExe);
            AppHostExtensions.BindAppHost(appExe);

            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining(sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void Running_AppHost_with_GUI_No_Console()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
            UseBuiltAppHost(appExe);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appExe);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.HaveStdErrContaining("This executable is not bound to a managed DLL to execute.");
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // GUI app host is only supported on Windows.
        public void Running_AppHost_with_GUI_Traces()
        {
            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
            UseBuiltAppHost(appExe);
            AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appExe);

            string traceFilePath;
            Command.Create(appExe)
                .EnableHostTracingToFile(out traceFilePath)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Fail()
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, "This executable is not bound to a managed DLL to execute.")
                .And.HaveStdErrContaining("This executable is not bound to a managed DLL to execute.");

            FileUtils.DeleteFileIfPossible(traceFilePath);
        }

        private void UseBuiltAppHost(string appExe)
        {
            File.Copy(Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, AppHostExeName), appExe, true);
        }

        public class SharedTestState : IDisposable
        {
            public TestProjectFixture StandaloneAppFixture_Built { get; }
            public TestProjectFixture StandaloneAppFixture_Published { get; }
            public RepoDirectoriesProvider RepoDirectories { get; }

            public SharedTestState()
            {
                RepoDirectories = new RepoDirectoriesProvider();

                var buildFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                buildFixture
                    .EnsureRestoredForRid(buildFixture.CurrentRid)
                    .BuildProject(runtime: buildFixture.CurrentRid);

                var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid)
                    .PublishProject(runtime: publishFixture.CurrentRid);

                ReplaceTestProjectOutputHostInTestProjectFixture(buildFixture);

                StandaloneAppFixture_Built = buildFixture;
                StandaloneAppFixture_Published = publishFixture;
            }

            public void Dispose()
            {
                StandaloneAppFixture_Built.Dispose();
                StandaloneAppFixture_Published.Dispose();
            }

            /*
             * This method is needed to workaround dotnet build not placing the host from the package
             * graph in the build output.
             * https://github.com/dotnet/cli/issues/2343
             */
            private static void ReplaceTestProjectOutputHostInTestProjectFixture(TestProjectFixture testProjectFixture)
            {
                var dotnet = testProjectFixture.BuiltDotnet;

                var testProjectHostPolicy = testProjectFixture.TestProject.HostPolicyDll;
                var testProjectHostFxr = testProjectFixture.TestProject.HostFxrDll;

                if (!File.Exists(testProjectHostPolicy))
                {
                    throw new Exception("host or hostpolicy does not exist in test project output. Is this a standalone app?");
                }

                var dotnetHostPolicy = Path.Combine(dotnet.GreatestVersionSharedFxPath, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostpolicy"));
                var dotnetHostFxr = Path.Combine(dotnet.GreatestVersionHostFxrPath, RuntimeInformationExtensions.GetSharedLibraryFileNameForCurrentPlatform("hostfxr"));

                File.Copy(dotnetHostPolicy, testProjectHostPolicy, true);

                if (File.Exists(testProjectHostFxr))
                {
                    File.Copy(dotnetHostFxr, testProjectHostFxr, true);
                }
            }
        }
    }
}

