// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Security;
using Xunit;
using Xunit.NetCore.Extensions;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        private const string s_xdg_open = "xdg-open";

        [Fact]
        private void TestWindowApisUnix()
        {
            // This tests the hardcoded implementations of these APIs on Unix.
            using (Process p = Process.GetCurrentProcess())
            {
                Assert.True(p.Responding);
                Assert.Equal(string.Empty, p.MainWindowTitle);
                Assert.False(p.CloseMainWindow());
                Assert.Throws<InvalidOperationException>(()=>p.WaitForInputIdle());
            }
        }

        [Fact]
        public void MainWindowHandle_GetUnix_ThrowsPlatformNotSupportedException()
        {
            CreateDefaultProcess();

            Assert.Equal(IntPtr.Zero, _process.MainWindowHandle);
        }

        [Fact]
        public void TestProcessOnRemoteMachineUnix()
        {
            Process currentProcess = Process.GetCurrentProcess();

            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessesByName(currentProcess.ProcessName, "127.0.0.1"));
            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessById(currentProcess.Id, "127.0.0.1"));
        }

        [Theory]
        [MemberData(nameof(MachineName_Remote_TestData))]
        public void GetProcessesByName_RemoteMachineNameUnix_ThrowsPlatformNotSupportedException(string machineName)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Assert.Throws<PlatformNotSupportedException>(() => Process.GetProcessesByName(currentProcess.ProcessName, machineName));
        }

        [Fact]
        public void TestRootGetProcessById()
        {
            Process p = Process.GetProcessById(1);
            Assert.Equal(1, p.Id);
        }

        [Fact]
        public void ProcessStart_TryExitCommandAsFileName_ThrowsWin32Exception()
        {
            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = "exit", Arguments = "42" }));
        }

        [Fact]
        public void ProcessStart_UseShellExecuteFalse_FilenameIsUrl_ThrowsWin32Exception()
        {
            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = false, FileName = "https://www.github.com/corefx" }));
        }

        [Theory, InlineData(false), InlineData(true)]
        public void ProcessStart_TryOpenFolder_ThrowsWin32Exception(bool useShellExecute)
        {
            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = useShellExecute, FileName = Environment.CurrentDirectory }));
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        // [OuterLoop("Opens program")]
        public void ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise()
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise)}");

            string[] allowedProgramsToRun = { s_xdg_open, "gnome-open", "kfmclient" };
            foreach (var program in allowedProgramsToRun)
            {
                if (IsProgramInstalled(program))
                {
                    var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen };
                    using (var px = Process.Start(startInfo))
                    {
                        Assert.NotNull(px);
                        Console.WriteLine($"{nameof(ProcessStart_UseShellExecuteTrue_OpenFile_ThrowsIfNoDefaultProgramInstalledSucceedsOtherwise)}(): {program} was used to open file on this machine. ProcessName: {px.ProcessName}");
                        Assert.Equal(program, px.ProcessName);
                        px.Kill();
                        px.WaitForExit();
                        Assert.True(px.HasExited);
                        Assert.Equal(137, px.ExitCode); // 137 means the process was killed
                    }
                    return;
                }
            }

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }));
        }

        [Theory, InlineData("nano"), InlineData("vi")]
        [PlatformSpecific(TestPlatforms.Linux)]
        // [OuterLoop("Opens program")]
        public void ProcessStart_OpenFileOnLinux_UsesSpecifiedProgram(string programToOpenWith)
        {

            if (IsProgramInstalled(programToOpenWith))
            {
                string fileToOpen = GetTestFilePath() + ".txt";
                File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_OpenFileOnLinux_UsesSpecifiedProgram)}");
                using (var px = Process.Start(programToOpenWith, fileToOpen))
                {
                    Assert.Equal(programToOpenWith, px.ProcessName);
                    px.Kill();
                    px.WaitForExit();
                    Assert.True(px.HasExited);
                    Assert.Equal(137, px.ExitCode); // 137 means the process was killed
                }
            }
            else
            {
                Console.WriteLine($"Program specified to open file with {programToOpenWith} is not installed on this machine.");
            }
        }

        [Fact, PlatformSpecific(TestPlatforms.Linux)]
        // [OuterLoop("Opens program")]
        public void ProcessStart_UseShellExecuteTrue_OpenMissingFile_XdgOpenReturnsExitCode2()
        {
            if (IsProgramInstalled(s_xdg_open))
            {
                string fileToOpen = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
                using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = fileToOpen }))
                {
                    Assert.NotNull(p);
                    Assert.Equal(s_xdg_open, p.ProcessName);
                    p.WaitForExit();
                    Assert.True(p.HasExited);
                    Assert.Equal(2, p.ExitCode);
                }
            }
            else
            {
                Console.WriteLine($"{nameof(ProcessStart_UseShellExecuteTrue_OpenMissingFile_XdgOpenReturnsExitCode2)}(): {s_xdg_open} is not installed on this machine.");
            }
        }

        [Theory, InlineData("/usr/bin/open"), InlineData("/usr/bin/nano")]
        [PlatformSpecific(TestPlatforms.OSX)]
        // [OuterLoop("Opens program")]
        public void ProcessStart_OpenFileOnOsx_UsesSpecifiedProgram(string programToOpenWith)
        {
            string fileToOpen = GetTestFilePath() + ".txt";
            File.WriteAllText(fileToOpen, $"{nameof(ProcessStart_OpenFileOnOsx_UsesSpecifiedProgram)}");
            using (var px = Process.Start(programToOpenWith, fileToOpen))
            {
                Console.WriteLine($"in OSX, {nameof(programToOpenWith)} is {programToOpenWith}, while {nameof(px.ProcessName)} is {px.ProcessName}.");
                // Assert.Equal(programToOpenWith, px.ProcessName); // on OSX, process name is dotnet for some reason
                px.Kill();
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(137, px.ExitCode); // 137 means the process was killed
            }
        }

        [Theory, InlineData("Safari"), InlineData("\"Google Chrome\"")]
        [PlatformSpecific(TestPlatforms.OSX)]
        // [OuterLoop("Opens browser")]
        public void ProcessStart_OpenUrl_UsesSpecifiedApplication(string applicationToOpenWith)
        {
            using (var px = Process.Start("/usr/bin/open", "https://github.com/dotnet/corefx -a " + applicationToOpenWith))
            {
                Assert.False(px.HasExited);
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode); // Exit Code 0 from open means success
            }
        }

        [Theory, InlineData("-a Safari"), InlineData("-a \"Google Chrome\"")]
        [PlatformSpecific(TestPlatforms.OSX)]
        // [OuterLoop("Opens browser")]
        public void ProcessStart_UseShellExecuteTrue_OpenUrl_SuccessfullyReadsArgument(string arguments)
        {
            var startInfo = new ProcessStartInfo { UseShellExecute = true, FileName = "https://github.com/dotnet/corefx", Arguments = arguments };
            using (var px = Process.Start(startInfo))
            {
                Assert.NotNull(px);
                // px.Kill(); // uncommenting this changes exit code to 137, meaning process was killed
                px.WaitForExit();
                Assert.True(px.HasExited);
                Assert.Equal(0, px.ExitCode);
            }
        }

        [Fact, PlatformSpecific(TestPlatforms.OSX)]
        // [OuterLoop("Opens program")]
        public void ProcessStart_UseShellExecuteTrue_TryOpenFileThatDoesntExist_ReturnsExitCode1()
        {
            string file = Path.Combine(Environment.CurrentDirectory, "_no_such_file.TXT");
            using (var p = Process.Start(new ProcessStartInfo { UseShellExecute = true, FileName = file }))
            {
                Assert.True(p.WaitForExit(WaitInMS));
                Assert.Equal(1, p.ExitCode); // Exit Code 1 from open means something went wrong
            }
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestPriorityClassUnix()
        {
            CreateDefaultProcess();

            ProcessPriorityClass priorityClass = _process.PriorityClass;

            _process.PriorityClass = ProcessPriorityClass.Idle;
            Assert.Equal(_process.PriorityClass, ProcessPriorityClass.Idle);

            try
            {
                _process.PriorityClass = ProcessPriorityClass.High;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.High);

                _process.PriorityClass = ProcessPriorityClass.Normal;
                Assert.Equal(_process.PriorityClass, ProcessPriorityClass.Normal);

                _process.PriorityClass = priorityClass;
            }
            catch (Win32Exception ex)
            {
                Assert.True(!PlatformDetection.IsSuperUser, $"Failed even though superuser {ex.ToString()}");
            }
        }

        [Fact]
        [Trait(XunitConstants.Category, XunitConstants.RequiresElevation)]
        public void TestBasePriorityOnUnix()
        {
            CreateDefaultProcess();

            ProcessPriorityClass originalPriority = _process.PriorityClass;
            Assert.Equal(ProcessPriorityClass.Normal, originalPriority);

            SetAndCheckBasePriority(ProcessPriorityClass.Idle, 19);

            try
            {
                SetAndCheckBasePriority(ProcessPriorityClass.Normal, 0);
                SetAndCheckBasePriority(ProcessPriorityClass.High, -11);
                _process.PriorityClass = originalPriority;
            }
            catch (Win32Exception ex)
            {
                Assert.True(!PlatformDetection.IsSuperUser, $"Failed even though superuser {ex.ToString()}");
            }
        }

        [Fact]
        public void TestStartOnUnixWithBadPermissions()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 644)); // no execute permissions

            Win32Exception e = Assert.Throws<Win32Exception>(() => Process.Start(path));
            Assert.NotEqual(0, e.NativeErrorCode);
        }

        [Fact]
        public void TestStartOnUnixWithBadFormat()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, chmod(path, 744)); // execute permissions

            using (Process p = Process.Start(path))
            {
                p.WaitForExit();
                Assert.NotEqual(0, p.ExitCode);
            }
        }

        [DllImport("libc")]
        private static extern int chmod(string path, int mode);

        /// <summary>
        /// Checks if the program is installed
        /// </summary>
        /// <param name="program"></param>
        /// <returns></returns>
        private bool IsProgramInstalled(string program)
        {
            string path;
            string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
            if (pathEnvVar != null)
            {
                var pathParser = new StringParser(pathEnvVar, ':', skipEmpty: true);
                while (pathParser.MoveNext())
                {
                    string subPath = pathParser.ExtractCurrent();
                    path = Path.Combine(subPath, program);
                    if (File.Exists(path))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
