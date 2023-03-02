// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessStartInfoTests : ProcessTestBase
    {
        private static bool IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles
            => IsAdmin_IsNotNano_RemoteExecutorIsSupported && WindowsTestFileShare.CanShareFiles;

        [ConditionalFact(nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported_CanShareFiles))] // Nano has no "netapi32.dll", Admin rights are required
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(ProcessWindowStyle.Normal)]
        [InlineData(ProcessWindowStyle.Hidden)]
        [InlineData(ProcessWindowStyle.Minimized)]
        [InlineData(ProcessWindowStyle.Maximized)]
        public void TestWindowStyleUseShellExecuteFalse(ProcessWindowStyle windowStyle)
        {
            // "x y" where x is the expected dwFlags & 0x1 result and y is the wShowWindow value
            string procArg = windowStyle switch
            {
                ProcessWindowStyle.Hidden => "1 0",
                ProcessWindowStyle.Minimized => "1 2",
                ProcessWindowStyle.Maximized => "1 3",
                _ => "0 0",
            };

            Process p = CreateProcess((string procArg) =>
            {
                Interop.GetStartupInfoW(out var si);

                string[] argSplit = procArg.Split(" ");
                int expectedFlags = int.Parse(argSplit[0]);
                short expectedWindow = short.Parse(argSplit[1]);

                if ((si.dwFlags & 0x00000001) == expectedFlags && si.wShowWindow == expectedWindow)
                {
                    return RemoteExecutor.SuccessExitCode;
                }
                else
                {
                    return -1;
                }
            }, procArg);
            p.StartInfo.UseShellExecute  = false;
            p.StartInfo.WindowStyle = windowStyle;
            p.Start();

            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
        }
    }
}
