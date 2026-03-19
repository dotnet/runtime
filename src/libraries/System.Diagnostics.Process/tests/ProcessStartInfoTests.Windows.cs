// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessStartInfoTests : ProcessTestBase
    {
        private static bool IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles
            => IsAdmin_IsNotNano_RemoteExecutorIsSupported && WindowsTestFileShare.CanShareFiles;

        [ConditionalFact(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles))] // Nano has no "netapi32.dll", Admin rights are required
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/80019", TestRuntimes.Mono)]
        public void TestUserNetworkCredentialsPropertiesOnWindows()
        {
            const string ShareName = "testForDotNet";
            const string TestFileContent = "42";
            const string UncPathEnvVar = nameof(UncPathEnvVar);

            string testFilePath = GetTestFilePath();
            File.WriteAllText(testFilePath, TestFileContent);

            using WindowsTestFileShare fileShare = new WindowsTestFileShare(ShareName, Path.GetDirectoryName(testFilePath));
            string testFileUncPath = $"\\\\{Environment.MachineName}\\{ShareName}\\{Path.GetFileName(testFilePath)}";

            using Process process = CreateProcess(() =>
            {
                try
                {
                    Assert.Equal(TestFileContent, File.ReadAllText(Environment.GetEnvironmentVariable(UncPathEnvVar)));

                    return RemoteExecutor.SuccessExitCode;
                }
                catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
                {
                    return -1;
                }
            });
            process.StartInfo.Environment[UncPathEnvVar] = testFileUncPath;
            process.StartInfo.UseCredentialsForNetworkingOnly = true;

            using TestProcessState processInfo = CreateUserAndExecute(process, Setup, Cleanup);

            Assert.Equal(Environment.UserName, Helpers.GetProcessUserName(process));

            Assert.True(process.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, process.ExitCode);

            void Setup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore) // for this particular Windows version it fails with Attempted to perform an unauthorized operation (#46619)
                {
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: true);
                }
            }

            void Cleanup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore)
                {
                    // remove the access
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: false);
                }
            }
        }

        [ConditionalTheory(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(ProcessWindowStyle.Normal, true)]
        [InlineData(ProcessWindowStyle.Normal, false)]
        [InlineData(ProcessWindowStyle.Hidden, true)]
        [InlineData(ProcessWindowStyle.Hidden, false)]
        [InlineData(ProcessWindowStyle.Minimized, true)]
        [InlineData(ProcessWindowStyle.Minimized, false)]
        [InlineData(ProcessWindowStyle.Maximized, true)]
        [InlineData(ProcessWindowStyle.Maximized, false)]
        public void TestWindowStyle(ProcessWindowStyle windowStyle, bool useShellExecute)
        {
            if (useShellExecute && PlatformDetection.IsMonoRuntime)
            {
                // https://github.com/dotnet/runtime/issues/34360
                throw new SkipTestException("ShellExecute tries to set STA COM apartment state which is not implemented by Mono.");
            }

            // "x y" where x is the expected dwFlags & 0x1 result and y is the wShowWindow value
            (bool expectUsesShowWindow, int expectedWindowFlag) = windowStyle switch
            {
                ProcessWindowStyle.Hidden => (true, 0), // SW_HIDE is 0
                ProcessWindowStyle.Minimized => (true, 2), // SW_SHOWMINIMIZED is 2
                ProcessWindowStyle.Maximized => (true, 3), // SW_SHOWMAXIMIZED is 3
                // UseShellExecute always sets the flag but no shell does not for Normal.
                _ => useShellExecute ? (true, 1) : (false, 0), // SW_SHOWNORMAL is 1
            };

            using Process p = CreateProcess((string procArg) =>
            {
                Interop.GetStartupInfoW(out Interop.STARTUPINFO si);

                string[] argSplit = procArg.Split(" ");
                bool expectUsesShowWindow = bool.Parse(argSplit[0]);
                short expectedWindowFlag = short.Parse(argSplit[1]);

                Assert.Equal(expectUsesShowWindow, (si.dwFlags & 0x1) != 0); // STARTF_USESHOWWINDOW is 0x1
                Assert.Equal(expectedWindowFlag, si.wShowWindow);
                return RemoteExecutor.SuccessExitCode;
            }, $"{expectUsesShowWindow} {expectedWindowFlag}");
            p.StartInfo.UseShellExecute  = useShellExecute;
            p.StartInfo.WindowStyle = windowStyle;
            p.Start();

            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
        }
    }
}
