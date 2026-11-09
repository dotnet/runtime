// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.DotNet.XUnitExtensions;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class RunTests : ProcessTestBase
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_ExitCodeIsReturned(bool useAsync, bool useMethodGroup)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            ProcessExitStatus exitStatus;
            if (useMethodGroup)
            {
                Func<ProcessStartInfo, TimeSpan?, ProcessExitStatus> run = Process.Run;
                Func<ProcessStartInfo, CancellationToken, Task<ProcessExitStatus>> runAsync = Process.RunAsync;
                exitStatus = useAsync ? await runAsync(template.StartInfo, default) : run(template.StartInfo, default);
            }
            else
            {
                exitStatus = useAsync
                    ? await Process.RunAsync(template.StartInfo, default)
                    : Process.Run(template.StartInfo, default);
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_WithFileName_ExitCodeIsReturned(bool useAsync, bool silent)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);
            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);

            ProcessExitStatus exitStatus = useAsync
                ? await Process.RunAsync(template.StartInfo.FileName, arguments, silent, default)
                : Process.Run(template.StartInfo.FileName, arguments, silent, default);

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_WithFileName_OutputIsDiscardedWhenSilent(bool useAsync, bool silent)
        {
            // Process A starts process B that writes large output to stdout and stderr.
            // When silent is true, B's output is redirected to the null device.
            // A's stdout/stderr are redirected so we can verify whether B's output leaked through.
            using Process outerTemplate = CreateProcess((string silentArg) =>
            {
                bool silentValue = bool.Parse(silentArg);

                using Process innerTemplate = CreateProcess(() =>
                {
                    Console.Write(new string('a', 100_000));
                    Console.Error.Write(new string('b', 100_000));
                    return RemoteExecutor.SuccessExitCode;
                });

                List<string>? args = Helpers.MapToArgumentList(innerTemplate.StartInfo);
                ProcessExitStatus status = Process.Run(innerTemplate.StartInfo.FileName, args, silent: silentValue);

                return status.ExitCode;
            }, silent.ToString());

            outerTemplate.StartInfo.RedirectStandardOutput = true;
            outerTemplate.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(outerTemplate.StartInfo)
                : Process.RunAndCaptureText(outerTemplate.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal(silent ? string.Empty : new string('a', 100_000), result.StandardOutput);
            Assert.Equal(silent ? string.Empty : new string('b', 100_000), result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_WithTimeout_ExitCodeIsReturned(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            ProcessExitStatus exitStatus;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                exitStatus = await Process.RunAsync(template.StartInfo, cts.Token);
            }
            else
            {
                exitStatus = Process.Run(template.StartInfo, TimeSpan.FromMinutes(1));
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, exitStatus.ExitCode);
            Assert.False(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_WithTimeoutOrCancellation_KillsLongRunningProcess(bool useAsync, bool entireProcessTree)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);

            ProcessExitStatus exitStatus;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                exitStatus = await Process.RunAsync(template.StartInfo, entireProcessTree, cts.Token);
            }
            else
            {
                exitStatus = Process.Run(template.StartInfo, TimeSpan.FromMilliseconds(100), entireProcessTree);
            }

            Assert.True(exitStatus.Canceled);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false, false)]
        [InlineData(false, true, false)]
        [InlineData(true, false, false)]
        [InlineData(true, true, false)]
        [InlineData(false, false, true)]
        [InlineData(true, false, true)]
        [InlineData(false, true, false, true)]
        [InlineData(true, true, false, true)]
        public async Task RunAsync_WithCancellation_KillsProcessTreeWhenRequested(bool captureText, bool entireProcessTree, bool useExistingOverload, bool nestedJob = false)
        {
            const bool AsyncRead = true, AsyncWrite = false;
            // Create a process tree, where each process (outer+inner) inherits the pipe handle.
            // If all of the processes get killed, the pipe will be closed and we can read EOF from the pipe immediately.
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle readPipe, out SafeFileHandle writePipe, asyncRead: AsyncRead);
            SafeFileHandle.CreateAnonymousPipe(out SafeFileHandle controlReadPipe, out SafeFileHandle controlWritePipe);

            using (readPipe)
            using (writePipe)
            using (controlReadPipe)
            using (controlWritePipe)
            {
                using Process outerTemplate = CreateProcess((pipeFd, controlAndJob) =>
                {
                    string[] controlAndJobParts = controlAndJob.Split('|');
                    string controlFd = controlAndJobParts[0];
                    using Process innerTemplate = CreateProcess((childPipeFd, childControlFd) =>
                    {
                        using FileStream pipe = new(new SafeFileHandle(nint.Parse(childPipeFd), ownsHandle: false),
                            FileAccess.Write, bufferSize: 0, isAsync: AsyncWrite);
                        using FileStream control = new(new SafeFileHandle(nint.Parse(childControlFd), ownsHandle: false),
                            FileAccess.Read, bufferSize: 0);
                        pipe.WriteByte(1);

                        if (control.ReadByte() == 2)
                        {
                            pipe.WriteByte(2);
                        }

                        return RemoteExecutor.SuccessExitCode;
                    }, pipeFd, controlFd);
                    innerTemplate.StartInfo.InheritedHandles =
                        [new SafeFileHandle(nint.Parse(pipeFd), ownsHandle: false),
                         new SafeFileHandle(nint.Parse(controlFd), ownsHandle: false)];
                    innerTemplate.StartInfo.KillOnParentExit = bool.Parse(controlAndJobParts[1]) && OperatingSystem.IsWindows();
                    innerTemplate.Start();

                    return innerTemplate.WaitForExitStatus().ExitCode;
                }, writePipe.DangerousGetHandle().ToString(), $"{controlReadPipe.DangerousGetHandle()}|{nestedJob}");

                if (captureText)
                {
                    outerTemplate.StartInfo.RedirectStandardOutput = true;
                    outerTemplate.StartInfo.RedirectStandardError = true;
                }
                outerTemplate.StartInfo.InheritedHandles = [writePipe, controlReadPipe];

                using CancellationTokenSource cts = new();

                Task runTask = useExistingOverload
                    ? captureText
                        ? Process.RunAndCaptureTextAsync(outerTemplate.StartInfo, cts.Token)
                        : Process.RunAsync(outerTemplate.StartInfo, cts.Token)
                    : captureText
                        ? Process.RunAndCaptureTextAsync(outerTemplate.StartInfo, entireProcessTree, cts.Token)
                        : Process.RunAsync(outerTemplate.StartInfo, entireProcessTree, cts.Token);

                writePipe.Close(); // close the write end of the pipe so that we can detect when the process tree is killed
                controlReadPipe.Close();
                using FileStream readStream = new(readPipe, FileAccess.Read, bufferSize: 0, isAsync: AsyncRead);
                using FileStream controlStream = new(controlWritePipe, FileAccess.Write, bufferSize: 0);

                try
                {
                    Assert.Equal(1, await readStream.ReadAsync(new byte[1], 0, 1).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                }
                finally
                {
                    cts.Cancel();
                }

                try
                {
                    if (captureText)
                    {
                        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await runTask.WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                    }
                    else
                    {
                        ProcessExitStatus status = await ((Task<ProcessExitStatus>)runTask).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS));

                        Assert.NotEqual(0, status.ExitCode);
                        Assert.NotEqual(RemoteExecutor.SuccessExitCode, status.ExitCode);
                        Assert.True(status.Canceled);
                    }

                    if (!entireProcessTree)
                    {
                        controlStream.WriteByte(2);
                        byte[] response = new byte[1];
                        Assert.Equal(1, await readStream.ReadAsync(response, 0, 1).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                        Assert.Equal(2, response[0]);
                    }

                    Assert.Equal(0, await readStream.ReadAsync(new byte[1], 0, 1).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                }
                finally
                {
                    controlStream.Dispose();
                }
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        [PlatformSpecific(TestPlatforms.Windows)]
        public async Task RunAndCaptureText_CaptureErrorAfterRootExit_KillsDescendants(bool useAsync)
        {
            string pidFile = GetTestFilePath();
            using Process template = CreateProcess(StartChildThatWritesInvalidOutputAfterParentExit, pidFile);
            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;
            template.StartInfo.StandardOutputEncoding = new UTF8Encoding(false, throwOnInvalidBytes: true);

            Task<ProcessTextOutput> runTask = useAsync
                ? Process.RunAndCaptureTextAsync(template.StartInfo, entireProcessTree: true, CancellationToken.None)
                : Task.Run(() => Process.RunAndCaptureText(template.StartInfo, Timeout.InfiniteTimeSpan, entireProcessTree: true));

            try
            {
                Assert.True(SpinWait.SpinUntil(() => File.Exists(pidFile + ".ready"), WaitInMS));
                using Process child = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile)));
                try
                {
                    File.WriteAllText(pidFile + ".continue", string.Empty);
                    await Assert.ThrowsAsync<DecoderFallbackException>(() => runTask.WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                    Assert.True(child.WaitForExit(WaitInMS));
                }
                finally
                {
                    if (!child.HasExited)
                    {
                        child.Kill();
                    }
                    Assert.True(child.WaitForExit(WaitInMS));
                }
            }
            finally
            {
                File.WriteAllText(pidFile + ".continue", string.Empty);
            }
        }

        private int StartChildThatWritesInvalidOutputAfterParentExit(string path)
        {
            using Process child = CreateProcess(static (childPath, parentId) =>
            {
                using Process parent = Process.GetProcessById(int.Parse(parentId));
                File.WriteAllText(childPath, Environment.ProcessId.ToString());
                File.WriteAllText(childPath + ".ready", string.Empty);
                parent.WaitForExit();
                // Close both capture handles so decoding runs while the child is still alive.
                using (SafeFileHandle error = new(Console.OpenStandardErrorHandle().DangerousGetHandle(), ownsHandle: true))
                {
                }
                using (SafeFileHandle output = new(Console.OpenStandardOutputHandle().DangerousGetHandle(), ownsHandle: true))
                using (FileStream stream = new(output, FileAccess.Write))
                {
                    stream.WriteByte(0xFF);
                }
                Thread.Sleep(WaitInMS * 10);
                return RemoteExecutor.SuccessExitCode;
            }, path, Environment.ProcessId.ToString());
            child.Start();
            Assert.True(SpinWait.SpinUntil(() => File.Exists(path + ".continue"), WaitInMS));
            return RemoteExecutor.SuccessExitCode;
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_EntireProcessTree_InvalidExecutable_Throws(bool useAsync, bool captureText)
        {
            ProcessStartInfo startInfo = new(GetTestFilePath())
            {
                RedirectStandardOutput = captureText,
                RedirectStandardError = captureText
            };
            using CancellationTokenSource cts = new();
            if (useAsync)
            {
                await Assert.ThrowsAsync<Win32Exception>(() => captureText
                    ? (Task)Process.RunAndCaptureTextAsync(startInfo, entireProcessTree: true, cts.Token)
                    : Process.RunAsync(startInfo, entireProcessTree: true, cts.Token));
            }
            else
            {
                Assert.Throws<Win32Exception>(() =>
                {
                    if (captureText)
                    {
                        Process.RunAndCaptureText(startInfo, TimeSpan.FromMilliseconds(WaitInMS), entireProcessTree: true);
                    }
                    else
                    {
                        Process.Run(startInfo, TimeSpan.FromMilliseconds(WaitInMS), entireProcessTree: true);
                    }
                });
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAsync_EntireProcessTree_AlreadyCanceled(bool captureText)
        {
            using Process template = CreateSleepProcess(WaitInMS * 10);
            template.StartInfo.RedirectStandardOutput = captureText;
            template.StartInfo.RedirectStandardError = captureText;
            using CancellationTokenSource cts = new();
            cts.Cancel();
            if (captureText)
            {
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    Process.RunAndCaptureTextAsync(template.StartInfo, entireProcessTree: true, cts.Token));
            }
            else
            {
                Assert.True((await Process.RunAsync(template.StartInfo, entireProcessTree: true, cts.Token)).Canceled);
            }
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task Run_EntireProcessTree_SuccessDoesNotKillDescendants(bool useAsync, bool captureText)
        {
            string pidFile = GetTestFilePath();
            using Process template = CreateProcess(StartChildAndExit, pidFile);
            template.StartInfo.RedirectStandardOutput = captureText;
            template.StartInfo.RedirectStandardError = captureText;
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(WaitInMS));
            Task runTask = captureText
                ? useAsync
                    ? Process.RunAndCaptureTextAsync(template.StartInfo, entireProcessTree: true, cts.Token)
                    : Task.Run(() => Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMilliseconds(WaitInMS), entireProcessTree: true))
                : useAsync
                    ? Process.RunAsync(template.StartInfo, entireProcessTree: true, cts.Token)
                    : Task.Run(() => Process.Run(template.StartInfo, TimeSpan.FromMilliseconds(WaitInMS), entireProcessTree: true));

            try
            {
                Assert.True(SpinWait.SpinUntil(() => File.Exists(pidFile + ".ready"), WaitInMS));
                using Process child = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile)));
                try
                {
                    File.WriteAllText(pidFile + ".continue", string.Empty);
                    await runTask.WaitAsync(TimeSpan.FromMilliseconds(WaitInMS));
                    Assert.False(child.HasExited);
                }
                finally
                {
                    if (!child.HasExited)
                    {
                        child.Kill();
                    }
                    Assert.True(child.WaitForExit(WaitInMS));
                }
            }
            finally
            {
                File.WriteAllText(pidFile + ".continue", string.Empty);
                cts.Cancel();
            }
        }

        private int StartChildAndExit(string path)
        {
            using Process child = CreateSleepProcess(WaitInMS * 10);
            using SafeFileHandle nullHandle = File.OpenNullHandle();
            child.StartInfo.StandardOutputHandle = nullHandle;
            child.StartInfo.StandardErrorHandle = nullHandle;
            child.StartInfo.InheritedHandles = [];
            child.Start();
            File.WriteAllText(path, child.Id.ToString());
            File.WriteAllText(path + ".ready", string.Empty);
            Assert.True(SpinWait.SpinUntil(() => File.Exists(path + ".continue"), WaitInMS));
            return RemoteExecutor.SuccessExitCode;
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAsync_EntireProcessTree_ConcurrentOperationsAreIsolated(bool killOnParentExit)
        {
            string firstFile = GetTestFilePath();
            string secondFile = GetTestFilePath();
            using Process first = CreateProcess(WaitForCancellation, firstFile);
            using Process second = CreateProcess(WaitForCancellation, secondFile);
            first.StartInfo.KillOnParentExit = killOnParentExit;
            second.StartInfo.KillOnParentExit = killOnParentExit;
            using CancellationTokenSource firstCts = new();
            using CancellationTokenSource secondCts = new();
            Task<ProcessExitStatus> firstTask = Process.RunAsync(first.StartInfo, entireProcessTree: true, firstCts.Token);
            Task<ProcessExitStatus> secondTask = Process.RunAsync(second.StartInfo, entireProcessTree: true, secondCts.Token);
            try
            {
                Assert.True(SpinWait.SpinUntil(() => File.Exists(firstFile) && File.Exists(secondFile), WaitInMS));
                using Process secondProcess = Process.GetProcessById(int.Parse(File.ReadAllText(secondFile)));
                firstCts.Cancel();
                Assert.True((await firstTask.WaitAsync(TimeSpan.FromMilliseconds(WaitInMS))).Canceled);
                Assert.False(secondProcess.HasExited);
                Assert.False(secondTask.IsCompleted);
            }
            finally
            {
                firstCts.Cancel();
                secondCts.Cancel();
                await Task.WhenAll(firstTask, secondTask).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS));
            }

            static int WaitForCancellation(string path)
            {
                File.WriteAllText(path + ".tmp", Environment.ProcessId.ToString());
                File.Move(path + ".tmp", path);
                Thread.Sleep(WaitInMS * 10);
                return RemoteExecutor.SuccessExitCode;
            }
        }

