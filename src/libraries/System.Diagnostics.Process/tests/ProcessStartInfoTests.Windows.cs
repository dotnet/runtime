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
            string testFilePath = GetTestFilePath();
            Assert.False(string.IsNullOrWhiteSpace(testFilePath), $"Path to test file should not be empty: {testFilePath}");

            string testFilePathRoot = Path.GetPathRoot(testFilePath);
            const string ShareName = "testForDotNet";
            using WindowsTestFileShare fileShare = new WindowsTestFileShare(ShareName, Path.GetDirectoryName(testFilePath));
            string testFileUncPath = $"\\\\{Environment.MachineName}\\{ShareName}\\{Path.GetFileName(testFilePath)}";
            string testFileContent = "42";
            File.WriteAllText(testFilePath, testFileContent);

            const string LocalPath = nameof(LocalPath);
            const string UncPath = nameof(UncPath);
            using Process p = CreateProcess(() =>
            {
                // Read file content using network credentials and output it for assertion comparison
                _ = File.ReadAllText(Environment.GetEnvironmentVariable(UncPath));
                try
                {
                    // Read file content using current user - should fail, because current user
                    // explicitly forbidden from accessing the file.
                    _ = File.ReadAllText(Environment.GetEnvironmentVariable(LocalPath));

                    return -1;
                }
                catch (Exception ex) when (ex is SecurityException or UnauthorizedAccessException)
                {
                    return RemoteExecutor.SuccessExitCode;
                }
            });
            p.StartInfo.Environment[LocalPath] = testFilePath;
            p.StartInfo.Environment[UncPath] = testFileUncPath;
            p.StartInfo.UseCredentialsForNetworkingOnly = true;

            using var processInfo = CreateUserAndExecute(p, Setup, Cleanup);

            string processUserName = Helpers.GetProcessUserName(p);
            Assert.True(p.WaitForExit(WaitInMS));

            Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
            Assert.Equal(Environment.UserName, processUserName);

            void Setup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore) // for this particular Windows version it fails with Attempted to perform an unauthorized operation (#46619)
                {
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: true, allow: true);
                    SetAccessControl(Environment.UserName, testFilePath, null, add: true, allow: false);
                }
            }

            void Cleanup(string username, string _)
            {
                if (PlatformDetection.IsNotWindowsServerCore)
                {
                    // remove the access
                    SetAccessControl(username, testFilePath, Path.GetDirectoryName(testFilePath), add: false, allow: true);
                    SetAccessControl(Environment.UserName, testFilePath, null, add: false, allow: false);
                }
            }
        }
    }
}
