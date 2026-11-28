// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Security;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    partial class ProcessStartInfoTests : ProcessTestBase
    {
        private const string CallerOnlyEnvironmentVariable = "DOTNET_PROCESS_TEST_CALLER_ONLY_73E761B1";
        private const string CallerOnlyEnvironmentValue = "caller-only";

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

        [ConditionalFact(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))] // Nano has no "netapi32.dll", Admin rights are required
        [PlatformSpecific(TestPlatforms.Windows)]
        [OuterLoop("Requires admin privileges")]
        public void Environment_UserNameSet_ReturnsEnvironmentOfThatUser()
        {
            const string OverrideEnvironmentVariable = "DOTNET_PROCESS_TEST_USER_OVERRIDE_73E761B1";
            const string OverrideEnvironmentValue = "explicit-override";

            RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable(CallerOnlyEnvironmentVariable, CallerOnlyEnvironmentValue);
                Directory.SetCurrentDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Windows));

                using WindowsTestAccount account = new($"d{Guid.NewGuid():N}".Substring(0, 20));
                string[] domainAndUserName = account.AccountName.Split('\\');

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
                    WorkingDirectory = Environment.SystemDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    Domain = domainAndUserName[0],
                    UserName = domainAndUserName[1],
                    PasswordInClearText = account.Password
                };

                IDictionary<string, string?> environment = startInfo.Environment;
                Assert.Equal(domainAndUserName[1], environment["USERNAME"]);
                Assert.False(environment.ContainsKey(CallerOnlyEnvironmentVariable));
                environment[OverrideEnvironmentVariable] = OverrideEnvironmentValue;

                Assert.Throws<InvalidOperationException>(() => startInfo.UserName = "otherUsername");
                Assert.Throws<InvalidOperationException>(() => startInfo.Domain = "otherDomain");
                Assert.Throws<InvalidOperationException>(() => startInfo.UseCredentialsForNetworkingOnly = true);

                startInfo.ArgumentList.Add("/d");
                startInfo.ArgumentList.Add("/c");
                startInfo.ArgumentList.Add($"echo %{OverrideEnvironmentVariable}%& if defined {CallerOnlyEnvironmentVariable} (echo present) else (echo absent)& cd");

                using Process process = Process.Start(startInfo)!;
                Assert.Collection(process.ReadAllLines(TimeSpan.FromMilliseconds(WaitInMS)),
                    line =>
                    {
                        Assert.False(line.StandardError);
                        Assert.Equal(OverrideEnvironmentValue, line.Content);
                    },
                    line =>
                    {
                        Assert.False(line.StandardError);
                        Assert.Equal("absent", line.Content);
                    },
                    line =>
                    {
                        Assert.False(line.StandardError);
                        Assert.Equal(startInfo.WorkingDirectory, line.Content, ignoreCase: true);
                    });
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
            }).Dispose();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Environment_WithNetworkOnlyCredentials_IncludesCallerVariables()
        {
            RemoteExecutor.Invoke(static () =>
            {
                Environment.SetEnvironmentVariable(CallerOnlyEnvironmentVariable, CallerOnlyEnvironmentValue);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    Domain = "unused-domain",
                    UserName = "unused-user",
                    PasswordInClearText = "PLACEHOLDER",
                    UseCredentialsForNetworkingOnly = true
                };

                Assert.Equal(CallerOnlyEnvironmentValue, startInfo.Environment[CallerOnlyEnvironmentVariable]);
            }).Dispose();
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Environment_UserNameSet_InvalidCredentials_ThrowsWin32Exception()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                UserName = "UserThatDoesNotExist",
                PasswordInClearText = "password"
            };

            Assert.Throws<Win32Exception>(() => startInfo.Environment);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Environment_Accessed_UserNameChanged_ThrowsInvalidOperationException(bool modifyEnvironment)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            IDictionary<string, string?> environment = startInfo.Environment;
            if (modifyEnvironment)
            {
                environment["TestVariable"] = "value";
            }

            startInfo.Domain = string.Empty;

            Assert.Throws<InvalidOperationException>(() => startInfo.UserName = "user");
            Assert.Equal(string.Empty, startInfo.UserName);
            Assert.Equal(string.Empty, startInfo.Domain);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Environment_Accessed_WithNetworkOnlyCredentials_UserNameAndDomainCanChange(bool modifyEnvironment)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            IDictionary<string, string?> environment = startInfo.Environment;
            if (modifyEnvironment)
            {
                environment["TestVariable"] = "value";
            }

            startInfo.UseCredentialsForNetworkingOnly = true;
            startInfo.UserName = "user";
            startInfo.Domain = "domain";

            Assert.True(startInfo.UseCredentialsForNetworkingOnly);
            Assert.Equal("user", startInfo.UserName);
            Assert.Equal("domain", startInfo.Domain);
        }

        [ConditionalTheory(typeof(ProcessStartInfoTests), nameof(IsAdmin_IsNotNano_RemoteExecutorIsSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(ProcessWindowStyle.Normal)]
        [InlineData(ProcessWindowStyle.Hidden)]
        [InlineData(ProcessWindowStyle.Minimized)]
        [InlineData(ProcessWindowStyle.Maximized)]
        public void TestWindowStyle(ProcessWindowStyle windowStyle)
        {
            (bool expectUsesShowWindow, int expectedWindowFlag) = windowStyle switch
            {
                ProcessWindowStyle.Hidden => (true, 0), // SW_HIDE is 0
                ProcessWindowStyle.Minimized => (true, 2), // SW_SHOWMINIMIZED is 2
                ProcessWindowStyle.Maximized => (true, 3), // SW_SHOWMAXIMIZED is 3
                _ => (false, 0),
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
            p.StartInfo.WindowStyle = windowStyle;
            p.Start();

            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(RemoteExecutor.SuccessExitCode, p.ExitCode);
        }
    }
}
