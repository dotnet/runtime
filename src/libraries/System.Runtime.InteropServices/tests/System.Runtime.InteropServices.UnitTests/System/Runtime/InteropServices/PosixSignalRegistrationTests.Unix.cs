// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using System.Collections.Generic;

namespace System.Tests
{
    public partial class PosixSignalRegistrationTests
    {
        public static IEnumerable<object[]> UninstallableSignals()
        {
            yield return new object[] { (PosixSignal)9 };
        }

        public static IEnumerable<object[]> SupportedSignals()
        {
            foreach (PosixSignal value in Enum.GetValues(typeof(PosixSignal)))
                yield return new object[] { value };
        }

        public static IEnumerable<object[]> UnsupportedSignals()
        {
            if (PlatformDetection.IsMobile)
            {
                foreach (PosixSignal value in Enum.GetValues(typeof(PosixSignal)))
                    yield return new object[] { value };
            }

            yield return new object[] { 0 };
            yield return new object[] { -1000 };
            yield return new object[] { 1000 };
        }

        public static bool NotMobileAndRemoteExecutable => PlatformDetection.IsNotMobile && RemoteExecutor.IsSupported;

        [ConditionalTheory(nameof(NotMobileAndRemoteExecutable))]
        [MemberData(nameof(SupportedSignals))]
        public void SignalHandlerCalledForKnownSignals(PosixSignal s)
        {
            RemoteExecutor.Invoke(signalStr =>
            {
                PosixSignal signal = Enum.Parse<PosixSignal>(signalStr);

                using SemaphoreSlim semaphore = new(0);
                using var _ = PosixSignalRegistration.Create(signal, ctx =>
                {
                    Assert.Equal(signal, ctx.Signal);

                    // Ensure signal doesn't cause the process to terminate.
                    ctx.Cancel = true;

                    semaphore.Release();
                });

                // Use 'kill' command with signal name to validate the signal pal mapping.
                string sigArg = signalStr.StartsWith("SIG") ? signalStr.Substring(3) : signalStr;
                using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = "/bin/sh", // Use a shell because not all platforms include a 'kill' executable.
                        ArgumentList = { "-c", $"kill -s {sigArg} {Environment.ProcessId.ToString()}" }
                    });
                process.WaitForExit();
                Assert.Equal(0, process.ExitCode);

                bool entered = semaphore.Wait(SuccessTimeout);
                Assert.True(entered);
            }, s.ToString()).Dispose();
        }

        [ConditionalTheory(nameof(NotMobileAndRemoteExecutable))]
        [MemberData(nameof(PosixSignalAsRawValues))]
        public void SignalHandlerCalledForRawSignals(PosixSignal s)
        {
            RemoteExecutor.Invoke((signalStr) =>
            {
                PosixSignal signal = Enum.Parse<PosixSignal>(signalStr);

                using SemaphoreSlim semaphore = new(0);
                using var _ = PosixSignalRegistration.Create(signal, ctx =>
                {
                    Assert.Equal(signal, ctx.Signal);

                    // Ensure signal doesn't cause the process to terminate.
                    ctx.Cancel = true;

                    semaphore.Release();
                });

                kill(signal);
                bool entered = semaphore.Wait(SuccessTimeout);
                Assert.True(entered);
            }, s.ToString()).Dispose();
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        public void SignalHandlerWorksForSecondRegistration()
        {
            PosixSignal signal = PosixSignal.SIGCONT;

            for (int i = 0; i < 2; i++)
            {
                using SemaphoreSlim semaphore = new(0);
                using var _ = PosixSignalRegistration.Create(signal, ctx =>
                {
                    Assert.Equal(signal, ctx.Signal);

                    // Ensure signal doesn't cause the process to terminate.
                    ctx.Cancel = true;

                    semaphore.Release();
                });

                kill(signal);
                bool entered = semaphore.Wait(SuccessTimeout);
                Assert.True(entered);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        public void SignalHandlerNotCalledWhenDisposed()
        {
            PosixSignal signal = PosixSignal.SIGCONT;

            PosixSignalRegistration.Create(signal, ctx =>
            {
                Assert.False(true, "Signal handler was called.");
            }).Dispose();

            kill(signal);
            Thread.Sleep(100);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile))]
        public void SignalHandlerNotCalledWhenFinalized()
        {
            PosixSignal signal = PosixSignal.SIGCONT;

            CreateDanglingRegistration();
            GC.Collect();
            GC.WaitForPendingFinalizers();

            kill(signal);
            Thread.Sleep(100);

            [MethodImpl(MethodImplOptions.NoInlining)]
            void CreateDanglingRegistration()
            {
                PosixSignalRegistration.Create(signal, ctx =>
                {
                    Assert.False(true, "Signal handler was called.");
                });
            }
        }

        [ConditionalTheory(nameof(NotMobileAndRemoteExecutable))]
        [InlineData(PosixSignal.SIGINT, true, 0)]
        [InlineData(PosixSignal.SIGINT, false, 130)]
        [InlineData(PosixSignal.SIGTERM, true, 0)]
        [InlineData(PosixSignal.SIGTERM, false, 143)]
        [InlineData(PosixSignal.SIGQUIT, true, 0)]
        [InlineData(PosixSignal.SIGQUIT, false, 131)]
        public void SignalCanCancelTermination(PosixSignal signal, bool cancel, int expectedExitCode)
        {
            // Mono doesn't restore and call SIG_DFL on SIGQUIT.
            if (PlatformDetection.IsMonoRuntime && signal ==  PosixSignal.SIGQUIT && cancel == false)
            {
                expectedExitCode = 0;
            }

            RemoteExecutor.Invoke((signalStr, cancelStr, expectedStr) =>
            {
                PosixSignal signalArg = Enum.Parse<PosixSignal>(signalStr);
                bool cancelArg = bool.Parse(cancelStr);
                int expected = int.Parse(expectedStr);

                using SemaphoreSlim semaphore = new(0);
                using var _ = PosixSignalRegistration.Create(signalArg, ctx =>
                {
                    ctx.Cancel = cancelArg;

                    semaphore.Release();
                });

                kill(signalArg);

                bool entered = semaphore.Wait(SuccessTimeout);
                Assert.True(entered);

                // Give the default signal handler a chance to run.
                Thread.Sleep(expected == 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromMinutes(10));

                return 0;
            }, signal.ToString(), cancel.ToString(), expectedExitCode.ToString(),
               new RemoteInvokeOptions() { ExpectedExitCode = expectedExitCode, TimeOut = 10 * 60 * 1000 }).Dispose();
        }

        public static TheoryData<PosixSignal> PosixSignalAsRawValues
        {
            get
            {
                var data = new TheoryData<PosixSignal>();
                foreach (var value in Enum.GetValues(typeof(PosixSignal)))
                {
                    int signo = GetPlatformSignalNumber((PosixSignal)value);
                    Assert.True(signo > 0, "Expected raw signal number to be greater than 0.");
                    data.Add((PosixSignal)signo);
                }
                return data;
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int kill(int pid, int sig);

        private static void kill(PosixSignal sig)
        {
            int signo = GetPlatformSignalNumber(sig);
            Assert.NotEqual(0, signo);
            int rv = kill(Environment.ProcessId, signo);
            Assert.Equal(0, rv);
        }

        [DllImport(Interop.Libraries.SystemNative, EntryPoint = "SystemNative_GetPlatformSignalNumber")]
        private static extern int GetPlatformSignalNumber(PosixSignal signal);
    }
}
