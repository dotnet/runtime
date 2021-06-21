// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Threading;
using System.Runtime.InteropServices;

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
        [MemberData(nameof(PosixSignalValues))]
        public void CanRegisterForKnownValues(PosixSignal signal)
        {
            using var _ = PosixSignalRegistration.Create(signal, ctx => { });
        }

        [Theory]
        [MemberData(nameof(PosixSignalValues))]
        public void SignalHandlerCalledForKnownSignals(PosixSignal signal)
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

        [Theory]
        [MemberData(nameof(PosixSignalAsRawValues))]
        public void SignalHandlerCalledForRawSignals(PosixSignal signal)
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

        [DllImport("libSystem.Native", EntryPoint = "SystemNative_GetPlatformSignalNumber")]
        [SuppressGCTransition]
        private static extern int GetPlatformSignalNumber(PosixSignal signal);
    }
}
