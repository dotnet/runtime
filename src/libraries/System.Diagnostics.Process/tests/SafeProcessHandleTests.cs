// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class SafeProcessHandleTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void CanStartProcess()
        {
            Process process = CreateProcess(static () =>
            {
                Console.WriteLine("ping");

                Assert.Equal("pong", Console.ReadLine()); // this will block until we receive input

                return RemoteExecutor.SuccessExitCode;
            });

            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle outputReadPipe, out SafeFileHandle outputWritePipe);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle inputReadPipe, out SafeFileHandle inputWritePipe);

            using (outputReadPipe)
            using (outputWritePipe)
            using (inputReadPipe)
            using (inputWritePipe)
            {
                process.StartInfo.StandardInputHandle = inputReadPipe;
                process.StartInfo.StandardOutputHandle = outputWritePipe;

                using SafeProcessHandle processHandle = SafeProcessHandle.Start(process.StartInfo);
                // close the parent copies of child handles
                outputWritePipe.Close();
                inputReadPipe.Close();

                using StreamReader streamReader = new(new FileStream(outputReadPipe, FileAccess.Read, bufferSize: 1, outputReadPipe.IsAsync));
                Assert.Equal("ping", streamReader.ReadLine());

                // We can get the process by id only when it's still running,
                // so we wait with "pong" until we obtain the process instance.
                // When we introduce SafeProcessHandle.WaitForExit* APIs, it's not needed.
                using Process fetchedProcess = Process.GetProcessById(processHandle.ProcessId);

                using StreamWriter streamWriter = new(new FileStream(inputWritePipe, FileAccess.Write, bufferSize: 1, inputWritePipe.IsAsync))
                {
                    AutoFlush = true
                };

                try
                {
                    streamWriter.WriteLine("pong");
                }
                finally
                {
                    fetchedProcess.Kill();
                    fetchedProcess.WaitForExit();
                }
            }
        }

        [Fact]
        public void ProcessId_InvalidHandle_ThrowsInvalidOperationException()
        {
            using SafeProcessHandle invalidHandle = new SafeProcessHandle();
            Assert.Throws<InvalidOperationException>(() => invalidHandle.ProcessId);
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // We don't use pidfd on Unix yet
        public void CanGetProcessIdForCopyOfTheHandle()
        {
            ProcessStartInfo startInfo = OperatingSystem.IsWindows()
                ? new("cmd") { ArgumentList = { "/c", "exit 42" } }
                : new("sh") { ArgumentList = { "-c", "exit 42" } };

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo);
            Assert.NotEqual(0, processHandle.ProcessId);

            using SafeProcessHandle copy = new(processHandle.DangerousGetHandle(), ownsHandle: false);
            Assert.Equal(processHandle.ProcessId, copy.ProcessId);
        }

        [Theory]
        [InlineData(true, false, false)]
        [InlineData(false, true, false)]
        [InlineData(false, false, true)]
        public void Start_WithRedirectedStreams_ThrowsInvalidOperationException(
            bool redirectInput, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("hostname")
            {
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError,
            };

            Assert.Throws<InvalidOperationException>(() => SafeProcessHandle.Start(startInfo));
        }
    }
}
