// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Microsoft.DotNet.Cli.Build.Framework;
using Microsoft.DotNet.CoreSetup.Test;
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
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
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
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
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
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
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
            AppHostExtensions.SearchAndReplace(appExe, Encoding.UTF8.GetBytes(appDllName), Encoding.UTF8.GetBytes(relativeDllPath), true);

            Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
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
            BindAppHost(appExe);

            Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdErr()
                .CaptureStdOut()
                .Execute()
                .Should().Pass()
                .And.HaveStdOutContaining("Hello World")
                .And.HaveStdOutContaining($"Framework Version:{sharedTestState.RepoDirectories.MicrosoftNETCoreAppVersion}");
        }

        [Fact]
        public void Running_AppHost_with_GUI_Reports_Errors_In_Window()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // GUI app host is only supported on Windows.
                return;
            }

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
            UseBuiltAppHost(appExe);
            MarkAppHostAsGUI(appExe);

            Command command = Command.Create(appExe)
                .CaptureStdErr()
                .CaptureStdOut()
                .Start();

            IntPtr windowHandle = WaitForPopupFromProcess(command.Process);
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // In theory we should close the window - but it's just easier to kill the process.
            // The popup should be the last thing the process does anyway.
            command.Process.Kill();

            CommandResult result = command.WaitForExit(true);

            // There should be no output written by the process.
            Assert.Equal(string.Empty, result.StdOut);
            Assert.Equal(string.Empty, result.StdErr);

            result.Should().Fail();
        }

        [Fact]
        public void Running_AppHost_with_GUI_Reports_Errors_In_Window_and_Traces()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // GUI app host is only supported on Windows.
                return;
            }

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
            UseBuiltAppHost(appExe);
            MarkAppHostAsGUI(appExe);

            string traceFilePath = Path.Combine(Path.GetDirectoryName(appExe), "trace.log");

            Command command = Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .EnvironmentVariable("COREHOST_TRACEFILE", traceFilePath)
                .Start();

            IntPtr windowHandle = WaitForPopupFromProcess(command.Process);
            Assert.NotEqual(IntPtr.Zero, windowHandle);

            // In theory we should close the window - but it's just easier to kill the process.
            // The popup should be the last thing the process does anyway.
            command.Process.Kill();

            CommandResult result = command.WaitForExit(true);

            result.Should().Fail()
                .And.FileExists(traceFilePath)
                .And.FileContains(traceFilePath, "This executable is not bound to a managed DLL to execute.");
        }

        [Fact]
        public void Running_AppHost_with_GUI_Doesnt_Report_Errors_In_Window_When_Disabled()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // GUI app host is only supported on Windows.
                return;
            }

            var fixture = sharedTestState.StandaloneAppFixture_Published
                .Copy();

            string appExe = fixture.TestProject.AppExe;

            // Mark the apphost as GUI, but don't bind it to anything - this will cause it to fail
            UseBuiltAppHost(appExe);
            MarkAppHostAsGUI(appExe);

            Command command = Command.Create(appExe)
                .EnvironmentVariable("COREHOST_TRACE", "1")
                .CaptureStdOut()
                .CaptureStdErr()
                .EnvironmentVariable(Constants.DisableGuiErrors.EnvironmentVariable, "1")
                .Start();

            CommandResult commandResult = command.WaitForExit(fExpectedToFail: false, timeoutMilliseconds: 30000);
            if (commandResult.ExitCode == -1)
            {
                try
                {
                    // Try to kill the process - it may be up with a dialog, or have some other issue.
                    command.Process.Kill();
                }
                catch
                {
                    // Ignore exceptions, we don't know what's going on with the process.
                }

                Assert.True(false, "The process failed to exit in the alloted time, it's possible it has a dialog up which should not be there.");
            }
        }

#if WINDOWS
        private delegate bool EnumThreadWindowsDelegate(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumThreadWindows(int dwThreadId, EnumThreadWindowsDelegate plfn, IntPtr lParam);

        private IntPtr WaitForPopupFromProcess(Process process, int timeout = 30000)
        {
            IntPtr windowHandle = IntPtr.Zero;
            StringBuilder diagMessages = new StringBuilder();

            int longTimeout = timeout * 3;
            int timeRemaining = longTimeout;
            while (timeRemaining > 0)
            {
                foreach (ProcessThread thread in process.Threads)
                {
                    // Note we take the last window we find - there really should only be one at most anyway.
                    EnumThreadWindows(thread.Id,
                        (hWnd, lParam) => {
                            diagMessages.AppendLine($"Callback for a window {hWnd} on thread {thread.Id}.");
                            windowHandle = hWnd;
                            return true;
                        },
                        IntPtr.Zero);
                }

                if (windowHandle != IntPtr.Zero)
                {
                    break;
                }

                Thread.Sleep(100);
                timeRemaining -= 100;
            }

            Assert.True(
                windowHandle != IntPtr.Zero,
                $"Waited {longTimeout} milliseconds for the popup window on process {process.Id}, but none was found." +
                $"{Environment.NewLine}{diagMessages.ToString()}");

            Assert.True(
                timeRemaining > (longTimeout - timeout),
                $"Waited {longTimeout - timeRemaining} milliseconds for the popup window on process {process.Id}. " +
                $"It did show and was detected as HWND {windowHandle}, but it took too long. Consider extending the timeout period for this test.");

            return windowHandle;
        }
#else
        private IntPtr WaitForPopupFromProcess(Process process, int timeout = 5000)
        {
            throw new PlatformNotSupportedException();
        }
#endif

        private void UseBuiltAppHost(string appExe)
        {
            File.Copy(Path.Combine(sharedTestState.RepoDirectories.HostArtifacts, AppHostExeName), appExe, true);
        }

        private void BindAppHost(string appExe)
        {
            string appName = Path.GetFileNameWithoutExtension(appExe);
            string appDll = $"{appName}.dll";
            string appDir = Path.GetDirectoryName(appExe);
            string appDirHostExe = Path.Combine(appDir, AppHostExeName);

            // Make a copy of apphost first, replace hash and overwrite app.exe, rather than
            // overwrite app.exe and edit in place, because the file is opened as "write" for
            // the replacement -- the test fails with ETXTBSY (exit code: 26) in Linux when
            // executing a file opened in "write" mode.
            File.Copy(appExe, appDirHostExe, true);
            using (var sha256 = SHA256.Create())
            {
                // Replace the hash with the managed DLL name.
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes("foobar"));
                var hashStr = BitConverter.ToString(hash).Replace("-", "").ToLower();
                AppHostExtensions.SearchAndReplace(appDirHostExe, Encoding.UTF8.GetBytes(hashStr), Encoding.UTF8.GetBytes(appDll), true);
            }
            File.Copy(appDirHostExe, appExe, true);
        }

        private void MarkAppHostAsGUI(string appExe)
        {
            string appDir = Path.GetDirectoryName(appExe);
            string appDirHostExe = Path.Combine(appDir, AppHostExeName);

            File.Copy(appExe, appDirHostExe, true);
            using (var sha256 = SHA256.Create())
            {
                AppHostExtensions.SetWindowsGraphicalUserInterfaceBit(appDirHostExe);
            }
            File.Copy(appDirHostExe, appExe, true);
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
                    .EnsureRestoredForRid(buildFixture.CurrentRid, RepoDirectories.CorehostPackages)
                    .BuildProject(runtime: buildFixture.CurrentRid);

                var publishFixture = new TestProjectFixture("StandaloneApp", RepoDirectories);
                publishFixture
                    .EnsureRestoredForRid(publishFixture.CurrentRid, RepoDirectories.CorehostPackages)
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

