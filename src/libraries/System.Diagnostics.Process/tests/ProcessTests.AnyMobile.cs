// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests : ProcessTestBase
    {
        // Simple Unix tools used in Android tests. On Android, these live under
        // /system/bin and are always present (they are provided by toybox/busybox).
        // GetProgramPath searches PATH, and Android's PATH includes /system/bin.
        private static string GetAndroidProgramPath(string program) =>
            GetProgramPath(program) ?? Path.Combine("/system/bin", program);

        private const string NonExistentPath = "/nonexistent_path_for_testing_1234567890";

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Start_SimpleCommand_ExitsSuccessfully()
        {
            string ls = GetAndroidProgramPath("ls");
            using (var process = Process.Start(ls, "/"))
            {
                Assert.NotNull(process);
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
                Assert.True(process.HasExited);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Start_WithArgumentList_ExitsSuccessfully()
        {
            string ls = GetAndroidProgramPath("ls");
            var psi = new ProcessStartInfo(ls);
            psi.ArgumentList.Add("/");
            using (var process = Process.Start(psi))
            {
                Assert.NotNull(process);
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
                Assert.True(process.HasExited);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Start_CaptureStdout_ReadsOutput()
        {
            string ls = GetAndroidProgramPath("ls");
            var psi = new ProcessStartInfo(ls, "/")
            {
                RedirectStandardOutput = true
            };
            using (var process = Process.Start(psi))
            {
                Assert.NotNull(process);
                string output = process.StandardOutput.ReadToEnd();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
                Assert.False(string.IsNullOrEmpty(output));
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Kill_TerminatesRunningProcess()
        {
            string sleep = GetAndroidProgramPath("sleep");
            using (var process = Process.Start(sleep, "600"))
            {
                Assert.NotNull(process);
                Assert.False(process.HasExited);
                Assert.True(process.Id > 0);
                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.True(process.HasExited);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Start_ExitCode_ReflectsCommandFailure()
        {
            string ls = GetAndroidProgramPath("ls");
            var psi = new ProcessStartInfo(ls, NonExistentPath)
            {
                RedirectStandardError = true // suppress error output
            };
            using (var process = Process.Start(psi))
            {
                Assert.NotNull(process);
                process.StandardError.ReadToEnd();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.NotEqual(0, process.ExitCode);
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_Id_IsValidForStartedProcess()
        {
            string sleep = GetAndroidProgramPath("sleep");
            using (var process = Process.Start(sleep, "600"))
            {
                Assert.NotNull(process);
                try
                {
                    Assert.True(process.Id > 0);
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_ProcessName_IsSetForStartedProcess()
        {
            string sleep = GetAndroidProgramPath("sleep");
            using (var process = Process.Start(sleep, "600"))
            {
                Assert.NotNull(process);
                try
                {
                    Assert.NotNull(process.ProcessName);
                    Assert.NotEmpty(process.ProcessName);
                }
                finally
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Android)]
        public void Process_HasExited_IsFalseWhileRunning_TrueAfterExit()
        {
            string sleep = GetAndroidProgramPath("sleep");
            using (var process = Process.Start(sleep, "600"))
            {
                Assert.NotNull(process);
                Assert.False(process.HasExited);
                process.Kill();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.True(process.HasExited);
            }
        }
    }
}
