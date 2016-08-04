// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace System.Tests
{
    public class Environment_Exit : RemoteExecutorTestBase
    {
        public static object[][] ExitCodeValues = new object[][]
        {
            new object[] { 0 },
            new object[] { 1 },
            new object[] { 42 },
            new object[] { -1 },
            new object[] { -45 },
            new object[] { 255 },
        };

        [Theory]
        [MemberData(nameof(ExitCodeValues))]
        public static void CheckExitCode(int expectedExitCode)
        {
            using (Process p = RemoteInvoke(s => int.Parse(s), expectedExitCode.ToString()).Process)
            {
                Assert.True(p.WaitForExit(30 * 1000));
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Assert.Equal(expectedExitCode, p.ExitCode);
                }
                else
                {
                    Assert.Equal((sbyte)expectedExitCode, (sbyte)p.ExitCode);
                }
            }
        }

#if netstandard17
        [Theory]
        [MemberData(nameof(ExitCodeValues))]
        public static void ExitCode_Roundtrips(int exitCode)
        {
            Environment.ExitCode = exitCode;
            Assert.Equal(exitCode, Environment.ExitCode);

            Environment.ExitCode = 0; // in case the test host has a void returning Main
        }
#endif //netstandard17

        [ActiveIssue("https://github.com/dotnet/coreclr/issues/6206")]
        [Theory]
        [InlineData(1)] // setting ExitCode and exiting Main
        [InlineData(2)] // setting ExitCode both from Main and from an Unloading event handler.
        [InlineData(3)] // using Exit(exitCode)
        public static void ExitCode_VoidMainAppReturnsSetValue(int mode)
        {
            int expectedExitCode = 123;

            const string AppName = "VoidMainWithExitCodeApp.exe";
            var psi = new ProcessStartInfo();
            if (File.Exists(HostRunner))
            {
                psi.FileName = HostRunner;
                psi.Arguments = $"{AppName} {expectedExitCode} {mode}";
            }
            else
            {
                psi.FileName = AppName;
                psi.Arguments = $"{expectedExitCode} {mode}";
            }

            using (Process p = Process.Start(psi))
            {
                p.WaitForExit();
                Assert.Equal(expectedExitCode, p.ExitCode);
            }
        }
    }
}
