// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class SafeProcessHandleTests
    {
        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [InlineData(PosixSignal.SIGINT)]
        [InlineData(PosixSignal.SIGQUIT)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task Signal_TerminatesProcessInNewProcessGroup(PosixSignal signal)
        {
            ProcessStartOptions options = MapToRemoteExecutorStartOptions(
                static (signalStr) =>
                {
                    // When a process is created with CREATE_NEW_PROCESS_GROUP specified, an implicit call to SetConsoleCtrlHandler(NULL,TRUE) is made.
                    // We need to re-enable the handler inside the child process.
                    if (Enum.Parse<PosixSignal>(signalStr) is PosixSignal.SIGINT)
                    {
                        unsafe
                        {
                            if (!Interop.Kernel32.SetConsoleCtrlHandler(null, false))
                            {
                                throw new Win32Exception();
                            }
                        }
                    }

                    Console.WriteLine("ready"); // signal readiness to the parent process

                    // This will block the child until parent kills it.
                    Thread.Sleep(TimeSpan.FromHours(8));
                    return 0;
                },
                arg: signal.ToString());

            options.CreateNewProcessGroup = true;

            using IDisposable server = CreateAnonymousPipe(out SafeFileHandle outputRead, out SafeFileHandle outputWrite);

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: outputWrite, error: Console.OpenStandardErrorHandle());
            outputWrite.Dispose();

            using StreamReader reader = new(new FileStream(outputRead, FileAccess.Read));
            string? line = await reader.ReadLineAsync();
            Assert.Equal("ready", line);

            processHandle.Signal(signal);

            bool exited = processHandle.TryWaitForExit(TimeSpan.FromMilliseconds(300), out ProcessExitStatus? exitStatus);
            Assert.True(exited, "Process should have exited after signal is sent");
            Assert.NotEqual(0, exitStatus?.ExitCode);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindowsNanoServer))]
        [PlatformSpecific(TestPlatforms.Windows)]
        public void Signal_UnsupportedSignal_ThrowsPlatformNotSupportedException()
        {
            ProcessStartOptions options = CreateTenSecondSleep();
            options.CreateNewProcessGroup = true;

            using SafeProcessHandle processHandle = SafeProcessHandle.Start(options, input: null, output: null, error: null);

            try
            {
                Assert.Throws<PlatformNotSupportedException>(() => processHandle.Signal(PosixSignal.SIGTERM));
            }
            finally
            {
                processHandle.Kill();
                processHandle.WaitForExit();
            }
        }
    }
}
