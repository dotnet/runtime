// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;

namespace System.Tests
{
    public class PosixSignalTests
    {
        private static TimeSpan Timeout => TimeSpan.FromSeconds(30);

        [Fact]
        public void HandlerNullThrows()
        {
            Assert.Throws<ArgumentNullException>(() => PosixSignalRegistration.Create(PosixSignal.SIGCONT, null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1000)]
        [InlineData(1000)]
        public void InvalidSignalValueThrows(int value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PosixSignalRegistration.Create((PosixSignal)value, ctx => { }));
        }

        [Theory]
        [InlineData((PosixSignal)9)]  // SIGKILL
        public void UninstallableSignalsThrow(PosixSignal signal)
        {
            Assert.Throws<IOException>(() => PosixSignalRegistration.Create(signal, ctx => { }));
        }

        [Theory]
        [MemberData(nameof(PosixSignalValues))]
        public void CanRegisterForKnownValues(PosixSignal signal)
        {
            using var _ = PosixSignalRegistration.Create(signal, ctx => { });
        }

        [Theory]
        [MemberData(nameof(PosixSignalValues))]
        public void SignalHandlerCalledForKnownSignals(PosixSignal signal)
        {
            if (TriggersConsoleCancelKeyPress(signal))
            {
                // skip: xunit test runner returns non zero exit code.
                return;
            }

            using SemaphoreSlim semaphore = new(0);
            using var _ = PosixSignalRegistration.Create(signal, ctx =>
            {
                Assert.Equal(signal, ctx.Signal);

                // Ensure signal doesn't cause the process to terminate.
                ctx.Cancel = true;

                semaphore.Release();
            });
            kill(signal);
            bool entered = semaphore.Wait(Timeout);
            Assert.True(entered);
        }

        [Theory]
        [MemberData(nameof(PosixSignalAsRawValues))]
        public void SignalHandlerCalledForRawSignals(PosixSignal signal)
        {
            if (TriggersConsoleCancelKeyPress(signal))
            {
                // skip: xunit test runner returns non zero exit code.
                return;
            }

            using SemaphoreSlim semaphore = new(0);
            using var _ = PosixSignalRegistration.Create(signal, ctx =>
            {
                Assert.Equal(signal, ctx.Signal);

                // Ensure signal doesn't cause the process to terminate.
                ctx.Cancel = true;

                semaphore.Release();
            });
            kill(signal);
            bool entered = semaphore.Wait(Timeout);
            Assert.True(entered);
        }

        [Fact]
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
                bool entered = semaphore.Wait(Timeout);
                Assert.True(entered);
            }
        }

        [Fact]
        public void SignalHandlerCalledNotCalledWhenDisposed()
        {
            PosixSignal signal = PosixSignal.SIGCONT;

            using var registration = PosixSignalRegistration.Create(signal, ctx =>
            {
                Assert.False(true, "Signal handler was called.");
            });
            registration.Dispose();

            kill(signal);
            Thread.Sleep(100);
        }

        [Fact]
        public void SignalHandlerCalledNotCalledWhenFinalized()
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

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(PosixSignal.SIGINT, true, 0)]
        [InlineData(PosixSignal.SIGINT, false, 130)]
        [InlineData(PosixSignal.SIGTERM, true, 0)]
        [InlineData(PosixSignal.SIGTERM, false, 143)]
        [InlineData(PosixSignal.SIGQUIT, true, 0)]
        [InlineData(PosixSignal.SIGQUIT, false, 999)] // TODO: 131
        public void SignalCanCancelTermination(PosixSignal signal, bool cancel, int expectedExitCode)
        {
            RemoteExecutor.Invoke((signalStr, cancelStr) =>
            {
                PosixSignal signalArg = Enum.Parse<PosixSignal>(signalStr);
                bool cancelArg = bool.Parse(cancelStr);

                using SemaphoreSlim semaphore = new(0);
                using var _ = PosixSignalRegistration.Create(signalArg, ctx =>
                {
                    ctx.Cancel = cancelArg;

                    semaphore.Release();
                });

                kill(signalArg);

                bool entered = semaphore.Wait(Timeout);
                Assert.True(entered);

                // Give the default signal handler a chance to run.
                Thread.Sleep(cancelArg ? TimeSpan.FromSeconds(1) : Timeout);
            }, signal.ToString(), cancel.ToString(),
               new RemoteInvokeOptions() { ExpectedExitCode = expectedExitCode }).Dispose();
        }

        public static TheoryData<PosixSignal> PosixSignalValues
        {
            get
            {
                var data = new TheoryData<PosixSignal>();
                foreach (var value in Enum.GetValues(typeof(PosixSignal)))
                {
                    data.Add((PosixSignal)value);
                }
                return data;
            }
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
        [SuppressGCTransition]
        private static extern int GetPlatformSignalNumber(PosixSignal signal);

        private static bool TriggersConsoleCancelKeyPress(PosixSignal signal)
        {
            return (signal == PosixSignal.SIGINT ||
                    signal == PosixSignal.SIGQUIT ||
                    signal == (PosixSignal)2 || // SIGINT raw
                    signal == (PosixSignal)3);  // SIGQUIT raw
        }
    }
}