#if TargetsWindows
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task RunAsync_WithCancellation_ExplicitBreakawaySurvives(bool captureText, bool killOnParentExit)
        {
            string pidFile = GetTestFilePath();
            using Process template = CreateProcess(StartBreakawayChildAndWait, pidFile);
            template.StartInfo.RedirectStandardOutput = captureText;
            template.StartInfo.RedirectStandardError = captureText;
            template.StartInfo.KillOnParentExit = killOnParentExit;
            using CancellationTokenSource cts = new();
            Task runTask = captureText
                ? Process.RunAndCaptureTextAsync(template.StartInfo, entireProcessTree: true, cts.Token)
                : Process.RunAsync(template.StartInfo, entireProcessTree: true, cts.Token);
            Process? child = null;
            try
            {
                Assert.True(SpinWait.SpinUntil(() => File.Exists(pidFile + ".ready"), WaitInMS));
                child = Process.GetProcessById(int.Parse(File.ReadAllText(pidFile)));
                cts.Cancel();
                if (captureText)
                {
                    await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask.WaitAsync(TimeSpan.FromMilliseconds(WaitInMS)));
                }
                else
                {
                    Assert.True((await ((Task<ProcessExitStatus>)runTask).WaitAsync(TimeSpan.FromMilliseconds(WaitInMS))).Canceled);
                }
                Assert.False(child.HasExited);
            }
            finally
            {
                cts.Cancel();
                if (child is not null)
                {
                    if (!child.HasExited)
                    {
                        child.Kill();
                    }
                    Assert.True(child.WaitForExit(WaitInMS));
                    child.Dispose();
                }
            }
        }

        private unsafe int StartBreakawayChildAndWait(string pidFile)
        {
            using Process template = CreateSleepProcess(WaitInMS * 10);
            using Process child = WindowsProcessStartArguments.Start(template.StartInfo, static args =>
            {
                Interop.Kernel32.STARTUPINFOEX startupInfo = default;
                startupInfo.StartupInfo.cb = sizeof(Interop.Kernel32.STARTUPINFO);
                startupInfo.StartupInfo.dwFlags = Interop.Advapi32.StartupInfoOptions.STARTF_USESTDHANDLES;
                startupInfo.StartupInfo.hStdInput = args.StandardInput;
                startupInfo.StartupInfo.hStdOutput = args.StandardOutput;
                startupInfo.StartupInfo.hStdError = args.StandardError;
                Interop.Kernel32.SECURITY_ATTRIBUTES attributes = default;
                Interop.Kernel32.PROCESS_INFORMATION info = default;
                int flags = Interop.Kernel32.CREATE_BREAKAWAY_FROM_JOB;
                if (args.EnvironmentVariables is not null)
                {
                    flags |= Interop.Advapi32.StartupInfoOptions.CREATE_UNICODE_ENVIRONMENT;
                }
                if (!Interop.Kernel32.CreateProcess(null, args.Arguments, ref attributes, ref attributes,
                    true, flags, args.EnvironmentVariables, null, &startupInfo, &info))
                {
                    throw new Win32Exception();
                }
                Interop.Kernel32.CloseHandle(info.hThread);
                return info.hProcess;
            });
            File.WriteAllText(pidFile, child.Id.ToString());
            File.WriteAllText(pidFile + ".ready", string.Empty);
            Thread.Sleep(WaitInMS * 10);
            return RemoteExecutor.SuccessExitCode;
        }
