// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    [PlatformSpecific(TestPlatforms.Android | TestPlatforms.MacCatalyst)]
    public class MobileProcessTests : ProcessTestBase
    {
        private const string NonExistentPath = "/nonexistent_path_for_testing_1234567890";

        [Fact]
        public void Process_Start_InheritedIO_ExitsSuccessfully()
        {
            using (Process process = Process.Start("ls", Path.GetTempPath()))
            {
                Assert.NotNull(process);
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
                Assert.True(process.HasExited);
            }
        }

        [Fact]
        public void Process_Start_RedirectedStandardOutput_ReadsOutput()
        {
            ProcessStartInfo psi = new("ls", Path.GetTempPath())
            {
                RedirectStandardOutput = true
            };
            using (Process process = Process.Start(psi))
            {
                Assert.NotNull(process);
                string output = process.StandardOutput.ReadToEnd();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.Equal(0, process.ExitCode);
                Assert.False(string.IsNullOrEmpty(output));
            }
        }

        [Fact]
        public void Process_Kill_TerminatesRunningProcess()
        {
            using (Process process = Process.Start("sleep", "600"))
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
        public void Process_Start_ExitCode_ReflectsCommandFailure()
        {
            ProcessStartInfo psi = new("ls", NonExistentPath)
            {
                RedirectStandardError = true
            };
            using (Process process = Process.Start(psi))
            {
                Assert.NotNull(process);
                string error = process.StandardError.ReadToEnd();
                Assert.True(process.WaitForExit(WaitInMS));
                Assert.NotEqual(0, process.ExitCode);
                Assert.False(string.IsNullOrEmpty(error));
            }
        }

        [Fact]
        public void Process_ProcessName_IsSetForStartedProcess()
        {
            using (Process process = Process.Start("sleep", "600"))
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Process_Start_WithStandardHandles_CanRedirectIO(bool restrictHandles)
        {
            string errorFile = Path.GetTempFileName();
            try
            {
                SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite);

                using SafeFileHandle inputHandle = File.OpenNullHandle();
                using SafeFileHandle errorHandle = File.OpenHandle(errorFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

                ProcessStartInfo psi = new("ls", Path.GetTempPath())
                {
                    StandardInputHandle = inputHandle,
                    StandardOutputHandle = outputWrite,
                    StandardErrorHandle = errorHandle,
                    InheritedHandles = restrictHandles ? [] : null
                };

                using (outputRead)
                using (outputWrite)
                {
                    using (Process process = Process.Start(psi))
                    {
                        Assert.NotNull(process);
                        outputWrite.Close(); // close the parent copy so ReadToEnd unblocks

                        using FileStream outputStream = new(outputRead, FileAccess.Read);
                        using StreamReader outputReader = new(outputStream);
                        string output = outputReader.ReadToEnd();

                        Assert.True(process.WaitForExit(WaitInMS));
                        Assert.Equal(0, process.ExitCode);
                        Assert.False(string.IsNullOrEmpty(output));
                    }
                }
            }
            finally
            {
                if (File.Exists(errorFile))
                {
                    File.Delete(errorFile);
                }
            }
        }
    }
}