#endif

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task RunAndCaptureText_CapturesOutput(bool useAsync, bool useMethodGroup)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("hello");
                Console.Error.Write("world");
                return RemoteExecutor.SuccessExitCode;
            });

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result;
            if (useMethodGroup)
            {
                Func<ProcessStartInfo, TimeSpan?, ProcessTextOutput> capture = Process.RunAndCaptureText;
                Func<ProcessStartInfo, CancellationToken, Task<ProcessTextOutput>> captureAsync = Process.RunAndCaptureTextAsync;
                result = useAsync ? await captureAsync(template.StartInfo, default) : capture(template.StartInfo, default);
            }
            else
            {
                result = useAsync
                    ? await Process.RunAndCaptureTextAsync(template.StartInfo, default)
                    : Process.RunAndCaptureText(template.StartInfo, default);
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal("hello", result.StandardOutput);
            Assert.Equal("world", result.StandardError);
            Assert.True(result.ProcessId > 0);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_WithFileName_CapturesOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("output");
                Console.Error.Write("error");
                return RemoteExecutor.SuccessExitCode;
            });

            List<string>? arguments = Helpers.MapToArgumentList(template.StartInfo);

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo.FileName, arguments, default)
                : Process.RunAndCaptureText(template.StartInfo.FileName, arguments, default);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.Equal("output", result.StandardOutput);
            Assert.Equal("error", result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_EmptyOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () => RemoteExecutor.SuccessExitCode);

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result = useAsync
                ? await Process.RunAndCaptureTextAsync(template.StartInfo)
                : Process.RunAndCaptureText(template.StartInfo);

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.Equal(string.Empty, result.StandardOutput);
            Assert.Equal(string.Empty, result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_WithTimeoutOrCancellation_CapturesOutput(bool useAsync)
        {
            using Process template = CreateProcess(static () =>
            {
                Console.Write("captured");
                Console.Error.Write("errors");
                return RemoteExecutor.SuccessExitCode;
            });

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            ProcessTextOutput result;
            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
                result = await Process.RunAndCaptureTextAsync(template.StartInfo, cts.Token);
            }
            else
            {
                result = Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMinutes(1));
            }

            Assert.Equal(RemoteExecutor.SuccessExitCode, result.ExitStatus.ExitCode);
            Assert.False(result.ExitStatus.Canceled);
            Assert.Equal("captured", result.StandardOutput);
            Assert.Equal("errors", result.StandardError);
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public async Task RunAndCaptureText_WithTimeoutOrCancellation_KillsLongRunningProcess(bool useAsync, bool entireProcessTree)
        {
            using Process template = CreateSleepProcess((int)TimeSpan.FromHours(1).TotalMilliseconds);

            template.StartInfo.RedirectStandardOutput = true;
            template.StartInfo.RedirectStandardError = true;

            if (useAsync)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
                    await Process.RunAndCaptureTextAsync(template.StartInfo, entireProcessTree, cts.Token));
            }
            else
            {
                Assert.ThrowsAny<TimeoutException>(() =>
                    Process.RunAndCaptureText(template.StartInfo, TimeSpan.FromMilliseconds(100), entireProcessTree));
            }
        }

        [Theory]
        [InlineData(false, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, false, false, true)]
        [InlineData(true, true, false, false)]
        [InlineData(true, false, true, false)]
        [InlineData(true, false, false, true)]
        public async Task Run_Redirected_ThrowsInvalidOperationException(bool useAsync, bool redirectInput, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                RedirectStandardInput = redirectInput,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError
            };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.Run(startInfo));
            }
        }

        [Theory]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, false, false)]
        public async Task RunAndCaptureText_NotRedirected_ThrowsInvalidOperationException(bool useAsync, bool redirectOutput, bool redirectError)
        {
            ProcessStartInfo startInfo = new("someprocess")
            {
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectError
            };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAndCaptureTextAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.RunAndCaptureText(startInfo));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_NullStartInfo_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("startInfo", () => Process.RunAsync((ProcessStartInfo)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.Run((ProcessStartInfo)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_NullFileName_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("fileName", () => Process.RunAsync((string)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.Run((string)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_EmptyFileName_ThrowsArgumentException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentException>("fileName", () => Process.RunAsync(string.Empty));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("fileName", () => Process.Run(string.Empty));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_NullStartInfo_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("startInfo", () => Process.RunAndCaptureTextAsync((ProcessStartInfo)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("startInfo", () => Process.RunAndCaptureText((ProcessStartInfo)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_NullFileName_ThrowsArgumentNullException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentNullException>("fileName", () => Process.RunAndCaptureTextAsync((string)null!));
            }
            else
            {
                AssertExtensions.Throws<ArgumentNullException>("fileName", () => Process.RunAndCaptureText((string)null!));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_EmptyFileName_ThrowsArgumentException(bool useAsync)
        {
            if (useAsync)
            {
                await Assert.ThrowsAsync<ArgumentException>("fileName", () => Process.RunAndCaptureTextAsync(string.Empty));
            }
            else
            {
                AssertExtensions.Throws<ArgumentException>("fileName", () => Process.RunAndCaptureText(string.Empty));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Run_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.Run(startInfo));
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task RunAndCaptureText_UseShellExecute_ThrowsInvalidOperationException(bool useAsync)
        {
            ProcessStartInfo startInfo = new("someprocess") { UseShellExecute = true };

            if (useAsync)
            {
                await Assert.ThrowsAsync<InvalidOperationException>(() => Process.RunAndCaptureTextAsync(startInfo));
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => Process.RunAndCaptureText(startInfo));
            }
        }
    }
}
