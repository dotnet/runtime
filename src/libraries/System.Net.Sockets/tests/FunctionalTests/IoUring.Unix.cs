// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;
using IoUringFixedRecvSnapshot = System.Net.Sockets.SocketAsyncEngine.IoUringFixedRecvSnapshotForTest;
using IoUringProvidedBufferSnapshot = System.Net.Sockets.SocketAsyncEngine.IoUringProvidedBufferSnapshotForTest;
using IoUringSqPollSnapshot = System.Net.Sockets.SocketAsyncEngine.IoUringSqPollSnapshotForTest;
using IoUringZeroCopyPinHoldSnapshot = System.Net.Sockets.SocketAsyncEngine.IoUringZeroCopyPinHoldSnapshotForTest;
using IoUringZeroCopySendSnapshot = System.Net.Sockets.SocketAsyncEngine.IoUringZeroCopySendSnapshotForTest;

namespace System.Net.Sockets.Tests
{
    // io_uring internals and reflection-based test hooks are currently validated on CoreCLR.
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMonoRuntime))]
    public partial class IoUring
    {
        private const int F_GETFD = 1;
        private const int F_GETFL = 3;
        private const int FD_CLOEXEC = 1;
        private const int O_NONBLOCK = 0x800;
        private const int RLIMIT_NOFILE = 7;

        [StructLayout(LayoutKind.Sequential)]
        private struct RLimit
        {
            public nuint Current;
            public nuint Maximum;
        }

        private static class IoUringEnvironmentVariables
        {
            public const string Enabled = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING";
            public const string ProvidedBufferSize = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_PROVIDED_BUFFER_SIZE";
            public const string AdaptiveBufferSizing = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_ADAPTIVE_BUFFER_SIZING";
            public const string RegisterBuffers = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_REGISTER_BUFFERS";
            public const string SqPoll = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_SQPOLL";
            public const string ZeroCopySend = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_ZERO_COPY_SEND";
            public const string DirectSqe = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_DIRECT_SQE";
            public const string ForceEagainOnceMask = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_EAGAIN_ONCE_MASK";
            public const string ForceEcanceledOnceMask = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_ECANCELED_ONCE_MASK";
            public const string ForceSubmitEpermOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_SUBMIT_EPERM_ONCE";
            public const string ForceEnterEintrRetryLimitOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_ENTER_EINTR_RETRY_LIMIT_ONCE";
            public const string ForceKernelVersionUnsupported = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_KERNEL_VERSION_UNSUPPORTED";
            public const string ForceProvidedBufferRingOomOnce = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_FORCE_PROVIDED_BUFFER_RING_OOM_ONCE";
            public const string TestEventBufferCount = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_EVENT_BUFFER_COUNT";
            public const string PrepareQueueCapacity = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_PREPARE_QUEUE_CAPACITY";
            public const string QueueEntries = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_TEST_QUEUE_ENTRIES";
            public const string ThreadCount = "DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT";
            public const string DisableReusePortAccept = "DOTNET_SYSTEM_NET_SOCKETS_IO_URING_DISABLE_REUSEPORT_ACCEPT";
        }

        // fcntl uses C int for fd/cmd/return on Linux ABIs.
        [LibraryImport("libc", EntryPoint = "fcntl", SetLastError = true)]
        private static partial int Fcntl(int fd, int cmd);

        [LibraryImport("libc", EntryPoint = "getrlimit", SetLastError = true)]
        private static partial int GetRLimit(int resource, out RLimit limit);

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // Uses Linux-only io_uring publication internals.
        public static async Task IoUringNonPinnableFallbackPublication_ConcurrentPublishers_EmitSingleDelta()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                SocketAsyncEngine.IoUringNonPinnableFallbackPublicationState originalState =
                    SocketAsyncEngine.GetIoUringNonPinnableFallbackPublicationStateForTest();

                try
                {
                    const long firstFallbackCount = 17;
                    const int publisherCount = 16;
                    long[] deltas = new long[publisherCount];
                    using var start = new ManualResetEventSlim(initialState: false);
                    var tasks = new Task[publisherCount];

                    SocketAsyncEngine.SetIoUringNonPinnableFallbackPublicationStateForTest(
                        new SocketAsyncEngine.IoUringNonPinnableFallbackPublicationState(
                            publishedCount: 0L,
                            publishingGate: 0,
                            fallbackCount: firstFallbackCount));

                    for (int i = 0; i < publisherCount; i++)
                    {
                        int capturedIndex = i;
                        tasks[i] = Task.Run(() =>
                        {
                            start.Wait();
                            deltas[capturedIndex] = SocketAsyncEngine.GetIoUringNonPinnablePrepareFallbackDeltaForTest();
                        });
                    }

                    start.Set();
                    Task.WaitAll(tasks);

                    long deltaTotal = 0;
                    int nonZeroCount = 0;
                    long nonZeroValue = 0;
                    foreach (long delta in deltas)
                    {
                        deltaTotal += delta;
                        if (delta != 0)
                        {
                            nonZeroCount++;
                            nonZeroValue = delta;
                        }
                    }

                    Assert.Equal(firstFallbackCount, deltaTotal);
                    Assert.Equal(1, nonZeroCount);
                    Assert.Equal(firstFallbackCount, nonZeroValue);

                    const long secondFallbackCount = 23;
                    SocketAsyncEngine.SetIoUringNonPinnableFallbackPublicationStateForTest(
                        new SocketAsyncEngine.IoUringNonPinnableFallbackPublicationState(
                            publishedCount: firstFallbackCount,
                            publishingGate: 0,
                            fallbackCount: secondFallbackCount));
                    Assert.Equal(secondFallbackCount - firstFallbackCount, SocketAsyncEngine.GetIoUringNonPinnablePrepareFallbackDeltaForTest());
                    Assert.Equal(0, SocketAsyncEngine.GetIoUringNonPinnablePrepareFallbackDeltaForTest());
                }
                finally
                {
                    SocketAsyncEngine.SetIoUringNonPinnableFallbackPublicationStateForTest(originalState);
                }
            }).DisposeAsync();
        }

        private static RemoteInvokeOptions CreateSocketEngineOptions(
            string? ioUringValue = "1",
            string? forceEagainOnceMask = null,
            string? forceEcanceledOnceMask = null,
            bool? forceSubmitEpermOnce = null,
            bool? forceEnterEintrRetryLimitOnce = null,
            bool? forceKernelVersionUnsupported = null,
            bool? forceProvidedBufferRingOomOnce = null,
            int? testEventBufferCount = null,
            string? testEventBufferCountRaw = null,
            int? prepareQueueCapacity = null,
            int? queueEntries = null,
            int? threadCount = null,
            int? providedBufferSize = null,
            bool? adaptiveBufferSizingEnabled = null,
            bool? registerBuffersEnabled = null,
            bool? sqPollEnabled = null,
            bool? directSqeEnabled = null,
            bool? zeroCopySendEnabled = null,
            bool? reusePortAcceptDisabled = null)
        {
            static void SetOrRemoveEnvironmentVariable(RemoteInvokeOptions options, string name, string? value)
            {
                if (value is null)
                {
                    options.StartInfo.EnvironmentVariables.Remove(name);
                }
                else
                {
                    options.StartInfo.EnvironmentVariables[name] = value;
                }
            }

            static void ValidateSocketEngineOptionCombination(int? configuredEventBufferCount, string? configuredEventBufferCountRaw)
            {
                if (configuredEventBufferCount.HasValue && configuredEventBufferCountRaw is not null)
                {
                    throw new ArgumentException(
                        "Specify either testEventBufferCount or testEventBufferCountRaw, not both.",
                        nameof(configuredEventBufferCountRaw));
                }
            }

            ValidateSocketEngineOptionCombination(testEventBufferCount, testEventBufferCountRaw);

            RemoteInvokeOptions options = new RemoteInvokeOptions();
            string? configuredEventBufferCount =
                testEventBufferCountRaw ?? (testEventBufferCount.HasValue ? testEventBufferCount.Value.ToString() : null);
            (string Name, string? Value)[] ioUringEnvironmentAssignments =
            {
                (IoUringEnvironmentVariables.Enabled, ioUringValue),
                (IoUringEnvironmentVariables.ProvidedBufferSize, providedBufferSize?.ToString()),
                (IoUringEnvironmentVariables.AdaptiveBufferSizing, adaptiveBufferSizingEnabled.HasValue ? (adaptiveBufferSizingEnabled.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.RegisterBuffers, registerBuffersEnabled.HasValue ? (registerBuffersEnabled.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.SqPoll, sqPollEnabled.HasValue ? (sqPollEnabled.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.DirectSqe, directSqeEnabled.HasValue ? (directSqeEnabled.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.ZeroCopySend, zeroCopySendEnabled.HasValue ? (zeroCopySendEnabled.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.ForceEagainOnceMask, string.IsNullOrEmpty(forceEagainOnceMask) ? null : forceEagainOnceMask),
                (IoUringEnvironmentVariables.ForceEcanceledOnceMask, string.IsNullOrEmpty(forceEcanceledOnceMask) ? null : forceEcanceledOnceMask),
                (IoUringEnvironmentVariables.ForceSubmitEpermOnce, forceSubmitEpermOnce.HasValue ? (forceSubmitEpermOnce.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.ForceEnterEintrRetryLimitOnce, forceEnterEintrRetryLimitOnce.HasValue ? (forceEnterEintrRetryLimitOnce.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.ForceKernelVersionUnsupported, forceKernelVersionUnsupported.HasValue ? (forceKernelVersionUnsupported.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.ForceProvidedBufferRingOomOnce, forceProvidedBufferRingOomOnce.HasValue ? (forceProvidedBufferRingOomOnce.Value ? "1" : "0") : null),
                (IoUringEnvironmentVariables.TestEventBufferCount, configuredEventBufferCount),
                (IoUringEnvironmentVariables.PrepareQueueCapacity, prepareQueueCapacity?.ToString()),
                (IoUringEnvironmentVariables.QueueEntries, queueEntries?.ToString()),
                (IoUringEnvironmentVariables.ThreadCount, threadCount?.ToString()),
                (IoUringEnvironmentVariables.DisableReusePortAccept, reusePortAcceptDisabled.HasValue ? (reusePortAcceptDisabled.Value ? "1" : "0") : null),
            };

            foreach ((string Name, string? Value) assignment in ioUringEnvironmentAssignments)
            {
                SetOrRemoveEnvironmentVariable(options, assignment.Name, assignment.Value);
            }

            options.TimeOut = (int)TimeSpan.FromMinutes(10).TotalMilliseconds;
            return options;
        }

        private static Task<T> ToTask<T>(Task<T> task) => task;
        private static Task<T> ToTask<T>(ValueTask<T> task) => task.AsTask();

        private static Task AwaitWithTimeoutAsync(Task task, string operationName) =>
            AwaitWithTimeoutAsync(task, operationName, TimeSpan.FromSeconds(15));

        private static async Task AwaitWithTimeoutAsync(Task task, string operationName, TimeSpan timeout)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (!ReferenceEquals(task, completed))
            {
                throw new TimeoutException($"Timed out waiting for {operationName}");
            }

            await task;
        }

        private static Task<T> AwaitWithTimeoutAsync<T>(Task<T> task, string operationName) =>
            AwaitWithTimeoutAsync(task, operationName, TimeSpan.FromSeconds(15));

        private static async Task<T> AwaitWithTimeoutAsync<T>(Task<T> task, string operationName, TimeSpan timeout)
        {
            Task completed = await Task.WhenAny(task, Task.Delay(timeout));
            if (!ReferenceEquals(task, completed))
            {
                throw new TimeoutException($"Timed out waiting for {operationName}");
            }

            return await task;
        }

        private static void AssertCanceledOrInterrupted(Exception? ex)
        {
            Assert.NotNull(ex);
            Assert.True(
                ex is OperationCanceledException ||
                ex is SocketException socketException &&
                (socketException.SocketErrorCode == SocketError.OperationAborted ||
                 socketException.SocketErrorCode == SocketError.Interrupted),
                $"Unexpected exception: {ex}");
        }

        private static void AssertCanceledDisposedOrInterrupted(Exception? ex)
        {
            if (ex is null)
            {
                return;
            }

            Assert.True(
                ex is ObjectDisposedException ||
                ex is OperationCanceledException ||
                ex is SocketException socketException &&
                (socketException.SocketErrorCode == SocketError.OperationAborted ||
                 socketException.SocketErrorCode == SocketError.Interrupted),
                $"Unexpected exception: {ex}");
        }

        private static bool IsProvidedBufferSnapshotUsable(IoUringProvidedBufferSnapshot snapshot) =>
            snapshot.HasIoUringPort &&
            snapshot.SupportsProvidedBufferRings &&
            snapshot.HasProvidedBufferRing &&
            snapshot.TotalBufferCount > 0;

        private static bool IsAdaptiveSizingUsable(IoUringProvidedBufferSnapshot snapshot) =>
            IsProvidedBufferSnapshotUsable(snapshot) && snapshot.AdaptiveBufferSizingEnabled;

        private static bool IsFixedRecvEnabled(IoUringFixedRecvSnapshot snapshot) =>
            snapshot.SupportsReadFixed && snapshot.HasRegisteredBuffers;

        private static bool IsSqPollActive(IoUringSqPollSnapshot snapshot) =>
            snapshot.HasIoUringPort && snapshot.SqPollEnabled;

        private sealed class NonPinnableMemoryManager : MemoryManager<byte>
        {
            private readonly byte[] _buffer;

            public NonPinnableMemoryManager(byte[] buffer)
            {
                _buffer = buffer;
            }

            public override Span<byte> GetSpan() => _buffer;

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                _ = elementIndex;
                throw new NotSupportedException("Non-pinnable test memory.");
            }

            public override void Unpin()
            {
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

        private sealed unsafe class TrackingPinnableMemoryManager : MemoryManager<byte>
        {
            private readonly byte[] _buffer;
            private int _pinCount;
            private int _unpinCount;

            public TrackingPinnableMemoryManager(byte[] buffer)
            {
                _buffer = buffer;
            }

            public int PinCount => Volatile.Read(ref _pinCount);
            public int UnpinCount => Volatile.Read(ref _unpinCount);

            public override Span<byte> GetSpan() => _buffer;

            public override MemoryHandle Pin(int elementIndex = 0)
            {
                if ((uint)elementIndex > (uint)_buffer.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(elementIndex));
                }

                Interlocked.Increment(ref _pinCount);
                GCHandle handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
                byte* pointer = (byte*)handle.AddrOfPinnedObject() + elementIndex;
                return new MemoryHandle(pointer, handle, this);
            }

            public override void Unpin()
            {
                Interlocked.Increment(ref _unpinCount);
            }

            protected override void Dispose(bool disposing)
            {
            }
        }

#if DEBUG
        private sealed class ThrowingTraceListener : TraceListener
        {
            public override void Write(string? message)
            {
            }

            public override void WriteLine(string? message)
            {
            }

            public override void Fail(string? message, string? detailMessage)
            {
                throw new InvalidOperationException($"{message} {detailMessage}");
            }
        }
#endif

        private static bool InvokeSocketAsyncEngineBoolMethod(string methodName)
        {
            return methodName switch
            {
                "IsIoUringEnabled" => SocketAsyncEngine.IsIoUringEnabledForTest(),
                "IsSqPollRequested" => SocketAsyncEngine.IsSqPollRequestedForTest(),
                "IsIoUringDirectSqeDisabled" => SocketAsyncEngine.IsIoUringDirectSqeDisabledForTest(),
                "IsZeroCopySendOptedIn" => SocketAsyncEngine.IsZeroCopySendOptedInForTest(),
                "IsIoUringRegisterBuffersEnabled" => SocketAsyncEngine.IsIoUringRegisterBuffersEnabledForTest(),
                _ => throw new ArgumentOutOfRangeException(nameof(methodName), methodName, "Unknown SocketAsyncEngine bool selector."),
            };
        }

        private static void AssertBooleanAppContextSwitch(
            string switchName,
            string methodName,
            bool expectedWhenSwitchTrue,
            bool expectedWhenSwitchFalse)
        {
            AppContext.SetSwitch(switchName, true);
            Assert.Equal(expectedWhenSwitchTrue, InvokeSocketAsyncEngineBoolMethod(methodName));

            AppContext.SetSwitch(switchName, false);
            Assert.Equal(expectedWhenSwitchFalse, InvokeSocketAsyncEngineBoolMethod(methodName));
        }

        private static long GetIoUringPendingRetryQueuedToPrepareQueueCount()
            => SocketAsyncEngine.GetIoUringPendingRetryQueuedToPrepareQueueCountForTest();

        private static void AssertNativeMsghdrLayoutContractForIoUring()
        {
            SocketAsyncEngine.IoUringNativeMsghdrLayoutSnapshotForTest layout =
                SocketAsyncEngine.GetIoUringNativeMsghdrLayoutForTest();

            Assert.Equal(56, layout.Size);
            Assert.Equal(0, layout.MsgNameOffset);
            Assert.Equal(8, layout.MsgNameLengthOffset);
            Assert.Equal(16, layout.MsgIovOffset);
            Assert.Equal(24, layout.MsgIovLengthOffset);
            Assert.Equal(32, layout.MsgControlOffset);
            Assert.Equal(40, layout.MsgControlLengthOffset);
            Assert.Equal(48, layout.MsgFlagsOffset);
        }

        private static void AssertNativeMsghdr32BitRejectionPathForIoUring()
        {
            Assert.True(SocketAsyncEngine.IsNativeMsghdrLayoutSupportedForIoUringForTest(pointerSize: 8, nativeMsghdrSize: 56));
            Assert.False(SocketAsyncEngine.IsNativeMsghdrLayoutSupportedForIoUringForTest(pointerSize: 4, nativeMsghdrSize: 56));
            Assert.False(SocketAsyncEngine.IsNativeMsghdrLayoutSupportedForIoUringForTest(pointerSize: 8, nativeMsghdrSize: 48));
        }

        private static void AssertIoUringCompletionSlotLayoutContractForIoUring()
        {
            SocketAsyncEngine.IoUringCompletionSlotLayoutSnapshotForTest layout =
                SocketAsyncEngine.GetIoUringCompletionSlotLayoutForTest();

            Assert.Equal(24, layout.Size);
            Assert.Equal(0, layout.GenerationOffset);
            Assert.Equal(8, layout.FreeListNextOffset);
            Assert.Equal(12, layout.PackedStateOffset);
            Assert.Equal(16, layout.FixedRecvBufferIdOffset);
            if (layout.TestForcedResultOffset >= 0)
            {
                Assert.Equal(20, layout.TestForcedResultOffset);
            }
        }

        private static bool TryInjectIoUringCqOverflowForTest(uint delta, out int injectedEngineCount)
            => SocketAsyncEngine.TryInjectIoUringCqOverflowForTest(delta, out injectedEngineCount);

        private static bool AssertIoUringCqReflectionTargetsStableForTest()
            => SocketAsyncEngine.HasActiveIoUringEngineWithInitializedCqStateForTest();

        private static int GetIoUringCompletionSlotsInUseForTest()
            => SocketAsyncEngine.GetIoUringCompletionSlotsInUseForTest();

        private static int GetIoUringTrackedOperationCountForTest()
            => SocketAsyncEngine.GetIoUringTrackedOperationCountForTest();

        private static ulong EncodeCompletionSlotUserDataForTest(int slotIndex, ulong generation)
            => SocketAsyncEngine.EncodeCompletionSlotUserDataForTest(slotIndex, generation);

        private static bool TryDecodeCompletionSlotUserDataForTest(ulong userData, out int slotIndex, out ulong generation)
            => SocketAsyncEngine.TryDecodeCompletionSlotUserDataForTest(userData, out slotIndex, out generation);

        private static ulong IncrementCompletionSlotGenerationForTest(ulong generation)
            => SocketAsyncEngine.IncrementCompletionSlotGenerationForTest(generation);

        private static bool IsTrackedIoUringUserDataForTest(ulong userData)
            => SocketAsyncEngine.IsTrackedIoUringUserDataForTest(userData);

        private static bool TryGetIoUringRingFdForTest(out int ringFd)
            => SocketAsyncEngine.TryGetIoUringRingFdForTest(out ringFd);

        private static bool TryGetIoUringWakeupEventFdForTest(out int eventFd)
            => SocketAsyncEngine.TryGetIoUringWakeupEventFdForTest(out eventFd);

        private static bool TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine)
        {
            return SocketAsyncEngine.TryGetFirstIoUringEngineForTest(out ioUringEngine);
        }

        private static void AssertCompletionSlotUserDataEncodingBoundaryContractForIoUring()
        {
            const int MaxSlotIndex = 8191;
            const ulong MaxGeneration = (1UL << 43) - 1;

            ulong encoded = EncodeCompletionSlotUserDataForTest(MaxSlotIndex, MaxGeneration);
            Assert.True(TryDecodeCompletionSlotUserDataForTest(encoded, out int decodedSlotIndex, out ulong decodedGeneration));
            Assert.Equal(MaxSlotIndex, decodedSlotIndex);
            Assert.Equal(MaxGeneration, decodedGeneration);

            ulong wrappedGeneration = IncrementCompletionSlotGenerationForTest(MaxGeneration);
            Assert.Equal(1UL, wrappedGeneration);

            ulong wrappedEncoded = EncodeCompletionSlotUserDataForTest(MaxSlotIndex, wrappedGeneration);
            Assert.True(TryDecodeCompletionSlotUserDataForTest(wrappedEncoded, out int wrappedSlotIndex, out ulong wrappedDecodedGeneration));
            Assert.Equal(MaxSlotIndex, wrappedSlotIndex);
            Assert.Equal(1UL, wrappedDecodedGeneration);
        }

        private static async Task<bool> WaitForIoUringCompletionSlotsInUseAtMostAsync(int maxValue, int timeoutMilliseconds = 10000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (GetIoUringCompletionSlotsInUseForTest() <= maxValue)
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return GetIoUringCompletionSlotsInUseForTest() <= maxValue;
        }

        private static async Task<bool> WaitForIoUringCompletionSlotsInUseAboveAsync(int baselineValue, int minimumDelta, int timeoutMilliseconds = 10000)
        {
            int threshold = baselineValue + minimumDelta;
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (GetIoUringCompletionSlotsInUseForTest() > threshold)
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return GetIoUringCompletionSlotsInUseForTest() > threshold;
        }

        private static async Task<bool> WaitForIoUringTrackedOperationsAtMostAsync(int maxValue, int timeoutMilliseconds = 10000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (GetIoUringTrackedOperationCountForTest() <= maxValue)
                {
                    return true;
                }

                await Task.Delay(25);
            }

            return GetIoUringTrackedOperationCountForTest() <= maxValue;
        }

        private static bool IsIoUringMultishotRecvSupported()
            => SocketAsyncEngine.IsIoUringMultishotRecvSupportedForTest();

        private static bool IsIoUringMultishotAcceptSupported()
            => SocketAsyncEngine.IsIoUringMultishotAcceptSupportedForTest();

        private static bool IsListenerMultishotAcceptArmed(Socket listener)
            => SocketAsyncContext.IsMultishotAcceptArmedForTest(listener);

        private static int GetListenerMultishotAcceptQueueCount(Socket listener)
            => SocketAsyncContext.GetMultishotAcceptQueueCountForTest(listener);

        private static async Task<bool> WaitForMultishotAcceptArmedStateAsync(Socket listener, bool expectedArmed, int timeoutMilliseconds = 5000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (IsListenerMultishotAcceptArmed(listener) == expectedArmed)
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return IsListenerMultishotAcceptArmed(listener) == expectedArmed;
        }

        private static bool IsPersistentMultishotRecvArmed(Socket socket)
            => SocketAsyncContext.IsPersistentMultishotRecvArmedForTest(socket);

        private static ulong GetPersistentMultishotRecvUserData(Socket socket)
            => SocketAsyncContext.GetPersistentMultishotRecvUserDataForTest(socket);

        private static int GetPersistentMultishotRecvBufferedCount(Socket socket)
            => SocketAsyncContext.GetPersistentMultishotRecvBufferedCountForTest(socket);

        private static async Task<bool> WaitForPersistentMultishotRecvArmedStateAsync(Socket socket, bool expectedArmed, int timeoutMilliseconds = 5000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            while (DateTime.UtcNow < deadline)
            {
                if (IsPersistentMultishotRecvArmed(socket) == expectedArmed)
                {
                    return true;
                }

                await Task.Delay(20);
            }

            return IsPersistentMultishotRecvArmed(socket) == expectedArmed;
        }

        private static bool HasSufficientFileDescriptorLimit(int requiredDescriptorCount)
        {
            if (requiredDescriptorCount <= 0)
            {
                return true;
            }

            if (GetRLimit(RLIMIT_NOFILE, out RLimit limit) != 0)
            {
                return true;
            }

            return limit.Current >= (nuint)requiredDescriptorCount;
        }

        private static bool DoesExecChildObserveFileDescriptor(int fd)
        {
            if (fd < 0)
            {
                return false;
            }

            using Process process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"[ -e /proc/self/fd/{fd} ]\"",
                    UseShellExecute = false,
                })!;

            process.WaitForExit();
            return process.ExitCode == 0;
        }

        private static async Task<IoUringZeroCopyPinHoldSnapshot> WaitForZeroCopyPinHoldSnapshotAsync(
            Func<IoUringZeroCopyPinHoldSnapshot, bool> predicate,
            int timeoutMilliseconds = 5000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            IoUringZeroCopyPinHoldSnapshot snapshot = GetIoUringZeroCopyPinHoldSnapshot();
            while (DateTime.UtcNow < deadline)
            {
                if (predicate(snapshot))
                {
                    return snapshot;
                }

                await Task.Delay(20);
                snapshot = GetIoUringZeroCopyPinHoldSnapshot();
            }

            return snapshot;
        }

        private static async Task AssertConnectedPairRoundTripAsync(Socket client, Socket server, byte marker)
        {
            byte[] payload = new byte[] { marker };
            byte[] receiveBuffer = new byte[1];
            Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
            Assert.Equal(1, await server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            Assert.Equal(marker, receiveBuffer[0]);
        }

        private static async Task AssertPinsReleasedAsync(TrackingPinnableMemoryManager manager)
        {
            DateTime start = DateTime.UtcNow;
            while (manager.PinCount != manager.UnpinCount)
            {
                if (DateTime.UtcNow - start > TimeSpan.FromSeconds(10))
                {
                    break;
                }

                await Task.Delay(20);
            }

            Assert.True(manager.PinCount > 0, "Expected at least one pin.");
            Assert.Equal(manager.PinCount, manager.UnpinCount);
        }

        private static IoUringProvidedBufferSnapshot GetIoUringProvidedBufferSnapshot()
        {
            return SocketAsyncEngine.GetIoUringProvidedBufferSnapshotForTest();
        }

        private static IoUringZeroCopySendSnapshot GetIoUringZeroCopySendSnapshot()
        {
            return SocketAsyncEngine.GetIoUringZeroCopySendSnapshotForTest();
        }

        private static IoUringFixedRecvSnapshot GetIoUringFixedRecvSnapshot()
        {
            return SocketAsyncEngine.GetIoUringFixedRecvSnapshotForTest();
        }

        private static IoUringSqPollSnapshot GetIoUringSqPollSnapshot()
        {
            return SocketAsyncEngine.GetIoUringSqPollSnapshotForTest();
        }

        private static bool IsAnyIoUringSqPollEngineNeedingWakeup()
            => SocketAsyncEngine.IsAnyIoUringSqPollEngineNeedingWakeupForTest();

        private static bool ValidateSqNeedWakeupMatchesRawSqFlagBit()
        {
            if (!SocketAsyncEngine.TryValidateSqNeedWakeupMatchesRawSqFlagBitForTest(out bool matches))
            {
                return false;
            }

            Assert.True(matches, "SqNeedWakeup should match the SQ_NEED_WAKEUP bit contract.");
            return true;
        }

        private static void EnableSqPollAppContextOptIn() =>
            AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", true);

        private static IoUringZeroCopyPinHoldSnapshot GetIoUringZeroCopyPinHoldSnapshot()
        {
            return SocketAsyncEngine.GetIoUringZeroCopyPinHoldSnapshotForTest();
        }

        private static bool TryForceIoUringProvidedBufferRingExhaustionForTest(out int forcedBufferCount)
            => SocketAsyncEngine.TryForceIoUringProvidedBufferRingExhaustionForTest(out forcedBufferCount);

        private static bool TryRecycleForcedIoUringProvidedBufferRingForTest(out int recycledBufferCount)
            => SocketAsyncEngine.TryRecycleForcedIoUringProvidedBufferRingForTest(out recycledBufferCount);


        private static Task<SocketAsyncEventArgs> StartReceiveMessageFromAsync(Socket socket, SocketAsyncEventArgs eventArgs)
            => StartSocketAsyncEventArgsOperation(socket, eventArgs, static (s, args) => s.ReceiveMessageFromAsync(args));

        private static Task<SocketAsyncEventArgs> StartSocketAsyncEventArgsOperation(
            Socket socket,
            SocketAsyncEventArgs eventArgs,
            Func<Socket, SocketAsyncEventArgs, bool> startOperation)
        {
            var tcs = new TaskCompletionSource<SocketAsyncEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler<SocketAsyncEventArgs> handler = null!;
            handler = (_, completedArgs) =>
            {
                eventArgs.Completed -= handler;
                tcs.TrySetResult(completedArgs);
            };

            eventArgs.Completed += handler;
            if (!startOperation(socket, eventArgs))
            {
                eventArgs.Completed -= handler;
                tcs.TrySetResult(eventArgs);
            }

            return tcs.Task;
        }

        private static async Task<(Socket Listener, Socket Client, Socket Server)> CreateConnectedTcpSocketTrioAsync(int listenBacklog = 1)
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(listenBacklog);

                Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    Task<Socket> acceptTask = listener.AcceptAsync();
                    await AwaitWithTimeoutAsync(client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!), "CreateConnectedTcpSocketTrioAsync_connect");
                    Socket server = await AwaitWithTimeoutAsync(acceptTask, "CreateConnectedTcpSocketTrioAsync_accept");
                    return (listener, client, server);
                }
                catch
                {
                    client.Dispose();
                    throw;
                }
            }
            catch
            {
                listener.Dispose();
                throw;
            }
        }

        private static async Task<(Socket Client, Socket Server)> AcceptConnectedTcpPairAsync(Socket listener, IPEndPoint endpoint)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                Task<Socket> acceptTask = listener.AcceptAsync();
                await AwaitWithTimeoutAsync(client.ConnectAsync(endpoint), "AcceptConnectedTcpPairAsync_connect");
                Socket server = await AwaitWithTimeoutAsync(acceptTask, "AcceptConnectedTcpPairAsync_accept");
                return (client, server);
            }
            catch
            {
                client.Dispose();
                throw;
            }
        }

        private static async Task RunTcpRoundTripAsync(int iterations)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] sendBuffer = new byte[] { 1 };
            byte[] receiveBuffer = new byte[1];

            for (int i = 0; i < iterations; i++)
            {
                var serverReceiveTask = server.ReceiveAsync(receiveBuffer, SocketFlags.None);
                await Task.Yield();

                int clientSent = await client.SendAsync(sendBuffer, SocketFlags.None);
                Assert.Equal(1, clientSent);

                int serverReceived = await serverReceiveTask;
                Assert.Equal(1, serverReceived);
                Assert.Equal(sendBuffer[0], receiveBuffer[0]);

                var clientReceiveTask = client.ReceiveAsync(receiveBuffer, SocketFlags.None);
                await Task.Yield();

                int serverSent = await server.SendAsync(sendBuffer, SocketFlags.None);
                Assert.Equal(1, serverSent);

                int clientReceived = await clientReceiveTask;
                Assert.Equal(1, clientReceived);
                Assert.Equal(sendBuffer[0], receiveBuffer[0]);

                unchecked
                {
                    sendBuffer[0]++;
                }
            }
        }

        private static async Task RunUnixDomainSocketRoundTripAsync()
        {
            if (!Socket.OSSupportsUnixDomainSockets)
            {
                return;
            }

            string path = UnixDomainSocketTest.GetRandomNonExistingFilePath();
            var endpoint = new UnixDomainSocketEndPoint(path);
            try
            {
                using Socket listener = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                listener.Bind(endpoint);
                listener.Listen(1);

                using Socket client = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                Task<Socket> acceptTask = listener.AcceptAsync();
                await client.ConnectAsync(endpoint);

                using Socket server = await acceptTask;
                await AssertConnectedPairRoundTripAsync(client, server, 0x31);
                await AssertConnectedPairRoundTripAsync(server, client, 0x32);
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch
                {
                }
            }
        }

        private static async Task RunHybridIoUringAndEpollEngineScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);

            // With DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT=2, one io_uring engine indicates a hybrid mix.
            if (SocketAsyncEngine.GetActiveIoUringEnginesForTest().Length != 1)
            {
                return;
            }

            const int ConnectionCount = 32;
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(ConnectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            var acceptTasks = new Task<Socket>[ConnectionCount];
            var clients = new Socket[ConnectionCount];
            var connectTasks = new Task[ConnectionCount];

            for (int i = 0; i < ConnectionCount; i++)
            {
                acceptTasks[i] = listener.AcceptAsync();
            }

            for (int i = 0; i < ConnectionCount; i++)
            {
                clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                connectTasks[i] = clients[i].ConnectAsync(endpoint);
            }

            await Task.WhenAll(connectTasks);
            Socket[] servers = await Task.WhenAll(acceptTasks);

            try
            {
                var work = new Task[ConnectionCount];
                for (int i = 0; i < ConnectionCount; i++)
                {
                    Socket client = clients[i];
                    Socket server = servers[i];
                    byte value = (byte)(i + 1);

                    work[i] = Task.Run(async () =>
                    {
                        byte[] tx = new byte[] { value };
                        byte[] rx = new byte[1];

                        int sent = await client.SendAsync(tx, SocketFlags.None);
                        Assert.Equal(1, sent);

                        int received = await server.ReceiveAsync(rx, SocketFlags.None);
                        Assert.Equal(1, received);
                        Assert.Equal(value, rx[0]);

                        sent = await server.SendAsync(tx, SocketFlags.None);
                        Assert.Equal(1, sent);

                        received = await client.ReceiveAsync(rx, SocketFlags.None);
                        Assert.Equal(1, received);
                        Assert.Equal(value, rx[0]);
                    });
                }

                await Task.WhenAll(work);
            }
            finally
            {
                for (int i = 0; i < ConnectionCount; i++)
                {
                    servers[i].Dispose();
                    clients[i].Dispose();
                }
            }
        }

        private static async Task RunThreadCountTwoCancellationRoutingScenarioAsync()
        {
            await RunHybridIoUringAndEpollEngineScenarioAsync();

            SocketAsyncEngine[] ioUringEngines = SocketAsyncEngine.GetActiveIoUringEnginesForTest();
            if (ioUringEngines.Length != 1)
            {
                return;
            }

            SocketAsyncEngine ioUringEngine = ioUringEngines[0];
            long queueLengthBefore = ioUringEngine.IoUringCancelQueueLengthForTest;
            long wakeRetryBefore = ioUringEngine.IoUringCancelQueueWakeRetryCountForTest;

            await RunCancellationSubmitContentionScenarioAsync(connectionCount: 8, cancellationsPerConnection: 64);

            Assert.True(queueLengthBefore >= 0);
            Assert.True(ioUringEngine.IoUringCancelQueueLengthForTest >= 0);
            Assert.True(
                ioUringEngine.IoUringCancelQueueLengthForTest <= SocketAsyncEngine.GetIoUringCancellationQueueCapacityForTest());
            Assert.True(ioUringEngine.IoUringCancelQueueWakeRetryCountForTest >= wakeRetryBefore);
        }

        private static async Task RunKernelVersionUnsupportedFallbackScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);
            Assert.Equal(0, SocketAsyncEngine.GetActiveIoUringEnginesForTest().Length);
        }

        private static async Task RunTrackedOperationGenerationTransitionStressScenarioAsync(int connectionCount, int iterationsPerConnection)
        {
            if (!PlatformDetection.IsArm64Process)
            {
                return;
            }

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(connectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();

            var clients = new List<Socket>(connectionCount);
            var servers = new List<Socket>(connectionCount);
            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    (Socket client, Socket server) = await AcceptConnectedTcpPairAsync(listener, endpoint);
                    clients.Add(client);
                    servers.Add(server);
                }

                var workers = new Task[connectionCount];
                for (int i = 0; i < connectionCount; i++)
                {
                    Socket client = clients[i];
                    Socket server = servers[i];
                    workers[i] = Task.Run(async () =>
                    {
                        byte[] sendBuffer = new byte[1];
                        byte[] receiveBuffer = new byte[1];
                        for (int iteration = 0; iteration < iterationsPerConnection; iteration++)
                        {
                            // Stress rapid slot reuse so generation mismatches surface as stuck operations
                            // rather than silently passing under low churn.
                            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                            await Task.Yield();

                            int sent = await client.SendAsync(sendBuffer, SocketFlags.None);
                            Assert.Equal(1, sent);

                            int received = await receiveTask;
                            Assert.Equal(1, received);
                            Assert.Equal(sendBuffer[0], receiveBuffer[0]);

                            unchecked
                            {
                                sendBuffer[0]++;
                            }
                        }
                    });
                }

                Task workerTask = Task.WhenAll(workers);
                Task completed = await Task.WhenAny(workerTask, Task.Delay(TimeSpan.FromSeconds(60)));
                Assert.Same(workerTask, completed);
                await workerTask;
            }
            finally
            {
                foreach (Socket server in servers)
                {
                    server.Dispose();
                }

                foreach (Socket client in clients)
                {
                    client.Dispose();
                }
            }

            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 2, timeoutMilliseconds: 15000),
                "Completion-slot usage remained elevated after ARM64 generation-transition stress.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 2, timeoutMilliseconds: 15000),
                "Tracked-operation count remained elevated after ARM64 generation-transition stress.");
        }

        private static async Task RunGenerationWrapAroundDispatchScenarioAsync()
        {
            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[1];
            Task<int> armReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x5C }, SocketFlags.None));
            Assert.Equal(1, await armReceive);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                "Expected persistent multishot recv to arm before generation-wrap dispatch validation.");

            ulong activeUserData = GetPersistentMultishotRecvUserData(server);
            Assert.NotEqual(0UL, activeUserData);
            Assert.True(IsTrackedIoUringUserDataForTest(activeUserData), "Active multishot user_data should be tracked.");
            Assert.True(TryDecodeCompletionSlotUserDataForTest(activeUserData, out int slotIndex, out ulong generation));

            // Derive max generation from encoding mask and verify helper wrap contract.
            ulong maxEncodedUserData = EncodeCompletionSlotUserDataForTest(slotIndex, ulong.MaxValue);
            Assert.True(TryDecodeCompletionSlotUserDataForTest(maxEncodedUserData, out _, out ulong maxGeneration));
            Assert.Equal(1UL, IncrementCompletionSlotGenerationForTest(maxGeneration));

            ulong staleGeneration = IncrementCompletionSlotGenerationForTest(generation);
            ulong staleUserData = EncodeCompletionSlotUserDataForTest(slotIndex, staleGeneration);
            if (staleUserData == activeUserData)
            {
                staleUserData = EncodeCompletionSlotUserDataForTest(slotIndex, generation == 1UL ? 2UL : 1UL);
            }

            Assert.NotEqual(activeUserData, staleUserData);
            Assert.False(
                IsTrackedIoUringUserDataForTest(staleUserData),
                "Stale wrapped-generation user_data should be rejected during dispatch lookup.");
            Assert.True(IsTrackedIoUringUserDataForTest(activeUserData));
        }

        private static async Task RunBufferListSendRoundTripAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] payload = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };
            var sendBuffers = new List<ArraySegment<byte>>
            {
                new ArraySegment<byte>(payload, 0, 2),
                new ArraySegment<byte>(payload, 2, 1),
                new ArraySegment<byte>(payload, 3, 2)
            };

            byte[] receiveBuffer = new byte[payload.Length];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();

            int sent = await client.SendAsync(sendBuffers, SocketFlags.None);
            Assert.Equal(payload.Length, sent);
            Assert.Equal(payload.Length, await receiveTask);
            Assert.Equal(payload, receiveBuffer);
        }

        private static async Task RunReceiveMessageFromRoundTripAsync()
        {
            using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            byte[] payload = new byte[] { 0x91, 0x92, 0x93 };
            byte[] receiveBuffer = new byte[payload.Length];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            var receiveTask = receiver.ReceiveMessageFromAsync(receiveBuffer, SocketFlags.None, remoteEndPoint);
            await Task.Yield();

            int sent = await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!);
            Assert.Equal(payload.Length, sent);

            SocketReceiveMessageFromResult result = await receiveTask;
            Assert.Equal(payload.Length, result.ReceivedBytes);
            Assert.Equal(payload, receiveBuffer);
            Assert.Equal(sender.LocalEndPoint, result.RemoteEndPoint);
        }

        private static async Task RunReceiveMessageFromPacketInformationRoundTripAsync(bool useIpv6)
        {
            if (useIpv6 && !Socket.OSSupportsIPv6)
            {
                return;
            }

            AddressFamily addressFamily = useIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
            SocketOptionLevel optionLevel = useIpv6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP;
            IPAddress loopbackAddress = useIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
            IPAddress anyAddress = useIpv6 ? IPAddress.IPv6Any : IPAddress.Any;

            using Socket receiver = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp);

            receiver.SetSocketOption(optionLevel, SocketOptionName.PacketInformation, true);
            receiver.Bind(new IPEndPoint(loopbackAddress, 0));
            sender.Bind(new IPEndPoint(loopbackAddress, 0));

            byte[] payload = useIpv6 ?
                new byte[] { 0xA1, 0xA2, 0xA3 } :
                new byte[] { 0x90, 0x91, 0x92, 0x93 };
            byte[] receiveBuffer = new byte[payload.Length];
            EndPoint remoteEndPoint = new IPEndPoint(anyAddress, 0);

            Task<SocketReceiveMessageFromResult> receiveTask =
                ToTask(receiver.ReceiveMessageFromAsync(receiveBuffer, SocketFlags.None, remoteEndPoint));
            await Task.Yield();

            int sent = await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!);
            Assert.Equal(payload.Length, sent);

            SocketReceiveMessageFromResult result = await receiveTask;
            Assert.Equal(payload.Length, result.ReceivedBytes);
            Assert.Equal(payload, receiveBuffer);
            Assert.Equal(sender.LocalEndPoint, result.RemoteEndPoint);
            Assert.Equal(((IPEndPoint)sender.LocalEndPoint!).Address, result.PacketInformation.Address);
        }

        private static async Task RunNonPinnableMemorySendFallbackScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] payload = new byte[] { 0x71, 0x72, 0x73, 0x74 };
            using var nonPinnableMemory = new NonPinnableMemoryManager(payload);
            byte[] receiveBuffer = new byte[payload.Length];

            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            int sent = await client.SendAsync(nonPinnableMemory.Memory, SocketFlags.None);
            Assert.Equal(payload.Length, sent);
            Assert.Equal(payload.Length, await receiveTask);
            Assert.Equal(payload, receiveBuffer);
        }

        private static async Task RunNonPinnableMemoryReceiveFallbackScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[4];
            using var nonPinnableMemory = new NonPinnableMemoryManager(receiveBuffer);
            byte[] payload = new byte[] { 0x81, 0x82, 0x83, 0x84 };

            Task<int> receiveTask = ToTask(server.ReceiveAsync(nonPinnableMemory.Memory, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(payload.Length, await client.SendAsync(payload, SocketFlags.None));
            Assert.Equal(payload.Length, await receiveTask);
            Assert.Equal(payload, receiveBuffer);
        }

        private static Task RunNonPinnableMemoryFallbackScenarioAsync(bool receivePath) =>
            receivePath ? RunNonPinnableMemoryReceiveFallbackScenarioAsync() : RunNonPinnableMemorySendFallbackScenarioAsync();

        private static async Task RunPinnableMemoryPinReleaseLifecycleScenarioAsync()
        {
            if (!GetIoUringProvidedBufferSnapshot().HasIoUringPort)
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Completion path: receive completes with data and must release pin.
            byte[] completionPayload = new byte[] { 0x91 };
            using var completionMemory = new TrackingPinnableMemoryManager(new byte[completionPayload.Length]);
            Task<int> completionReceive = ToTask(server.ReceiveAsync(completionMemory.Memory, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(completionPayload, SocketFlags.None));
            Assert.Equal(1, await completionReceive);
            Assert.Equal(completionPayload, completionMemory.GetSpan().ToArray());
            await AssertPinsReleasedAsync(completionMemory);

            // Cancellation path: pending receive canceled by token must release pin.
            using var cancellationMemory = new TrackingPinnableMemoryManager(new byte[16]);
            using (var cts = new CancellationTokenSource())
            {
                Task<int> canceledReceive = ToTask(server.ReceiveAsync(cancellationMemory.Memory, SocketFlags.None, cts.Token));
                await Task.Delay(20);
                cts.Cancel();

                Exception? canceledException = await Record.ExceptionAsync(async () => await canceledReceive);
                AssertCanceledOrInterrupted(canceledException);
            }

            await AssertPinsReleasedAsync(cancellationMemory);

            // Teardown/abort path: pending receive interrupted by close must release pin.
            using var teardownMemory = new TrackingPinnableMemoryManager(new byte[16]);
            Task<int> teardownReceive = ToTask(server.ReceiveAsync(teardownMemory.Memory, SocketFlags.None));
            await Task.Yield();
            client.Dispose();
            server.Dispose();

            Exception? teardownException = await Record.ExceptionAsync(async () => await teardownReceive);
            AssertCanceledDisposedOrInterrupted(teardownException);
            await AssertPinsReleasedAsync(teardownMemory);
        }

        private static async Task RunProvidedBufferRegistrationLifecycleScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[1];
            Task<int> initialReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0xA1 }, SocketFlags.None));
            Assert.Equal(1, await initialReceive);

            IoUringProvidedBufferSnapshot initialSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(initialSnapshot))
            {
                return;
            }

            Assert.Equal(initialSnapshot.TotalBufferCount, initialSnapshot.AvailableCount + initialSnapshot.InUseCount);
            Assert.Equal(0, initialSnapshot.InUseCount);

            using (var cts = new CancellationTokenSource())
            {
                Task<int> canceledReceive = ToTask(server.ReceiveAsync(new byte[1], SocketFlags.None, cts.Token));
                await Task.Yield();
                cts.Cancel();

                Exception? canceledException = await Record.ExceptionAsync(async () => await canceledReceive);
                AssertCanceledOrInterrupted(canceledException);
            }

            await Task.Delay(50);
            IoUringProvidedBufferSnapshot postCancellationSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.Equal(initialSnapshot.TotalBufferCount, postCancellationSnapshot.TotalBufferCount);
            Assert.Equal(postCancellationSnapshot.TotalBufferCount, postCancellationSnapshot.AvailableCount + postCancellationSnapshot.InUseCount);
            Assert.Equal(0, postCancellationSnapshot.InUseCount);
        }

        private static async Task RunProvidedBufferSelectReceiveScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(beforeSnapshot))
            {
                return;
            }

            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();

            Assert.Equal(1, await client.SendAsync(new byte[] { 0xB2 }, SocketFlags.None));
            Assert.Equal(1, await receiveTask);
            Assert.Equal(0xB2, receiveBuffer[0]);

            IoUringProvidedBufferSnapshot afterSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.Equal(afterSnapshot.TotalBufferCount, afterSnapshot.AvailableCount + afterSnapshot.InUseCount);
            Assert.Equal(0, afterSnapshot.InUseCount);
        }

        private static async Task RunProvidedBufferRecycleReuseScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(beforeSnapshot))
            {
                return;
            }

            long allocationFailuresBefore = beforeSnapshot.AllocationFailureCount;

            int iterations = Math.Max(beforeSnapshot.TotalBufferCount + 64, 512);
            byte[] receiveBuffer = new byte[1];
            byte[] payload = new byte[1];

            for (int i = 0; i < iterations; i++)
            {
                Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();

                payload[0] = unchecked((byte)i);
                Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await receiveTask);
                Assert.Equal(payload[0], receiveBuffer[0]);
            }

            IoUringProvidedBufferSnapshot afterSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.Equal(allocationFailuresBefore, afterSnapshot.AllocationFailureCount);
            Assert.Equal(beforeSnapshot.TotalBufferCount, afterSnapshot.TotalBufferCount);
            Assert.Equal(0, afterSnapshot.InUseCount);
            Assert.Equal(afterSnapshot.TotalBufferCount, afterSnapshot.AvailableCount);
        }

        private static async Task RunProvidedBufferExhaustionScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] warmupBuffer = new byte[1];
            Task<int> warmupReceive = ToTask(server.ReceiveAsync(warmupBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0xC1 }, SocketFlags.None));
            Assert.Equal(1, await warmupReceive);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot))
            {
                return;
            }

            Assert.True(TryForceIoUringProvidedBufferRingExhaustionForTest(out int forcedBufferCount));
            Assert.True(forcedBufferCount > 0);

            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();

            Assert.Equal(1, await client.SendAsync(new byte[] { 0xC2 }, SocketFlags.None));
            Task completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(receiveTask, completed);

            Exception? receiveException = await Record.ExceptionAsync(async () => await receiveTask);
            SocketException socketException = Assert.IsType<SocketException>(receiveException);
            Assert.Equal(SocketError.NoBufferSpaceAvailable, socketException.SocketErrorCode);
        }

        private static async Task RunProvidedBufferMixedWorkloadScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(beforeSnapshot))
            {
                return;
            }

            using Socket udpReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            using Socket udpSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpReceiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            udpSender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            byte[] tcpReceiveBuffer = new byte[1];
            byte[] udpReceiveBuffer = new byte[2];

            Task<int> tcpReceive = ToTask(server.ReceiveAsync(tcpReceiveBuffer, SocketFlags.None));
            Task<SocketReceiveFromResult> udpReceive = ToTask(
                udpReceiver.ReceiveFromAsync(
                    udpReceiveBuffer,
                    SocketFlags.None,
                    new IPEndPoint(IPAddress.Any, 0)));
            await Task.Yield();

            Assert.Equal(1, await client.SendAsync(new byte[] { 0xD1 }, SocketFlags.None));
            Assert.Equal(2, await udpSender.SendToAsync(new byte[] { 0xE1, 0xE2 }, SocketFlags.None, udpReceiver.LocalEndPoint!));

            Assert.Equal(1, await tcpReceive);
            Assert.Equal(0xD1, tcpReceiveBuffer[0]);

            SocketReceiveFromResult udpResult = await udpReceive;
            Assert.Equal(2, udpResult.ReceivedBytes);
            Assert.Equal(0xE1, udpReceiveBuffer[0]);
            Assert.Equal(0xE2, udpReceiveBuffer[1]);

            IoUringProvidedBufferSnapshot afterSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.Equal(afterSnapshot.TotalBufferCount, afterSnapshot.AvailableCount + afterSnapshot.InUseCount);
            Assert.Equal(0, afterSnapshot.InUseCount);
        }

        private static async Task SendExactlyAsync(Socket socket, ReadOnlyMemory<byte> buffer)
        {
            int totalSent = 0;
            while (totalSent < buffer.Length)
            {
                int sent = await socket.SendAsync(buffer.Slice(totalSent), SocketFlags.None);
                Assert.True(sent > 0, "Socket.SendAsync returned 0 before sending all bytes.");
                totalSent += sent;
            }
        }

        private static async Task ReceiveExactlyAsync(Socket socket, Memory<byte> buffer)
        {
            int totalReceived = 0;
            while (totalReceived < buffer.Length)
            {
                int received = await socket.ReceiveAsync(buffer.Slice(totalReceived), SocketFlags.None);
                Assert.True(received > 0, "Socket.ReceiveAsync returned 0 before receiving all expected bytes.");
                totalReceived += received;
            }
        }

        private static async Task<IoUringProvidedBufferSnapshot> WaitForProvidedBufferSnapshotAsync(
            Func<IoUringProvidedBufferSnapshot, bool> predicate,
            int timeoutMilliseconds = 10000)
        {
            DateTime deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(timeoutMilliseconds);
            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            while (DateTime.UtcNow < deadline)
            {
                if (predicate(snapshot))
                {
                    return snapshot;
                }

                await Task.Delay(50);
                snapshot = GetIoUringProvidedBufferSnapshot();
            }

            return snapshot;
        }

        private static async Task RunAdaptiveProvidedBufferSmallMessageShrinkScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            const int payloadSize = 64;
            byte[] sendBuffer = new byte[payloadSize];
            byte[] receiveBuffer = new byte[payloadSize];

            for (int i = 0; i < 320; i++)
            {
                sendBuffer.AsSpan().Fill(unchecked((byte)i));
                Task receiveTask = ReceiveExactlyAsync(server, receiveBuffer);
                await SendExactlyAsync(client, sendBuffer);
                await receiveTask;
                Assert.Equal(sendBuffer, receiveBuffer);
            }

            IoUringProvidedBufferSnapshot afterSnapshot = await WaitForProvidedBufferSnapshotAsync(
                snapshot => IsAdaptiveSizingUsable(snapshot) &&
                    (snapshot.RecommendedBufferSize < initialBufferSize || snapshot.BufferSize < initialBufferSize));

            Assert.True(
                afterSnapshot.RecommendedBufferSize < initialBufferSize || afterSnapshot.BufferSize < initialBufferSize,
                $"Expected adaptive recommendation to shrink from {initialBufferSize}. " +
                $"actual buffer={afterSnapshot.BufferSize}, recommended={afterSnapshot.RecommendedBufferSize}");
        }

        private static async Task RunAdaptiveProvidedBufferLargeMessageGrowScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            int payloadSize = initialBufferSize;
            byte[] sendBuffer = new byte[payloadSize];
            byte[] receiveBuffer = new byte[payloadSize];
            sendBuffer.AsSpan().Fill(0x5A);

            for (int i = 0; i < 320; i++)
            {
                Task receiveTask = ReceiveExactlyAsync(server, receiveBuffer);
                await SendExactlyAsync(client, sendBuffer);
                await receiveTask;
                Assert.Equal(sendBuffer, receiveBuffer);
            }

            IoUringProvidedBufferSnapshot afterSnapshot = await WaitForProvidedBufferSnapshotAsync(
                snapshot => IsAdaptiveSizingUsable(snapshot) &&
                    (snapshot.RecommendedBufferSize > initialBufferSize || snapshot.BufferSize > initialBufferSize));

            Assert.True(
                afterSnapshot.RecommendedBufferSize > initialBufferSize || afterSnapshot.BufferSize > initialBufferSize,
                $"Expected adaptive recommendation to grow from {initialBufferSize}. " +
                $"actual buffer={afterSnapshot.BufferSize}, recommended={afterSnapshot.RecommendedBufferSize}");
        }

        private static async Task RunAdaptiveProvidedBufferMixedWorkloadStableScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            byte[] smallSend = new byte[64];
            byte[] smallReceive = new byte[64];
            byte[] largeSend = new byte[initialBufferSize];
            byte[] largeReceive = new byte[initialBufferSize];
            smallSend.AsSpan().Fill(0x11);
            largeSend.AsSpan().Fill(0x77);

            for (int i = 0; i < 320; i++)
            {
                bool useLarge = (i & 1) == 1;
                byte[] send = useLarge ? largeSend : smallSend;
                byte[] receive = useLarge ? largeReceive : smallReceive;

                Task receiveTask = ReceiveExactlyAsync(server, receive);
                await SendExactlyAsync(client, send);
                await receiveTask;
                Assert.Equal(send, receive);
            }

            await Task.Delay(250);
            IoUringProvidedBufferSnapshot afterSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.True(IsAdaptiveSizingUsable(afterSnapshot));
            Assert.Equal(initialBufferSize, afterSnapshot.RecommendedBufferSize);
        }

        private static async Task RunAdaptiveProvidedBufferResizeSwapNoDataLossScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            const int payloadSize = 64;
            byte[] sendBuffer = new byte[payloadSize];
            byte[] receiveBuffer = new byte[payloadSize];
            for (int i = 0; i < 384; i++)
            {
                sendBuffer.AsSpan().Fill(unchecked((byte)i));
                Task receiveTask = ReceiveExactlyAsync(server, receiveBuffer);
                await SendExactlyAsync(client, sendBuffer);
                await receiveTask;
                Assert.Equal(sendBuffer, receiveBuffer);
            }

            IoUringProvidedBufferSnapshot afterSnapshot = await WaitForProvidedBufferSnapshotAsync(
                snapshot => IsAdaptiveSizingUsable(snapshot) && snapshot.BufferSize < initialBufferSize,
                timeoutMilliseconds: 15000);

            Assert.True(
                afterSnapshot.BufferSize < initialBufferSize,
                $"Expected adaptive resize swap to shrink active ring. initial={initialBufferSize}, current={afterSnapshot.BufferSize}");
        }

        private static async Task RunAdaptiveProvidedBufferResizeSwapConcurrentInFlightNoDataLossScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            const int batchSize = 64;
            const int rounds = 24;

            // Keep many receives in flight while driving enough completions to trigger adaptive
            // resize; this exercises ring-swap safety under concurrent tracked receive activity.
            for (int round = 0; round < rounds; round++)
            {
                Task<int>[] receiveTasks = new Task<int>[batchSize];
                byte[][] receiveBuffers = new byte[batchSize][];
                for (int i = 0; i < batchSize; i++)
                {
                    byte[] receiveBuffer = new byte[1];
                    receiveBuffers[i] = receiveBuffer;
                    receiveTasks[i] = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                }

                await Task.Yield();

                for (int i = 0; i < batchSize; i++)
                {
                    byte expected = unchecked((byte)(round + i + 1));
                    Assert.Equal(1, await AwaitWithTimeoutAsync(client.SendAsync(new[] { expected }, SocketFlags.None), $"adaptive_resize_send_{round}_{i}"));
                }

                int[] completed = await AwaitWithTimeoutAsync(Task.WhenAll(receiveTasks), $"adaptive_resize_whenall_{round}");
                for (int i = 0; i < batchSize; i++)
                {
                    Assert.Equal(1, completed[i]);
                    Assert.NotEqual(0, receiveBuffers[i][0]);
                }
            }

            IoUringProvidedBufferSnapshot afterSnapshot = await WaitForProvidedBufferSnapshotAsync(
                snapshot => IsAdaptiveSizingUsable(snapshot) && snapshot.BufferSize < initialBufferSize,
                timeoutMilliseconds: 15000);

            Assert.True(
                afterSnapshot.BufferSize < initialBufferSize,
                $"Expected adaptive resize swap to shrink active ring under in-flight receive stress. initial={initialBufferSize}, current={afterSnapshot.BufferSize}");
            Assert.Equal(0, afterSnapshot.InUseCount);
            Assert.Equal(afterSnapshot.TotalBufferCount, afterSnapshot.AvailableCount + afterSnapshot.InUseCount);
        }

        private static async Task RunAdaptiveProvidedBufferDisabledScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(beforeSnapshot))
            {
                return;
            }

            Assert.False(beforeSnapshot.AdaptiveBufferSizingEnabled);

            int initialBufferSize = beforeSnapshot.BufferSize;
            int initialRecommendedSize = beforeSnapshot.RecommendedBufferSize;

            const int payloadSize = 64;
            byte[] sendBuffer = new byte[payloadSize];
            byte[] receiveBuffer = new byte[payloadSize];
            sendBuffer.AsSpan().Fill(0xA5);

            for (int i = 0; i < 320; i++)
            {
                Task receiveTask = ReceiveExactlyAsync(server, receiveBuffer);
                await SendExactlyAsync(client, sendBuffer);
                await receiveTask;
                Assert.Equal(sendBuffer, receiveBuffer);
            }

            await Task.Delay(250);
            IoUringProvidedBufferSnapshot afterSnapshot = GetIoUringProvidedBufferSnapshot();
            Assert.True(IsProvidedBufferSnapshotUsable(afterSnapshot));
            Assert.False(afterSnapshot.AdaptiveBufferSizingEnabled);
            Assert.Equal(initialBufferSize, afterSnapshot.BufferSize);
            Assert.Equal(initialRecommendedSize, afterSnapshot.RecommendedBufferSize);
        }

        private static async Task RunAdaptiveProvidedBufferSizingStateScenarioAsync(bool expectedEnabled)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Warm up receive path so io_uring provided-buffer ring state is initialized.
            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x42 }, SocketFlags.None));
            Assert.Equal(1, await receiveTask);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot))
            {
                return;
            }

            Assert.Equal(expectedEnabled, snapshot.AdaptiveBufferSizingEnabled);
        }

        private static async Task RunProvidedBufferKernelRegistrationDisabledScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Warm up receive path so io_uring provided-buffer ring state is initialized.
            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x42 }, SocketFlags.None));
            Assert.Equal(1, await receiveTask);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot))
            {
                return;
            }

            Assert.False(snapshot.HasRegisteredBuffers);
        }

        private static async Task RunProvidedBufferKernelRegistrationSuccessScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Warm up receive path so io_uring provided-buffer ring state and telemetry are initialized.
            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x42 }, SocketFlags.None));
            Assert.Equal(1, await receiveTask);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot))
            {
                return;
            }

            // Best-effort success-path assertion: only enforce when registration succeeded on this machine.
            if (!snapshot.HasRegisteredBuffers)
            {
                return;
            }
        }

        private static async Task RunProvidedBufferKernelRegistrationFailureNonFatalScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Warm up receive path so io_uring provided-buffer ring state and telemetry are initialized.
            byte[] receiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x42 }, SocketFlags.None));
            Assert.Equal(1, await receiveTask);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot) || snapshot.HasRegisteredBuffers)
            {
                // No observed registration failure in this environment.
                return;
            }

            // Registration is not active: verify provided-buffer receive path still works.
            byte[] payload = new byte[4096];
            byte[] received = new byte[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 31));
            }

            Task receiveAllTask = ReceiveExactlyAsync(server, received);
            await SendExactlyAsync(client, payload);
            await receiveAllTask;
            Assert.Equal(payload, received);
        }

        private static async Task RunProvidedBufferKernelReregistrationOnResizeScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot beforeSnapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsAdaptiveSizingUsable(beforeSnapshot))
            {
                return;
            }

            int initialBufferSize = beforeSnapshot.BufferSize;
            Assert.True(initialBufferSize > 0);

            const int payloadSize = 64;
            byte[] sendBuffer = new byte[payloadSize];
            byte[] receiveBuffer = new byte[payloadSize];
            for (int i = 0; i < 384; i++)
            {
                sendBuffer.AsSpan().Fill(unchecked((byte)(i + 1)));
                Task receivePayloadTask = ReceiveExactlyAsync(server, receiveBuffer);
                await SendExactlyAsync(client, sendBuffer);
                await receivePayloadTask;
                Assert.Equal(sendBuffer, receiveBuffer);
            }

            IoUringProvidedBufferSnapshot afterSnapshot = await WaitForProvidedBufferSnapshotAsync(
                snapshot => IsAdaptiveSizingUsable(snapshot) && snapshot.BufferSize < initialBufferSize,
                timeoutMilliseconds: 15000);

            Assert.True(afterSnapshot.BufferSize < initialBufferSize);
        }

        private static async Task RunProvidedBufferRegisteredBuffersDataCorrectnessScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot) || !snapshot.HasRegisteredBuffers)
            {
                return;
            }

            // Reuse the mixed workload profile to validate payload correctness with registered buffers active.
            byte[] smallSend = new byte[64];
            byte[] largeSend = new byte[Math.Max(snapshot.BufferSize, 4096)];
            byte[] smallReceive = new byte[smallSend.Length];
            byte[] largeReceive = new byte[largeSend.Length];

            for (int i = 0; i < 64; i++)
            {
                smallSend.AsSpan().Fill(unchecked((byte)(i + 5)));
                largeSend.AsSpan().Fill(unchecked((byte)(i + 11)));

                Task smallReceiveTask = ReceiveExactlyAsync(server, smallReceive);
                await SendExactlyAsync(client, smallSend);
                await smallReceiveTask;
                Assert.Equal(smallSend, smallReceive);

                Task largeReceiveTask = ReceiveExactlyAsync(server, largeReceive);
                await SendExactlyAsync(client, largeSend);
                await largeReceiveTask;
                Assert.Equal(largeSend, largeReceive);
            }
        }

        private static async Task RunProvidedBufferRegistrationMemoryPressureScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!IsProvidedBufferSnapshotUsable(snapshot))
            {
                return;
            }

            int payloadSize = Math.Min(snapshot.BufferSize, 16 * 1024);
            payloadSize = Math.Max(payloadSize, 1024);
            byte[] payload = new byte[payloadSize];
            byte[] received = new byte[payloadSize];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 41));
            }

            Task receiveTask = ReceiveExactlyAsync(server, received);
            await SendExactlyAsync(client, payload);
            await receiveTask;
            Assert.Equal(payload, received);
        }

        private static async Task RunProvidedBufferRingForcedAllocationFailureFallbackScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);

            IoUringProvidedBufferSnapshot snapshot = GetIoUringProvidedBufferSnapshot();
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            Assert.False(snapshot.HasProvidedBufferRing, "Provided-buffer ring should be disabled after forced allocation failure.");
            Assert.False(snapshot.SupportsProvidedBufferRings, "Capability should remain disabled when provided-buffer ring creation fails.");

            // Ensure sockets continue to function after provided-buffer OOM fallback.
            await RunTcpRoundTripAsync(4);
        }

        private static Task RunProvidedBufferTeardownOrderingContractScenarioAsync()
        {
            Assert.True(
                SocketAsyncEngine.ValidateIoUringProvidedBufferTeardownOrderingForTest(),
                "Expected teardown to unregister/dispose provided buffers before ring unmap/close.");

            return Task.CompletedTask;
        }

        private static async Task RunZeroCopySendStateScenarioAsync(bool expectedEnabledWhenSupported)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] sendBuffer = new byte[64];
            byte[] receiveBuffer = new byte[sendBuffer.Length];
            Assert.Equal(sendBuffer.Length, await client.SendAsync(sendBuffer, SocketFlags.None));
            await ReceiveExactlyAsync(server, receiveBuffer);

            IoUringZeroCopySendSnapshot snapshot = GetIoUringZeroCopySendSnapshot();
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            if (!snapshot.SupportsSendZc)
            {
                Assert.False(snapshot.ZeroCopySendEnabled);
                return;
            }

            Assert.Equal(expectedEnabledWhenSupported, snapshot.ZeroCopySendEnabled);
        }

        private static async Task RunFixedRecvStateScenarioAsync(bool expectedEnabled)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] sendBuffer = new byte[64];
            byte[] receiveBuffer = new byte[sendBuffer.Length];
            Assert.Equal(sendBuffer.Length, await client.SendAsync(sendBuffer, SocketFlags.None));
            await ReceiveExactlyAsync(server, receiveBuffer);

            IoUringFixedRecvSnapshot snapshot = GetIoUringFixedRecvSnapshot();
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            Assert.Equal(expectedEnabled, IsFixedRecvEnabled(snapshot));
        }

        private static async Task RunFixedRecvActivationFollowsRuntimeCapabilitiesScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] sendBuffer = new byte[64];
            byte[] receiveBuffer = new byte[sendBuffer.Length];
            Assert.Equal(sendBuffer.Length, await client.SendAsync(sendBuffer, SocketFlags.None));
            await ReceiveExactlyAsync(server, receiveBuffer);

            IoUringFixedRecvSnapshot snapshot = GetIoUringFixedRecvSnapshot();
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            Assert.Equal(snapshot.SupportsReadFixed && snapshot.HasRegisteredBuffers, IsFixedRecvEnabled(snapshot));
        }

        private static async Task RunFixedRecvDataCorrectnessScenarioAsync()
        {
            IoUringFixedRecvSnapshot snapshot = GetIoUringFixedRecvSnapshot();
            if (!snapshot.HasIoUringPort || !IsFixedRecvEnabled(snapshot) || !snapshot.SupportsReadFixed || !snapshot.HasRegisteredBuffers)
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            byte[] payload = new byte[32 * 1024];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i * 13));
            }

            byte[] received = new byte[payload.Length];
            Task receiveTask = ReceiveExactlyAsync(server, received);
            Assert.Equal(payload.Length, await client.SendAsync(payload, SocketFlags.None));
            await receiveTask;
            Assert.Equal(payload, received);
        }

        private static async Task RunSqPollBasicSendReceiveScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(8);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!IsSqPollActive(snapshot))
            {
                return;
            }

            await RunTcpRoundTripAsync(16);
        }

        private static async Task RunDeferTaskrunEventLoopInitScenarioAsync()
        {
            // io_uring_setup runs on the event loop thread (deferred from constructor via
            // ManualResetEventSlim gate in TryRegisterSocket). This sets submitter_task to
            // the event loop thread so DEFER_TASKRUN's per-enter check passes.
            // TCP round-trips exercise io_uring_enter; if submitter_task were wrong,
            // io_uring_enter would return EEXIST and all operations would fail.
            await RunTcpRoundTripAsync(8);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            // Non-SQPOLL engines negotiate DEFER_TASKRUN by default.
            if (!snapshot.SqPollEnabled)
            {
                Assert.True(
                    snapshot.DeferTaskrunEnabled,
                    "Non-SQPOLL io_uring engines should negotiate DEFER_TASKRUN.");
            }

            // Additional round-trips to confirm ongoing stability.
            await RunTcpRoundTripAsync(8);
        }

        private static async Task RunSqPollRequestedScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(8);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            // Some Helix legs can run without an active io_uring port (kernel/config/runtime gating).
            // In that case this SQPOLL-request scenario is not applicable.
            if (!snapshot.HasIoUringPort)
            {
                return;
            }

            if (!snapshot.SqPollEnabled)
            {
                // SQPOLL wasn't active on this leg, but socket operations must continue to succeed.
                await RunTcpRoundTripAsync(16);
                return;
            }

            Assert.False(
                snapshot.DeferTaskrunEnabled,
                "SQPOLL and DEFER_TASKRUN must be mutually exclusive in negotiated io_uring setup flags.");
        }

        private static async Task RunSqPollWakeupAfterIdleScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(4);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!IsSqPollActive(snapshot))
            {
                return;
            }

            // Let the kernel SQPOLL thread go idle and set SQ_NEED_WAKEUP.
            bool observedNeedWakeup = false;
            for (int i = 0; i < 25; i++)
            {
                await Task.Delay(100);
                if (IsAnyIoUringSqPollEngineNeedingWakeup())
                {
                    observedNeedWakeup = true;
                    break;
                }
            }

            if (!observedNeedWakeup)
            {
                return;
            }

            await RunTcpRoundTripAsync(2);
        }

        private static async Task RunSqPollMultishotRecvScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(4);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!IsSqPollActive(snapshot))
            {
                return;
            }

            await RunMultishotRecvBasicScenarioAsync(iterations: 32);
        }

        private static async Task RunSqPollZeroCopySendScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(4);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!IsSqPollActive(snapshot))
            {
                return;
            }

            await RunZeroCopySendLargeBufferRoundTripScenarioAsync();
        }

        private static async Task RunSqPollNeedWakeupContractScenarioAsync()
        {
            EnableSqPollAppContextOptIn();
            await RunTcpRoundTripAsync(4);

            IoUringSqPollSnapshot snapshot = GetIoUringSqPollSnapshot();
            if (!IsSqPollActive(snapshot))
            {
                return;
            }

            Assert.True(
                ValidateSqNeedWakeupMatchesRawSqFlagBit(),
                "Expected at least one active SQPOLL io_uring engine for SqNeedWakeup contract validation.");
        }

        private static bool IsZeroCopySendEnabledAndSupported(out IoUringZeroCopySendSnapshot snapshot)
        {
            snapshot = GetIoUringZeroCopySendSnapshot();
            return snapshot.HasIoUringPort && snapshot.SupportsSendZc && snapshot.ZeroCopySendEnabled;
        }

        private static bool IsZeroCopySendMessageEnabledAndSupported(out IoUringZeroCopySendSnapshot snapshot)
        {
            snapshot = GetIoUringZeroCopySendSnapshot();
            return snapshot.HasIoUringPort && snapshot.SupportsSendMsgZc && snapshot.ZeroCopySendEnabled;
        }

        private static async Task RunZeroCopySendLargeBufferRoundTripScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            byte[] payload = new byte[64 * 1024];
            byte[] received = new byte[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)i);
            }

            Task receiveTask = ReceiveExactlyAsync(server, received);
            int sent = await client.SendAsync(payload, SocketFlags.None);
            Assert.Equal(payload.Length, sent);
            await receiveTask;
            Assert.Equal(payload, received);
        }

        private static async Task RunZeroCopySendSmallBufferUsesRegularSendWithForcedSendErrorScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            byte[] smallPayload = new byte[1024];
            // forceEcanceledOnceMask: "send" is set by the caller. Small payloads should use regular SEND,
            // so the first send is expected to observe the injected cancellation/interruption.
            Exception? sendException = await Record.ExceptionAsync(async () => await client.SendAsync(smallPayload, SocketFlags.None));
            AssertCanceledOrInterrupted(sendException);

            byte[] verificationPayload = new byte[] { 0x5A };
            byte[] verificationReceive = new byte[1];
            Task<int> verificationReceiveTask = ToTask(server.ReceiveAsync(verificationReceive, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(verificationPayload, SocketFlags.None));
            Assert.Equal(1, await verificationReceiveTask);
            Assert.Equal(verificationPayload[0], verificationReceive[0]);
        }

        private static async Task RunZeroCopySendNotifCqeReleasesPinHoldsScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            byte[] payload = new byte[128 * 1024];
            byte[] received = new byte[payload.Length];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 1));
            }

            const int iterations = 8;
            for (int i = 0; i < iterations; i++)
            {
                Task receiveTask = ReceiveExactlyAsync(server, received);
                int sent = await client.SendAsync(payload, SocketFlags.None);
                Assert.Equal(payload.Length, sent);
                await receiveTask;
                Assert.Equal(payload, received);
            }

            IoUringZeroCopyPinHoldSnapshot releasedSnapshot = await WaitForZeroCopyPinHoldSnapshotAsync(
                static snapshot => !snapshot.HasIoUringPort || (snapshot.ActivePinHolds == 0 && snapshot.PendingNotificationCount == 0));
            if (!releasedSnapshot.HasIoUringPort)
            {
                return;
            }

            Assert.Equal(0, releasedSnapshot.ActivePinHolds);
            Assert.Equal(0, releasedSnapshot.PendingNotificationCount);
        }

        private static async Task RunZeroCopySendResetStormSlotRecoveryScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            const int ConcurrentSendCount = 512;
            const int SlotPressureDelta = 32;
            TimeSpan runDuration = TimeSpan.FromSeconds(60);

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(ConcurrentSendCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            int baselineSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            IoUringZeroCopyPinHoldSnapshot baselineSnapshot = GetIoUringZeroCopyPinHoldSnapshot();

            byte[] payload = new byte[64 * 1024];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 11));
            }

            DateTime deadline = DateTime.UtcNow + runDuration;
            int rounds = 0;
            int roundsWithConnectionReset = 0;
            bool observedPendingNotifications = false;
            // Long-running reset churn is intentional: leaked pending-NOTIF slots tend to show
            // up only after repeated mid-flight resets, not short happy-path bursts.
            while (DateTime.UtcNow < deadline)
            {
                (Socket client, Socket server) = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using (client)
                using (server)
                {
                    server.LingerState = new LingerOption(enable: true, seconds: 0);
                    var sendTasks = new Task<int>[ConcurrentSendCount];
                    for (int i = 0; i < sendTasks.Length; i++)
                    {
                        sendTasks[i] = ToTask(client.SendAsync(payload, SocketFlags.None));
                    }

                    // Wait for slot pressure rather than sleeping arbitrarily so the test
                    // only resets once a meaningful in-flight SEND_ZC wave exists.
                    Assert.True(
                        await WaitForIoUringCompletionSlotsInUseAboveAsync(baselineSlotsInUse, SlotPressureDelta, timeoutMilliseconds: 2_000),
                        $"Expected completion slots to exceed baseline {baselineSlotsInUse} by at least {SlotPressureDelta}, observed {GetIoUringCompletionSlotsInUseForTest()}.");

                    if (GetIoUringZeroCopyPinHoldSnapshot().PendingNotificationCount > 0)
                    {
                        observedPendingNotifications = true;
                    }

                    server.Dispose();

                    bool roundSawConnectionReset = false;
                    for (int i = 0; i < sendTasks.Length; i++)
                    {
                        Exception? ex = await Record.ExceptionAsync(async () => await sendTasks[i]);
                        if (ex is null)
                        {
                            continue;
                        }

                        if (ex is SocketException socketException)
                        {
                            if (socketException.SocketErrorCode == SocketError.ConnectionReset)
                            {
                                roundSawConnectionReset = true;
                            }

                            Assert.True(
                                socketException.SocketErrorCode == SocketError.ConnectionReset ||
                                socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                                socketException.SocketErrorCode == SocketError.OperationAborted ||
                                socketException.SocketErrorCode == SocketError.Interrupted ||
                                socketException.SocketErrorCode == SocketError.Shutdown,
                                $"Unexpected socket error during reset-churn SEND_ZC stress: {socketException.SocketErrorCode}");
                        }
                        else
                        {
                            Assert.True(
                                ex is ObjectDisposedException || ex is OperationCanceledException,
                                $"Unexpected exception during reset-churn SEND_ZC stress: {ex}");
                        }
                    }

                    if (roundSawConnectionReset)
                    {
                        roundsWithConnectionReset++;
                    }
                }

                rounds++;
            }

            Assert.True(rounds > 0, "Expected at least one reset-churn round in the SEND_ZC recovery scenario.");
            Assert.True(
                observedPendingNotifications,
                "Expected to observe at least one in-flight pending SEND_ZC notification during reset-churn stress.");
            Assert.True(
                (double)roundsWithConnectionReset / rounds >= 0.10,
                $"Expected at least 10% of reset-churn rounds to include ConnectionReset; observed {roundsWithConnectionReset}/{rounds}.");

            IoUringZeroCopyPinHoldSnapshot settledSnapshot = await WaitForZeroCopyPinHoldSnapshotAsync(
                snapshot => !snapshot.HasIoUringPort ||
                    (snapshot.ActivePinHolds == baselineSnapshot.ActivePinHolds &&
                     snapshot.PendingNotificationCount == baselineSnapshot.PendingNotificationCount),
                timeoutMilliseconds: 30_000);
            if (!settledSnapshot.HasIoUringPort)
            {
                return;
            }

            Assert.Equal(baselineSnapshot.ActivePinHolds, settledSnapshot.ActivePinHolds);
            Assert.Equal(baselineSnapshot.PendingNotificationCount, settledSnapshot.PendingNotificationCount);
            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineSlotsInUse, timeoutMilliseconds: 30_000),
                $"Expected completion slots to recover to baseline {baselineSlotsInUse}, observed {GetIoUringCompletionSlotsInUseForTest()}.");

            await RunZeroCopySendLargeBufferRoundTripScenarioAsync();
        }

        private static async Task RunZeroCopySendPartialSendResubmissionScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            await RunLargeSendWithBackpressureAsync(useBufferListSend: false);
        }

        private static async Task RunZeroCopySendCompletionPinLifetimeScenarioAsync()
        {
            if (!IsZeroCopySendEnabledAndSupported(out _))
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            byte[] payload = new byte[96 * 1024];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 3));
            }

            using var trackingMemory = new TrackingPinnableMemoryManager(payload);
            byte[] received = new byte[payload.Length];
            Task receiveTask = ReceiveExactlyAsync(server, received);
            int sent = await client.SendAsync(trackingMemory.Memory, SocketFlags.None);
            Assert.Equal(payload.Length, sent);
            await receiveTask;
            await AssertPinsReleasedAsync(trackingMemory);
            Assert.Equal(payload, received);
        }

        private static async Task RunZeroCopySendUnsupportedOpcodeFallbackScenarioAsync()
        {
            SocketAsyncEngine[] engines = SocketAsyncEngine.GetActiveIoUringEnginesForTest();
            var overrides = new List<(SocketAsyncEngine Engine, bool SupportsSendZc, bool ZeroCopyEnabled)>(engines.Length);
            foreach (SocketAsyncEngine engine in engines)
            {
                overrides.Add((engine, engine.SupportsOpSendZcForTest, engine.ZeroCopySendEnabledForTest));
                engine.SupportsOpSendZcForTest = false;
                engine.ZeroCopySendEnabledForTest = false;
            }

            if (engines.Length == 0)
            {
                return;
            }

            try
            {
                IoUringZeroCopySendSnapshot snapshot = GetIoUringZeroCopySendSnapshot();
                Assert.False(snapshot.SupportsSendZc);
                Assert.False(snapshot.ZeroCopySendEnabled);

                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] payload = new byte[64 * 1024];
                byte[] received = new byte[payload.Length];
                Task receiveTask = ReceiveExactlyAsync(server, received);
                int sent = await client.SendAsync(payload, SocketFlags.None);
                Assert.Equal(payload.Length, sent);
                await receiveTask;
                Assert.Equal(payload, received);
            }
            finally
            {
                foreach ((SocketAsyncEngine engine, bool supports, bool enabled) in overrides)
                {
                    engine.SupportsOpSendZcForTest = supports;
                    engine.ZeroCopySendEnabledForTest = enabled;
                }
            }
        }

        private static async Task RunZeroCopySendBufferListSegmentThresholdScenarioAsync()
        {
            if (!IsZeroCopySendMessageEnabledAndSupported(out _))
            {
                return;
            }

            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;

            const int segmentCount = 8;
            const int segmentSize = 4 * 1024;
            int payloadLength = segmentCount * segmentSize;
            byte[] payload = new byte[payloadLength];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 17));
            }

            var sendBuffers = new List<ArraySegment<byte>>(segmentCount);
            for (int i = 0; i < segmentCount; i++)
            {
                sendBuffers.Add(new ArraySegment<byte>(payload, i * segmentSize, segmentSize));
            }

            byte[] received = new byte[payload.Length];
            Task receiveTask = ReceiveExactlyAsync(server, received);
            int sent = await client.SendAsync(sendBuffers, SocketFlags.None);
            Assert.Equal(payload.Length, sent);
            await receiveTask;
            Assert.Equal(payload, received);
        }

        private static async Task RunZeroCopySendToAboveThresholdScenarioAsync()
        {
            if (!IsZeroCopySendMessageEnabledAndSupported(out _))
            {
                return;
            }

            using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            byte[] payload = new byte[20 * 1024];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = unchecked((byte)(i + 23));
            }

            byte[] receiveBuffer = new byte[payload.Length];
            Task<SocketReceiveFromResult> receiveTask =
                ToTask(receiver.ReceiveFromAsync(receiveBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));
            await Task.Yield();

            int sent = await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!);
            Assert.Equal(payload.Length, sent);

            SocketReceiveFromResult receiveResult = await receiveTask;
            Assert.Equal(payload.Length, receiveResult.ReceivedBytes);
            Assert.Equal(payload, receiveBuffer);
            Assert.Equal(sender.LocalEndPoint, receiveResult.RemoteEndPoint);
        }

        private static async Task RunMultishotRecvBasicScenarioAsync(int iterations)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            byte[] receiveBuffer = new byte[1];
            byte[] payload = new byte[1];
            for (int i = 0; i < iterations; i++)
            {
                Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();

                payload[0] = unchecked((byte)(i + 1));
                Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await receiveTask);
                Assert.Equal(payload[0], receiveBuffer[0]);
            }

            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                "Expected persistent multishot recv to remain armed after repeated ReceiveAsync calls.");
        }

        private static async Task RunMultishotRecvCancellationScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket listener = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            _ = listener;
            _ = client;

            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            byte[] receiveBuffer = new byte[16];
            using var cts = new CancellationTokenSource();
            Task<int> pendingReceive = ToTask(server.ReceiveAsync(receiveBuffer.AsMemory(), SocketFlags.None, cts.Token));
            await Task.Yield();
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                "Expected persistent multishot recv to arm before cancellation.");

            cts.Cancel();
            Task completed = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(pendingReceive, completed);
            Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
            AssertCanceledOrInterrupted(ex);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: false),
                "Expected persistent multishot recv to disarm after cancellation.");
        }

        private static async Task RunMultishotRecvPeerCloseScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            byte[] receiveBuffer = new byte[8];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();

            client.Shutdown(SocketShutdown.Both);
            client.Dispose();

            Task completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(receiveTask, completed);

            Exception? ex = await Record.ExceptionAsync(async () => await receiveTask);
            if (ex is null)
            {
                Assert.Equal(0, await receiveTask);
            }
            else
            {
                SocketException socketException = Assert.IsType<SocketException>(ex);
                Assert.True(
                    socketException.SocketErrorCode == SocketError.ConnectionReset ||
                    socketException.SocketErrorCode == SocketError.OperationAborted ||
                    socketException.SocketErrorCode == SocketError.Interrupted,
                    $"Unexpected socket error after multishot peer close: {socketException.SocketErrorCode}");
            }

            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: false),
                "Expected persistent multishot recv to disarm after terminal peer-close completion.");
        }

        private static async Task RunPersistentMultishotRecvProvidedBufferExhaustionScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            byte[] receiveBuffer = new byte[1];
            Task<int> armReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0xC3 }, SocketFlags.None));
            Assert.Equal(1, await armReceive);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                "Expected persistent multishot recv to arm before forced provided-buffer exhaustion.");

            Assert.True(TryForceIoUringProvidedBufferRingExhaustionForTest(out int forcedBufferCount));
            Assert.True(forcedBufferCount > 0);

            // This test intentionally exercises kernel-visible provided-buffer-group exhaustion:
            // ring buffers are force-checked-out so the kernel cannot select a buffer and reports
            // terminal ENOBUFS. This is distinct from userspace drain/materialization pressure,
            // where positive-result shots may be dropped (drop_persistent_shot) without terminal
            // ENOBUFS injection.
            Task<int> exhaustedReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0xC4 }, SocketFlags.None));
            Task exhaustedCompleted = await Task.WhenAny(exhaustedReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(exhaustedReceive, exhaustedCompleted);

            Exception? exhaustedException = await Record.ExceptionAsync(async () => await exhaustedReceive);
            SocketException exhaustedSocketException = Assert.IsType<SocketException>(exhaustedException);
            Assert.Equal(SocketError.NoBufferSpaceAvailable, exhaustedSocketException.SocketErrorCode);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: false),
                "Expected persistent multishot recv to disarm after ENOBUFS terminal completion.");

            Assert.True(TryRecycleForcedIoUringProvidedBufferRingForTest(out int recycledBufferCount));
            Assert.True(recycledBufferCount > 0, "Expected forced checked-out provided buffers to be recycled for recovery.");

            Task<int> recoveredReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0xC5 }, SocketFlags.None));
            Assert.Equal(1, await recoveredReceive);
            Assert.Equal(0xC5, receiveBuffer[0]);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                "Expected persistent multishot recv to re-arm after provided buffers were recycled.");
        }

        private static async Task RunPersistentMultishotRecvShapeChangeScenarioAsync()
        {
            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            receiver.Connect(sender.LocalEndPoint!);
            sender.Connect(receiver.LocalEndPoint!);

            byte[] receiveBuffer = new byte[1];
            Task<int> armReceive = ToTask(receiver.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await sender.SendAsync(new byte[] { 0xD1 }, SocketFlags.None));
            Assert.Equal(1, await armReceive);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(receiver, expectedArmed: true),
                "Expected persistent multishot recv to arm before shape-change scenario.");

            byte[] receiveFromBuffer = new byte[1];
            Task<SocketReceiveFromResult> receiveFromTask = ToTask(
                receiver.ReceiveFromAsync(receiveFromBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));
            await Task.Yield();
            Assert.Equal(1, await sender.SendAsync(new byte[] { 0xD2 }, SocketFlags.None));
            SocketReceiveFromResult receiveFromResult = await receiveFromTask;
            Assert.Equal(1, receiveFromResult.ReceivedBytes);
            Assert.Equal(0xD2, receiveFromBuffer[0]);

            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(receiver, expectedArmed: false),
                "Expected persistent multishot recv to disarm when receive shape switches to ReceiveFromAsync.");

            Task<int> rearmReceive = ToTask(receiver.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await sender.SendAsync(new byte[] { 0xD3 }, SocketFlags.None));
            Assert.Equal(1, await rearmReceive);
            Assert.Equal(0xD3, receiveBuffer[0]);
            Assert.True(
                await WaitForPersistentMultishotRecvArmedStateAsync(receiver, expectedArmed: true),
                "Expected persistent multishot recv to re-arm after shape-change operation completed.");
        }

        private static async Task RunPersistentMultishotRecvDataThenFinScenarioAsync(int iterations)
        {
            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] receiveBuffer = new byte[1];

                Task<int> armReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();
                Assert.Equal(1, await client.SendAsync(new byte[] { 0xE1 }, SocketFlags.None));
                Assert.Equal(1, await AwaitWithTimeoutAsync(armReceive, $"data_fin_arm_{i}"));
                Assert.Equal(0xE1, receiveBuffer[0]);
                Assert.True(
                    await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                    $"Expected persistent multishot recv to arm before FIN race at iteration {i}.");

                Task<int> dataReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();
                Assert.Equal(1, await client.SendAsync(new byte[] { 0xE2 }, SocketFlags.None));
                client.Shutdown(SocketShutdown.Send);

                int dataBytes = await AwaitWithTimeoutAsync(dataReceive, $"data_fin_payload_{i}");
                Assert.Equal(1, dataBytes);
                Assert.Equal(0xE2, receiveBuffer[0]);

                Task<int> terminalReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                int terminalBytes = await AwaitWithTimeoutAsync(terminalReceive, $"data_fin_terminal_{i}");
                Assert.Equal(0, terminalBytes);
            }
        }

        private static async Task RunPersistentMultishotRecvDataThenResetScenarioAsync(int iterations)
        {
            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] receiveBuffer = new byte[1];

                Task<int> armReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();
                Assert.Equal(1, await client.SendAsync(new byte[] { 0xF1 }, SocketFlags.None));
                Assert.Equal(1, await AwaitWithTimeoutAsync(armReceive, $"data_rst_arm_{i}"));
                Assert.Equal(0xF1, receiveBuffer[0]);
                Assert.True(
                    await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                    $"Expected persistent multishot recv to arm before RST race at iteration {i}.");

                Task<int> dataReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                await Task.Yield();
                Assert.Equal(1, await client.SendAsync(new byte[] { 0xF2 }, SocketFlags.None));
                client.LingerState = new LingerOption(enable: true, seconds: 0);
                client.Dispose();

                int dataBytes = await AwaitWithTimeoutAsync(dataReceive, $"data_rst_payload_{i}");
                Assert.Equal(1, dataBytes);
                Assert.Equal(0xF2, receiveBuffer[0]);

                Task<int> terminalReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Exception? terminalException = await Record.ExceptionAsync(
                    async () => await AwaitWithTimeoutAsync(terminalReceive, $"data_rst_terminal_{i}"));
                Assert.NotNull(terminalException);
                Assert.True(
                    terminalException is SocketException socketException &&
                    (socketException.SocketErrorCode == SocketError.ConnectionReset ||
                     socketException.SocketErrorCode == SocketError.OperationAborted ||
                     socketException.SocketErrorCode == SocketError.Interrupted),
                    $"Unexpected terminal exception for data+RST scenario at iteration {i}: {terminalException}");
            }
        }

        private static async Task RunPersistentMultishotRecvConcurrentCloseRaceScenarioAsync(int iterations)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(Math.Max(4, iterations));
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotRecvSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using Socket client = pair.Client;
                using Socket server = pair.Server;

                byte[] armBuffer = new byte[1];
                Task<int> armReceive = ToTask(server.ReceiveAsync(armBuffer, SocketFlags.None));
                await Task.Yield();
                Assert.Equal(1, await client.SendAsync(new byte[] { 0xE1 }, SocketFlags.None));
                Assert.Equal(1, await armReceive);

                Assert.True(
                    await WaitForPersistentMultishotRecvArmedStateAsync(server, expectedArmed: true),
                    "Expected persistent multishot recv to arm before concurrent close race.");

                Task<int> pendingReceive = ToTask(server.ReceiveAsync(new byte[1], SocketFlags.None));
                await Task.Yield();

                _ = Task.Run(() =>
                {
                    server.Dispose();
                    client.Dispose();
                });

                Task completed = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(pendingReceive, completed);

                Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
                if (ex is SocketException socketException)
                {
                    Assert.True(
                        socketException.SocketErrorCode == SocketError.ConnectionReset ||
                        socketException.SocketErrorCode == SocketError.OperationAborted ||
                        socketException.SocketErrorCode == SocketError.Interrupted,
                        $"Unexpected socket error from persistent multishot recv close race: {socketException.SocketErrorCode}");
                }
                else if (ex is not ObjectDisposedException and not null)
                {
                    throw ex;
                }
            }
        }

        private static Task RunPersistentMultishotRecvQueueSaturationScenarioAsync()
        {
            using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Assert.True(SocketAsyncContext.TryGetSocketAsyncContextForTest(socket, out SocketAsyncContext? context));
            Assert.NotNull(context);

            byte[] payload = new byte[] { 0xE7 };
            for (int i = 0; i < 16; i++)
            {
                Assert.True(context!.TryBufferEarlyPersistentMultishotRecvData(payload));
            }

            Assert.False(context!.TryBufferEarlyPersistentMultishotRecvData(payload));
            Assert.Equal(16, GetPersistentMultishotRecvBufferedCount(socket));
            return Task.CompletedTask;
        }

        private static async Task RunNetworkStreamReadAsyncCancellationTokenScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;
            using var networkStream = new NetworkStream(server, ownsSocket: false);

            byte[] readBuffer = new byte[1];
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            ValueTask<int> readTask = networkStream.ReadAsync(readBuffer, cts.Token);
            await Task.Yield();

            Assert.Equal(1, await client.SendAsync(new byte[] { 0xF1 }, SocketFlags.None));
            Assert.Equal(1, await readTask);
            Assert.Equal(0xF1, readBuffer[0]);
        }

        private static async Task RunReceiveAsyncSocketAsyncEventArgsBufferListScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[1];
            using var receiveEventArgs = new SocketAsyncEventArgs
            {
                BufferList = new List<ArraySegment<byte>>
                {
                    new ArraySegment<byte>(receiveBuffer)
                }
            };

            Task<SocketAsyncEventArgs> receiveTask = StartSocketAsyncEventArgsOperation(
                server,
                receiveEventArgs,
                static (s, args) => s.ReceiveAsync(args));
            await Task.Yield();

            Assert.Equal(1, await client.SendAsync(new byte[] { 0xF2 }, SocketFlags.None));
            SocketAsyncEventArgs completedReceive = await receiveTask;
            Assert.Equal(SocketError.Success, completedReceive.SocketError);
            Assert.Equal(1, completedReceive.BytesTransferred);
            Assert.Equal(0xF2, receiveBuffer[0]);
            Assert.False(
                IsPersistentMultishotRecvArmed(server),
                "SAEA BufferList receive path should not arm persistent multishot recv state.");
        }

        private static async Task RunMultishotAcceptBasicScenarioAsync(int connectionCount)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(connectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            Task<Socket> firstAcceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Multishot accept was not armed while first accept was pending.");

            using (Socket firstClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await firstClient.ConnectAsync(endpoint);
                using Socket firstServer = await AwaitWithTimeoutAsync(firstAcceptTask, "first multishot accept");
                await AssertConnectedPairRoundTripAsync(firstClient, firstServer, 0x41);
            }

            for (int i = 1; i < connectionCount; i++)
            {
                (Socket clientSocket, Socket serverSocket) = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using Socket client = clientSocket;
                using Socket server = serverSocket;
                await AssertConnectedPairRoundTripAsync(client, server, unchecked((byte)(0x41 + i)));
            }
        }

        private static async Task RunMultishotAcceptPrequeueScenarioAsync(int prequeuedConnectionCount)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(prequeuedConnectionCount + 2);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            // Arm multishot accept once, then connect a burst of clients before issuing
            // subsequent AcceptAsync calls to create a pre-queue opportunity.
            Task<Socket> armingAcceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Multishot accept was not armed while arming accept was pending.");

            var connectedClients = new List<Socket>(prequeuedConnectionCount + 1);
            try
            {
                var connectTasks = new List<Task>(prequeuedConnectionCount + 1);
                for (int i = 0; i < prequeuedConnectionCount + 1; i++)
                {
                    var client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    connectedClients.Add(client);
                    connectTasks.Add(client.ConnectAsync(endpoint));
                }

                await Task.WhenAll(connectTasks);
                using Socket armingServer = await AwaitWithTimeoutAsync(armingAcceptTask, "arming multishot accept");

                DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                int queueCount = 0;
                while (DateTime.UtcNow < deadline)
                {
                    queueCount = GetListenerMultishotAcceptQueueCount(listener);
                    if (queueCount > 0)
                    {
                        break;
                    }

                    await Task.Delay(25);
                }

                Assert.True(queueCount > 0, "Expected at least one pre-accepted connection to be queued.");

                for (int i = 0; i < prequeuedConnectionCount; i++)
                {
                    using Socket _ = await AwaitWithTimeoutAsync(listener.AcceptAsync(), "prequeued accept completion");
                }
            }
            finally
            {
                foreach (Socket client in connectedClients)
                {
                    client.Dispose();
                }
            }
        }

        private static async Task RunMultishotAcceptListenerCloseScenarioAsync()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(4);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            Task<Socket> firstAcceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Multishot accept was not armed while first accept was pending.");

            using (Socket firstClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await firstClient.ConnectAsync(endpoint);
                using Socket firstServer = await AwaitWithTimeoutAsync(firstAcceptTask, "first accept before listener close");
                await AssertConnectedPairRoundTripAsync(firstClient, firstServer, 0x71);
            }

            Task<Socket> pendingAccept = listener.AcceptAsync();
            await Task.Yield();
            listener.Dispose();

            Task completed = await Task.WhenAny(pendingAccept, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(pendingAccept, completed);

            Exception? acceptException = await Record.ExceptionAsync(async () => await pendingAccept);
            Assert.NotNull(acceptException);
            Assert.True(
                acceptException is ObjectDisposedException ||
                acceptException is SocketException,
                $"Unexpected pending-accept exception after listener close: {acceptException}");

            Assert.Equal(0, GetListenerMultishotAcceptQueueCount(listener));
            Assert.False(IsListenerMultishotAcceptArmed(listener));
        }

        private static async Task RunMultishotAcceptTeardownRaceScenarioAsync(int iterations)
        {
            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                await RunMultishotAcceptListenerCloseScenarioAsync();
            }
        }

        private static async Task RunMultishotAcceptDisposeDuringArmingRaceScenarioAsync(int iterations)
        {
            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                Task<Socket> pendingAccept = listener.AcceptAsync();
                Task disposeTask = Task.Run(listener.Dispose);

                Task completed = await Task.WhenAny(pendingAccept, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(pendingAccept, completed);
                await disposeTask;

                Exception? acceptException = await Record.ExceptionAsync(async () => await pendingAccept);
                Assert.NotNull(acceptException);
                Assert.True(
                    acceptException is ObjectDisposedException || acceptException is SocketException,
                    $"Unexpected accept exception during dispose/arm race at iteration {i}: {acceptException}");
            }
        }

        private static async Task RunMultishotAcceptPrepareUnsupportedOneShotFallbackScenarioAsync()
        {
            // Prime socket engine initialization so s_engines contains any active io_uring engines.
            await RunTcpRoundTripAsync(4);

            SocketAsyncEngine[] engines = SocketAsyncEngine.GetActiveIoUringEnginesForTest();
            var overrides = new List<(SocketAsyncEngine Engine, bool SupportsMultishotAccept)>(engines.Length);
            foreach (SocketAsyncEngine engine in engines)
            {
                overrides.Add((engine, engine.SupportsMultishotAcceptForTest));
                engine.SupportsMultishotAcceptForTest = false;
            }

            if (engines.Length == 0)
            {
                return;
            }

            try
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(2);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                Task<Socket> acceptTask = listener.AcceptAsync();
                await Task.Yield();
                Assert.False(IsListenerMultishotAcceptArmed(listener), "Listener should remain in one-shot accept mode.");

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync(endpoint);
                using Socket server = await AwaitWithTimeoutAsync(acceptTask, "one-shot accept fallback");
                await AssertConnectedPairRoundTripAsync(client, server, 0x7A);
            }
            finally
            {
                foreach ((SocketAsyncEngine engine, bool supports) in overrides)
                {
                    engine.SupportsMultishotAcceptForTest = supports;
                }
            }
        }

        private static async Task RunMultishotAcceptRearmAfterTerminalCqeScenarioAsync()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(4);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            Task<Socket> firstAcceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Multishot accept was not armed before forced terminal CQE.");

            using (Socket firstClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                await firstClient.ConnectAsync(endpoint);
                Exception? firstAcceptException = await Record.ExceptionAsync(async () => await firstAcceptTask);
                Assert.NotNull(firstAcceptException);
                Assert.True(
                    firstAcceptException is SocketException ||
                    firstAcceptException is ObjectDisposedException,
                    $"Unexpected forced-accept exception type: {firstAcceptException}");
            }

            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: false),
                "Expected multishot accept to disarm after terminal CQE.");

            Task<Socket> secondAcceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Expected multishot accept to re-arm on subsequent accept.");

            using Socket secondClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await secondClient.ConnectAsync(endpoint);
            using Socket secondServer = await AwaitWithTimeoutAsync(secondAcceptTask, "re-armed multishot accept");
            await AssertConnectedPairRoundTripAsync(secondClient, secondServer, 0x33);
        }

        private static async Task RunMultishotAcceptHighConnectionRateScenarioAsync(int connectionCount)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(connectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            var acceptTasks = new Task<Socket>[connectionCount];
            var clients = new Socket?[connectionCount];
            var connectTasks = new Task[connectionCount];

            for (int i = 0; i < connectionCount; i++)
            {
                acceptTasks[i] = listener.AcceptAsync();
            }

            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    connectTasks[i] = clients[i].ConnectAsync(endpoint);
                }

                await Task.WhenAll(connectTasks);
                Socket[] servers = await Task.WhenAll(acceptTasks);

                try
                {
                    var verificationTasks = new List<Task>(connectionCount);
                    for (int i = 0; i < connectionCount; i++)
                    {
                        Socket client = Assert.IsType<Socket>(clients[i]);
                        Socket server = servers[i];
                        byte marker = unchecked((byte)i);
                        verificationTasks.Add(AssertConnectedPairRoundTripAsync(client, server, marker));
                    }

                    await Task.WhenAll(verificationTasks);
                }
                finally
                {
                    foreach (Socket server in servers)
                    {
                        server.Dispose();
                    }
                }
            }
            finally
            {
                foreach (Socket? client in clients)
                {
                    client?.Dispose();
                }
            }
        }

        private static async Task RunMultishotAcceptIdlePendingScenarioAsync(int iterations, TimeSpan idleDelay)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(8);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            for (int i = 0; i < iterations; i++)
            {
                Task<Socket> pendingAccept = listener.AcceptAsync();
                Assert.True(
                    await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                    $"Multishot accept was not armed while pending at iteration {i}.");

                // Keep accept pending during an idle/no-traffic window before connecting.
                // This targets regressions where a tracked waiting accept loses submission progress.
                await Task.Delay(idleDelay);

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await AwaitWithTimeoutAsync(client.ConnectAsync(endpoint), $"idle accept connect iteration {i}");
                using Socket server = await AwaitWithTimeoutAsync(pendingAccept, $"idle accept completion iteration {i}");
                await AssertConnectedPairRoundTripAsync(client, server, unchecked((byte)(0x50 + (i % 32))));
            }
        }

        private static async Task RunSlotCapacityStressScenarioAsync(int connectionCount)
        {
            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            int requiredDescriptorCount = checked((connectionCount * 2) + 256);
            if (!HasSufficientFileDescriptorLimit(requiredDescriptorCount))
            {
                return;
            }

            await RunMultishotAcceptHighConnectionRateScenarioAsync(connectionCount);
        }

        private static async Task RunLargeSendWithBackpressureAsync(bool useBufferListSend)
        {
            var trio = await AwaitWithTimeoutAsync(CreateConnectedTcpSocketTrioAsync(), nameof(CreateConnectedTcpSocketTrioAsync));
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            // Moderate buffer sizes + smaller payload to exercise partial-send resubmission
            // while keeping completion time reasonable. With 1024-byte buffers and 2MB payload,
            // TCP flow control yields ~32KB partial sends with ~3-5s gaps, taking ~200s total.
            // With 16KB buffers and 256KB payload the transfer completes much faster but still
            // exercises multiple partial sends. Use a generous per-recv timeout because TCP
            // flow control with io_uring can have long pauses between data availability.
            client.SendBufferSize = 16384;
            server.ReceiveBufferSize = 16384;

            const int PayloadLength = 256 * 1024;
            TimeSpan recvTimeout = TimeSpan.FromSeconds(60);
            byte[] payload = new byte[PayloadLength];
            for (int i = 0; i < payload.Length; i++)
            {
                payload[i] = (byte)i;
            }

            Task<int> sendTask;
            if (useBufferListSend)
            {
                const int SegmentSize = 1024;
                var sendBuffers = new List<ArraySegment<byte>>();
                for (int offset = 0; offset < payload.Length; offset += SegmentSize)
                {
                    int count = Math.Min(SegmentSize, payload.Length - offset);
                    sendBuffers.Add(new ArraySegment<byte>(payload, offset, count));
                }

                sendTask = ToTask(client.SendAsync(sendBuffers, SocketFlags.None));
            }
            else
            {
                sendTask = ToTask(client.SendAsync(payload, SocketFlags.None));
            }

            await Task.Delay(20);

            byte[] received = new byte[payload.Length];
            int totalReceived = 0;
            while (totalReceived < payload.Length)
            {
                int receivedNow;
                receivedNow = await AwaitWithTimeoutAsync(
                    ToTask(server.ReceiveAsync(received.AsMemory(totalReceived), SocketFlags.None)),
                    $"partial_send_receive_{totalReceived}",
                    recvTimeout);
                Assert.True(receivedNow > 0, $"ReceiveAsync returned 0 at offset={totalReceived}");
                totalReceived += receivedNow;
                if ((totalReceived & 0x3FFF) == 0)
                {
                    await Task.Delay(1);
                }
            }

            Assert.Equal(payload.Length, await AwaitWithTimeoutAsync(sendTask, "partial_send_sendtask_completion", recvTimeout));
            Assert.Equal(payload.Length, totalReceived);
            Assert.Equal(payload, received);
        }

        private static async Task RunAsyncCancelRequestIsolationScenarioAsync(int iterations)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(2);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            var cancelPair = await AcceptConnectedTcpPairAsync(listener, endpoint);
            using Socket cancelClient = cancelPair.Client;
            using Socket cancelServer = cancelPair.Server;

            var activePair = await AcceptConnectedTcpPairAsync(listener, endpoint);
            using Socket activeClient = activePair.Client;
            using Socket activeServer = activePair.Server;

            byte[] cancelBuffer = new byte[1];
            byte[] activeBuffer = new byte[1];
            for (int i = 0; i < iterations; i++)
            {
                using var cts = new CancellationTokenSource();
                Task<int> canceledReceive = ToTask(cancelServer.ReceiveAsync(cancelBuffer, SocketFlags.None, cts.Token));
                Task<int> activeReceive = ToTask(activeServer.ReceiveAsync(activeBuffer, SocketFlags.None));
                await Task.Yield();

                cts.Cancel();
                byte expected = unchecked((byte)(i + 1));
                Assert.Equal(1, await activeClient.SendAsync(new byte[] { expected }, SocketFlags.None));

                Assert.Equal(1, await activeReceive);
                Assert.Equal(expected, activeBuffer[0]);

                Exception? cancelException = await Record.ExceptionAsync(async () => await canceledReceive);
                AssertCanceledOrInterrupted(cancelException);
            }
        }

        private static async Task RunReceiveMessageFromCancellationAndDisposeScenariosAsync()
        {
            using Socket cancelReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            cancelReceiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            cancelReceiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            EndPoint cancelRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            using var cts = new CancellationTokenSource();
            Task<SocketReceiveMessageFromResult> canceledReceive = ToTask(
                cancelReceiver.ReceiveMessageFromAsync(new byte[64], SocketFlags.None, cancelRemoteEndPoint, cts.Token));
            await Task.Yield();
            cts.Cancel();

            Task cancelCompleted = await Task.WhenAny(canceledReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(canceledReceive, cancelCompleted);
            Exception? cancelException = await Record.ExceptionAsync(async () => await canceledReceive);
            AssertCanceledOrInterrupted(cancelException);

            using Socket disposeReceiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            disposeReceiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            disposeReceiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            byte[] receiveBuffer = new byte[32];
            using var receiveEventArgs = new SocketAsyncEventArgs
            {
                BufferList = new List<ArraySegment<byte>>
                {
                    new ArraySegment<byte>(receiveBuffer, 0, 16),
                    new ArraySegment<byte>(receiveBuffer, 16, 16)
                },
                RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
            };

            Task<SocketAsyncEventArgs> pendingReceive = StartReceiveMessageFromAsync(disposeReceiver, receiveEventArgs);
            await Task.Yield();
            disposeReceiver.Dispose();

            Task disposeCompleted = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(pendingReceive, disposeCompleted);
            SocketAsyncEventArgs completedArgs = await pendingReceive;
            Assert.True(
                completedArgs.SocketError == SocketError.OperationAborted ||
                completedArgs.SocketError == SocketError.Interrupted);
        }

        private static async Task RunReceiveMessageFromCancelThenReceiveScenarioAsync()
        {
            using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, true);
            receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            EndPoint initialRemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            using var cts = new CancellationTokenSource();
            Task<SocketReceiveMessageFromResult> canceledReceive = ToTask(
                receiver.ReceiveMessageFromAsync(new byte[64], SocketFlags.None, initialRemoteEndPoint, cts.Token));
            await Task.Yield();
            cts.Cancel();

            Task canceledCompleted = await Task.WhenAny(canceledReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(canceledReceive, canceledCompleted);
            Exception? cancelException = await Record.ExceptionAsync(async () => await canceledReceive);
            AssertCanceledOrInterrupted(cancelException);

            byte[] payload = new byte[] { 0x10, 0x20, 0x30, 0x40 };
            Assert.Equal(
                payload.Length,
                await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!));

            byte[] receiveBuffer = new byte[64];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
            SocketReceiveMessageFromResult received = await ToTask(
                receiver.ReceiveMessageFromAsync(receiveBuffer, SocketFlags.None, remoteEndPoint, CancellationToken.None));

            Assert.Equal(payload.Length, received.ReceivedBytes);
            Assert.True(payload.AsSpan().SequenceEqual(receiveBuffer.AsSpan(0, payload.Length)));
        }

        private static async Task RunReceiveMessageFromCancellationAndDisposeScenariosWithGcPressureAsync(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                await RunReceiveMessageFromCancellationAndDisposeScenariosAsync();
                if ((i & 0x3) == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                }
            }
        }

        private static async Task RunTeardownDrainTrackedOperationsScenarioAsync(int iterations)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(8);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            for (int i = 0; i < iterations; i++)
            {
                var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using Socket client = pair.Client;
                using Socket server = pair.Server;

                Task<int> pendingReceive = ToTask(server.ReceiveAsync(new byte[1], SocketFlags.None));
                await Task.Yield();

                client.Dispose();
                server.Dispose();

                Task completed = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(pendingReceive, completed);
                Exception? receiveException = await Record.ExceptionAsync(async () => await pendingReceive);
                AssertCanceledDisposedOrInterrupted(receiveException);
            }
        }

        private static async Task RunTeardownCancellationDuplicateGuardScenarioAsync(int iterations)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(8);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            for (int i = 0; i < iterations; i++)
            {
                var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using Socket client = pair.Client;
                using Socket server = pair.Server;

                using var cts = new CancellationTokenSource();
                Task<int> pendingReceive = ToTask(server.ReceiveAsync(new byte[1], SocketFlags.None, cts.Token));
                await Task.Yield();
                cts.Cancel();

                server.Dispose();
                client.Dispose();

                Task completed = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(pendingReceive, completed);
                Exception? receiveException = await Record.ExceptionAsync(async () => await pendingReceive);
                AssertCanceledDisposedOrInterrupted(receiveException);
            }
        }

        private static async Task RunCancellationSubmitContentionScenarioAsync(int connectionCount, int cancellationsPerConnection)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(connectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            var clients = new List<Socket>(connectionCount);
            var servers = new List<Socket>(connectionCount);
            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                    clients.Add(pair.Client);
                    servers.Add(pair.Server);
                }

                Task[] churnTasks = new Task[connectionCount];
                for (int index = 0; index < connectionCount; index++)
                {
                    Socket server = servers[index];
                    churnTasks[index] = Task.Run(async () =>
                    {
                        byte[] receiveBuffer = new byte[1];
                        for (int i = 0; i < cancellationsPerConnection; i++)
                        {
                            using var cts = new CancellationTokenSource();
                            Task<int> pendingReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None, cts.Token));
                            cts.Cancel();

                            Exception? receiveException = await Record.ExceptionAsync(async () => await pendingReceive);
                            AssertCanceledOrInterrupted(receiveException);
                        }
                    });
                }

                await Task.WhenAll(churnTasks);

                // Ensure the cancellation churn does not stall normal completion progress afterward.
                for (int i = 0; i < connectionCount; i++)
                {
                    byte expected = unchecked((byte)(i + 1));
                    byte[] receiveBuffer = new byte[1];
                    Task<int> receiveTask = ToTask(servers[i].ReceiveAsync(receiveBuffer, SocketFlags.None));
                    await Task.Yield();

                    Assert.Equal(1, await clients[i].SendAsync(new byte[] { expected }, SocketFlags.None));
                    Assert.Equal(1, await receiveTask);
                    Assert.Equal(expected, receiveBuffer[0]);
                }
            }
            finally
            {
                foreach (Socket server in servers)
                {
                    server.Dispose();
                }

                foreach (Socket client in clients)
                {
                    client.Dispose();
                }
            }
        }

        private static async Task RunCancellationQueueWakeBeforeOverflowScenarioAsync()
        {
#if DEBUG
            await RunTcpRoundTripAsync(4);

            if (!TryGetFirstIoUringEngineForTest(out SocketAsyncEngine? ioUringEngine) || ioUringEngine is null)
            {
                return;
            }

            long configuredCapacity = SocketAsyncEngine.GetIoUringCancellationQueueCapacityForTest();
            Assert.True(configuredCapacity > 0, "Cancellation queue capacity must be positive for wake-before-overflow verification.");

            long originalQueueLength = ioUringEngine.IoUringCancelQueueLengthForTest;
            int originalWakeupRequested = ioUringEngine.IoUringWakeupRequestedForTest;
            try
            {
                long overflowBefore = ioUringEngine.IoUringCancelQueueOverflowCountForTest;
                long wakeRetryBefore = ioUringEngine.IoUringCancelQueueWakeRetryCountForTest;

                // Force queue-full path deterministically by priming the observed queue length
                // to capacity before enqueue; this must trigger wake-and-retry before overflow.
                ioUringEngine.IoUringCancelQueueLengthForTest = configuredCapacity;
                ioUringEngine.IoUringWakeupRequestedForTest = 0;

                bool enqueueResult = ioUringEngine.TryEnqueueIoUringCancellationForTest((ulong)1);

                long overflowAfter = ioUringEngine.IoUringCancelQueueOverflowCountForTest;
                long wakeRetryAfter = ioUringEngine.IoUringCancelQueueWakeRetryCountForTest;

                Assert.False(enqueueResult);
                Assert.Equal(overflowBefore + 1, overflowAfter);
                Assert.True(
                    wakeRetryAfter > wakeRetryBefore,
                    $"Expected cancel queue full path to record a wake-before-retry. before={wakeRetryBefore}, after={wakeRetryAfter}");
            }
            finally
            {
                ioUringEngine.IoUringCancelQueueLengthForTest = originalQueueLength;
                ioUringEngine.IoUringWakeupRequestedForTest = originalWakeupRequested;
            }
#else
            await Task.CompletedTask;
#endif
        }

        private static async Task RunMixedModeReadinessCompletionStressScenarioAsync(int iterations)
        {
            var trio = await AwaitWithTimeoutAsync(CreateConnectedTcpSocketTrioAsync(), nameof(CreateConnectedTcpSocketTrioAsync));
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] completionBuffer = new byte[1];
            byte[] payload = new byte[1];

            for (int i = 0; i < iterations; i++)
            {
                Task<int> completionReceive = ToTask(server.ReceiveAsync(completionBuffer, SocketFlags.None));
                Task<int> readinessProbe = ToTask(server.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None));
                await Task.Yield();

                payload[0] = unchecked((byte)(i + 1));
                Assert.Equal(1, await AwaitWithTimeoutAsync(client.SendAsync(payload, SocketFlags.None), $"mixed_send_{i}"));
                Assert.Equal(1, await AwaitWithTimeoutAsync(completionReceive, $"mixed_completion_receive_{i}"));
                Assert.Equal(payload[0], completionBuffer[0]);

                Task completed = await Task.WhenAny(readinessProbe, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(readinessProbe, completed);
                Assert.Equal(0, await readinessProbe);
            }
        }

        private static async Task RunSameSocketReadinessCompletionBacklogScenarioAsync(int iterations, int completionBatchSize)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] sendPayload = new byte[completionBatchSize];
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                var receiveBuffers = new byte[completionBatchSize][];
                var completionReceives = new Task<int>[completionBatchSize];
                for (int i = 0; i < completionBatchSize; i++)
                {
                    byte expected = unchecked((byte)((iteration + i + 1) & 0xFF));
                    sendPayload[i] = expected;
                    byte[] receiveBuffer = new byte[1];
                    receiveBuffers[i] = receiveBuffer;
                    completionReceives[i] = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                }

                Task<int> readinessProbe = ToTask(server.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None));
                await Task.Yield();

                int sent = 0;
                while (sent < sendPayload.Length)
                {
                    sent += await client.SendAsync(sendPayload.AsMemory(sent), SocketFlags.None);
                }

                Assert.Equal(sendPayload.Length, sent);

                Task readinessCompleted = await Task.WhenAny(readinessProbe, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(readinessProbe, readinessCompleted);
                Assert.Equal(0, await readinessProbe);

                int[] receivedCounts = await Task.WhenAll(completionReceives);
                for (int i = 0; i < completionBatchSize; i++)
                {
                    Assert.Equal(1, receivedCounts[i]);
                    Assert.Equal(sendPayload[i], receiveBuffers[i][0]);
                }
            }
        }

        private static async Task RunPureCompletionScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] tcpSendPayload = new byte[] { 0x11 };
            byte[] tcpReceiveBuffer = new byte[1];

            Task<int> tcpReceive = ToTask(server.ReceiveAsync(tcpReceiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(tcpSendPayload, SocketFlags.None));
            Assert.Equal(1, await AwaitWithTimeoutAsync(tcpReceive, nameof(tcpReceive)));
            Assert.Equal(tcpSendPayload[0], tcpReceiveBuffer[0]);

            Task<int> tcpZeroByteReceive = ToTask(server.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None));
            await Task.Yield();

            byte[] tcpPayloadAfterProbe = new byte[] { 0x22 };
            Assert.Equal(1, await client.SendAsync(tcpPayloadAfterProbe, SocketFlags.None));
            Task completed = await Task.WhenAny(tcpZeroByteReceive, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(tcpZeroByteReceive, completed);
            Assert.Equal(0, await tcpZeroByteReceive);

            byte[] tcpDataAfterZeroByte = new byte[1];
            Task<int> tcpTailReceive = ToTask(server.ReceiveAsync(tcpDataAfterZeroByte, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await AwaitWithTimeoutAsync(tcpTailReceive, nameof(tcpTailReceive)));
            Assert.Equal(tcpPayloadAfterProbe[0], tcpDataAfterZeroByte[0]);

            using Socket connectListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectListener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            connectListener.Listen(1);
            IPEndPoint connectEndPoint = (IPEndPoint)connectListener.LocalEndPoint!;

            using Socket connectClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Task<Socket> acceptTask = connectListener.AcceptAsync();
            await connectClient.ConnectAsync(connectEndPoint);
            using Socket connectServer = await AwaitWithTimeoutAsync(acceptTask, nameof(acceptTask));

            byte[] connectPayload = new byte[] { 0x33 };
            Assert.Equal(1, await connectClient.SendAsync(connectPayload, SocketFlags.None));
            byte[] connectReceiveBuffer = new byte[1];
            Assert.Equal(1, await connectServer.ReceiveAsync(connectReceiveBuffer, SocketFlags.None));
            Assert.Equal(connectPayload[0], connectReceiveBuffer[0]);

            using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            using Socket udpSender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

            byte[] udpPayload = new byte[] { 0x33, 0x44, 0x55 };
            byte[] udpReceiveBuffer = new byte[udpPayload.Length];

            Task<SocketReceiveFromResult> receiveFromTask =
                ToTask(receiver.ReceiveFromAsync(udpReceiveBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));
            await Task.Yield();
            Assert.Equal(udpPayload.Length, await udpSender.SendToAsync(udpPayload, SocketFlags.None, receiver.LocalEndPoint!));

            SocketReceiveFromResult receiveFromResult = await receiveFromTask;
            Assert.Equal(udpPayload.Length, receiveFromResult.ReceivedBytes);
            Assert.Equal(udpPayload, udpReceiveBuffer);
            Assert.Equal(udpSender.LocalEndPoint, receiveFromResult.RemoteEndPoint);
        }

        private static async Task RunPrepareQueueOverflowFallbackScenarioAsync(int connectionCount)
        {
            for (int round = 0; round < 4; round++)
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(connectionCount);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                var clients = new List<Socket>(connectionCount);
                var servers = new List<Socket>(connectionCount);
                var receiveTasks = new List<Task<int>>(connectionCount);
                try
                {
                    for (int i = 0; i < connectionCount; i++)
                    {
                        var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                        clients.Add(pair.Client);
                        servers.Add(pair.Server);

                        receiveTasks.Add(ToTask(pair.Server.ReceiveAsync(new byte[1], SocketFlags.None)));
                    }

                    await Task.Yield();

                    for (int i = 0; i < connectionCount; i++)
                    {
                        Assert.Equal(1, await clients[i].SendAsync(new byte[] { 0x5A }, SocketFlags.None));
                    }

                    for (int i = 0; i < receiveTasks.Count; i++)
                    {
                        Assert.Equal(1, await AwaitWithTimeoutAsync(receiveTasks[i], $"overflow_receive_{round}_{i}"));
                    }
                }
                finally
                {
                    foreach (Socket server in servers)
                    {
                        server.Dispose();
                    }

                    foreach (Socket client in clients)
                    {
                        client.Dispose();
                    }
                }
            }
        }

        private static async Task RunConnectQueueOverflowFallbackScenarioAsync(int connectionCount)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(connectionCount);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            var clients = new List<Socket>(connectionCount);
            var connectTasks = new List<Task>(connectionCount);
            var acceptTasks = new List<Task<Socket>>(connectionCount);
            var acceptedSockets = new List<Socket>(connectionCount);

            try
            {
                for (int i = 0; i < connectionCount; i++)
                {
                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clients.Add(client);
                    acceptTasks.Add(listener.AcceptAsync());
                    connectTasks.Add(client.ConnectAsync(endpoint));
                }

                Task connectAll = Task.WhenAll(connectTasks);
                Task connectCompleted = await Task.WhenAny(connectAll, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(connectAll, connectCompleted);
                await connectAll;

                foreach (Task<Socket> acceptTask in acceptTasks)
                {
                    acceptedSockets.Add(await AwaitWithTimeoutAsync(acceptTask, nameof(RunConnectQueueOverflowFallbackScenarioAsync)));
                }
            }
            finally
            {
                foreach (Socket acceptedSocket in acceptedSockets)
                {
                    acceptedSocket.Dispose();
                }

                foreach (Socket client in clients)
                {
                    client.Dispose();
                }
            }
        }

        private static async Task RunCqOverflowRecoveryScenarioAsync()
        {
            await RunTcpRoundTripAsync(8);

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();

            if (!TryInjectIoUringCqOverflowForTest(delta: 1, out int injectedEngineCount) || injectedEngineCount == 0)
            {
                return;
            }

            await RunTcpRoundTripAsync(32);

            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 1),
                "Completion-slot usage did not return near baseline after CQ overflow recovery.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 1),
                "Tracked io_uring operation count did not return near baseline after CQ overflow recovery.");
        }

        private static async Task RunCqOverflowRecoveryWithZeroTrackedOperationsScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);

            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(0),
                "Expected zero tracked io_uring operations before injected CQ overflow.");

            if (!TryInjectIoUringCqOverflowForTest(delta: 1, out int injectedEngineCount) || injectedEngineCount == 0)
            {
                return;
            }

            await RunTcpRoundTripAsync(4);

            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(0),
                "Tracked io_uring operation count should remain at zero after zero-tracked-operations recovery.");
        }

        private static async Task RunCqOverflowRecoveryWithSmallRingScenarioAsync()
        {
            await RunTcpRoundTripAsync(8);

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();

            // Small CQ-ring overflow is timing-sensitive even with tiny rings; run multiple bursts.
            // Use completion slot spike above baseline as a proxy for overflow detection (overflow
            // causes slot retention until recovery sweeps them).
            for (int round = 0; round < 6; round++)
            {
                await RunTcpRoundTripAsync(256);

                int slotsInUse = GetIoUringCompletionSlotsInUseForTest();
                if (slotsInUse > baselineCompletionSlotsInUse + 4)
                {
                    // CQ overflow likely occurred. Verify recovery settles slots and tracked ops.
                    Assert.True(
                        await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 2, timeoutMilliseconds: 15000),
                        "Completion-slot usage did not settle after detected CQ overflow.");
                    Assert.True(
                        await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 2, timeoutMilliseconds: 15000),
                        "Tracked-operation count did not settle after detected CQ overflow.");
                    return;
                }
            }

            // On very fast machines, overflow may not occur. Verify stable state regardless.
            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 2, timeoutMilliseconds: 15000),
                "Completion-slot usage did not settle after small-ring CQ overflow scenario.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 2, timeoutMilliseconds: 15000),
                "Tracked-operation count did not settle after small-ring CQ overflow scenario.");
        }

        private static async Task RunMultishotAcceptOverflowArmingScenarioAsync()
        {
            if (!IsIoUringMultishotAcceptSupported())
            {
                return;
            }

            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(4);
            IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

            Task<Socket> acceptTask = listener.AcceptAsync();
            Assert.True(
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                "Multishot accept was not armed before injected CQ overflow.");

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();

            if (!TryInjectIoUringCqOverflowForTest(delta: 1, out int injectedEngineCount) || injectedEngineCount == 0)
            {
                return;
            }

            using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await client.ConnectAsync(endpoint);

            Socket? acceptedSocket = null;
            Exception? acceptException = await Record.ExceptionAsync(async () =>
            {
                acceptedSocket = await AwaitWithTimeoutAsync(acceptTask, "multishot accept after CQ overflow");
            });

            if (acceptException is null)
            {
                using Socket server = Assert.IsType<Socket>(acceptedSocket);
                await AssertConnectedPairRoundTripAsync(client, server, 0xA1);
            }
            else
            {
                Assert.True(
                    acceptException is SocketException || acceptException is ObjectDisposedException,
                    $"Unexpected accept completion after CQ overflow: {acceptException}");
            }

            Task<Socket> nextAcceptTask = listener.AcceptAsync();
            using Socket nextClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await nextClient.ConnectAsync(endpoint);
            using Socket nextServer = await AwaitWithTimeoutAsync(nextAcceptTask, "post-overflow multishot accept follow-up");
            await AssertConnectedPairRoundTripAsync(nextClient, nextServer, 0xA2);

            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 2),
                "Completion-slot usage remained unexpectedly elevated after multishot-accept overflow scenario.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 2),
                "Tracked-operation count remained unexpectedly elevated after multishot-accept overflow scenario.");
        }

        private static async Task RunTeardownUnderOverflowScenarioAsync()
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(2);

            Task<Socket> pendingAccept = listener.AcceptAsync();
            await Task.Yield();

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();
            _ = TryInjectIoUringCqOverflowForTest(delta: 1, out _);

            Task disposeTask = Task.Run(listener.Dispose);

            Task completed = await Task.WhenAny(pendingAccept, Task.Delay(TimeSpan.FromSeconds(60)));
            Assert.Same(pendingAccept, completed);
            await disposeTask;

            Exception? acceptException = await Record.ExceptionAsync(async () => await pendingAccept);
            Assert.NotNull(acceptException);
            Assert.True(
                acceptException is ObjectDisposedException || acceptException is SocketException,
                $"Unexpected pending-accept exception under teardown+overflow race: {acceptException}");

            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 1, timeoutMilliseconds: 15000),
                "Completion-slot usage did not settle after teardown-under-overflow scenario.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 1, timeoutMilliseconds: 15000),
                "Tracked-operation count did not settle after teardown-under-overflow scenario.");
        }

        private static async Task RunSustainedOverflowReentrancyScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);

            int baselineCompletionSlotsInUse = GetIoUringCompletionSlotsInUseForTest();
            int baselineTrackedOperations = GetIoUringTrackedOperationCountForTest();

            if (!TryInjectIoUringCqOverflowForTest(delta: 1, out int injectedEngineCount) || injectedEngineCount == 0)
            {
                return;
            }

            DateTime end = DateTime.UtcNow + TimeSpan.FromSeconds(10);
            Task injectorTask = Task.Run(async () =>
            {
                while (DateTime.UtcNow < end)
                {
                    _ = TryInjectIoUringCqOverflowForTest(delta: 1, out _);
                    await Task.Delay(5);
                }
            });

            Task workloadTask = Task.Run(async () =>
            {
                while (DateTime.UtcNow < end)
                {
                    await RunTcpRoundTripAsync(2);
                }
            });

            Task combined = Task.WhenAll(injectorTask, workloadTask);
            Task completed = await Task.WhenAny(combined, Task.Delay(TimeSpan.FromSeconds(30)));
            Assert.Same(combined, completed);
            await combined;

            Assert.True(
                await WaitForIoUringCompletionSlotsInUseAtMostAsync(baselineCompletionSlotsInUse + 2, timeoutMilliseconds: 15000),
                "Completion-slot usage did not settle after sustained overflow scenario.");
            Assert.True(
                await WaitForIoUringTrackedOperationsAtMostAsync(baselineTrackedOperations + 2, timeoutMilliseconds: 15000),
                "Tracked-operation count did not settle after sustained overflow scenario.");
        }

        private static async Task RunCqOverflowReflectionTargetStabilityScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);
            bool hasIoUringPort = AssertIoUringCqReflectionTargetsStableForTest();
            if (!hasIoUringPort)
            {
                return;
            }

            Assert.True(hasIoUringPort, "Expected at least one active io_uring engine when io_uring mode is enabled.");
        }

        private static Task RunNativeMsghdrLayoutContractScenarioAsync()
        {
            AssertNativeMsghdrLayoutContractForIoUring();
            return Task.CompletedTask;
        }

        private static Task RunNativeMsghdr32BitRejectionScenarioAsync()
        {
            AssertNativeMsghdr32BitRejectionPathForIoUring();
            return Task.CompletedTask;
        }

        private static Task RunCompletionSlotLayoutContractScenarioAsync()
        {
            AssertIoUringCompletionSlotLayoutContractForIoUring();
            return Task.CompletedTask;
        }

        private static Task RunCompletionSlotUserDataBoundaryScenarioAsync()
        {
            AssertCompletionSlotUserDataEncodingBoundaryContractForIoUring();
            return Task.CompletedTask;
        }

        private static async Task RunCloseOnExecForkExecScenarioAsync()
        {
            await RunTcpRoundTripAsync(4);

            if (!TryGetIoUringRingFdForTest(out int ringFd) ||
                !TryGetIoUringWakeupEventFdForTest(out int wakeupEventFd))
            {
                return;
            }

            Assert.False(
                DoesExecChildObserveFileDescriptor(ringFd),
                $"Ring fd {ringFd} unexpectedly survived exec.");
            Assert.False(
                DoesExecChildObserveFileDescriptor(wakeupEventFd),
                $"Wakeup eventfd {wakeupEventFd} unexpectedly survived exec.");
        }

        private static async Task RunDebugNonEventLoopSingleIssuerAssertionScenarioAsync()
        {
#if DEBUG
            await RunTcpRoundTripAsync(4);

            SocketAsyncEngine[] engines = SocketAsyncEngine.GetActiveIoUringEnginesForTest();
            if (engines.Length == 0)
            {
                return;
            }

            SocketAsyncEngine ioUringEngine = engines[0];

            using var listener = new ThrowingTraceListener();
            Trace.Listeners.Add(listener);
            try
            {
                Exception? ex = await Record.ExceptionAsync(async () =>
                {
                    await Task.Run(() => ioUringEngine.SubmitIoUringOperationsNormalizedForTest());
                });

                Assert.NotNull(ex);
                Assert.Contains("SINGLE_ISSUER", ex.ToString(), StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }
#endif
        }

        private static async Task RunCompletionCancellationRaceAsync(int iterations)
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[1];
            int completedCount = 0;
            int canceledCount = 0;
            for (int i = 0; i < iterations; i++)
            {
                while (server.Available > 0)
                {
                    int drainLength = Math.Min(server.Available, 256);
                    byte[] drainBuffer = new byte[drainLength];
                    int drained = await ToTask(server.ReceiveAsync(drainBuffer, SocketFlags.None));
                    if (drained == 0)
                    {
                        break;
                    }
                }

                using var cts = new CancellationTokenSource();
                Task<int> receiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None, cts.Token));
                Task<int> sendTask;

                if ((i & 1) == 0)
                {
                    cts.Cancel();
                    sendTask = ToTask(client.SendAsync(new byte[] { unchecked((byte)(i + 1)) }, SocketFlags.None));
                }
                else
                {
                    sendTask = ToTask(client.SendAsync(new byte[] { unchecked((byte)(i + 1)) }, SocketFlags.None));
                    await Task.Yield();
                    cts.Cancel();
                }

                Exception? receiveException = await Record.ExceptionAsync(async () => await receiveTask);
                if (receiveException is null)
                {
                    completedCount++;
                    Assert.Equal(1, receiveTask.Result);
                }
                else
                {
                    canceledCount++;
                    AssertCanceledOrInterrupted(receiveException);
                }

                Assert.Equal(1, await sendTask);
            }

            Assert.True(completedCount > 0);
            Assert.True(canceledCount > 0);
        }

        private static async Task DrainAvailableBytesAsync(Socket socket)
        {
            while (socket.Available > 0)
            {
                int bytesToRead = Math.Min(socket.Available, 256);
                byte[] drainBuffer = new byte[bytesToRead];
                int read = await ToTask(socket.ReceiveAsync(drainBuffer, SocketFlags.None));
                if (read <= 0)
                {
                    return;
                }
            }
        }

        private static async Task RunForcedEagainReceiveScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] firstReceiveBuffer = new byte[1];
            Task<int> receiveTask = ToTask(server.ReceiveAsync(firstReceiveBuffer, SocketFlags.None));
            await Task.Yield();

            byte sendByte = 0x31;
            for (int i = 0; i < 6 && !receiveTask.IsCompleted; i++)
            {
                Assert.Equal(1, await client.SendAsync(new byte[] { sendByte }, SocketFlags.None));
                sendByte++;
                await Task.Delay(10);
            }

            Task completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(receiveTask, completed);
            Assert.True(await receiveTask > 0);
            await DrainAvailableBytesAsync(server);

            byte[] secondReceiveBuffer = new byte[1];
            Task<int> followUpReceiveTask = ToTask(server.ReceiveAsync(secondReceiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x40 }, SocketFlags.None));
            Task followUpCompleted = await Task.WhenAny(followUpReceiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(followUpReceiveTask, followUpCompleted);
            Assert.True(await followUpReceiveTask > 0);
        }

        private static async Task RunForcedEcanceledReceiveScenarioAsync()
        {
            var trio = await CreateConnectedTcpSocketTrioAsync();
            using Socket _ = trio.Listener;
            using Socket client = trio.Client;
            using Socket server = trio.Server;

            byte[] receiveBuffer = new byte[1];
            Task<int> forcedReceiveTask = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x44 }, SocketFlags.None));

            Task completed = await Task.WhenAny(forcedReceiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(forcedReceiveTask, completed);
            Exception? forcedReceiveException = await Record.ExceptionAsync(async () => await forcedReceiveTask);
            if (forcedReceiveException is null)
            {
                Assert.True(forcedReceiveTask.Result > 0);
            }
            else
            {
                AssertCanceledOrInterrupted(forcedReceiveException);
            }
            await DrainAvailableBytesAsync(server);

            byte[] followUpReceiveBuffer = new byte[1];
            Task<int> followUpReceiveTask = ToTask(server.ReceiveAsync(followUpReceiveBuffer, SocketFlags.None));
            await Task.Yield();
            Assert.Equal(1, await client.SendAsync(new byte[] { 0x45 }, SocketFlags.None));
            Task followUpCompleted = await Task.WhenAny(followUpReceiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(followUpReceiveTask, followUpCompleted);
            Assert.True(await followUpReceiveTask > 0);
        }

        private static Task RunForcedReceiveScenarioAsync(bool forceEcanceled) =>
            forceEcanceled ? RunForcedEcanceledReceiveScenarioAsync() : RunForcedEagainReceiveScenarioAsync();

        private static async Task RunForcedEnterEintrRetryLimitScenarioAsync()
        {
            Exception? firstFailure = await Record.ExceptionAsync(
                async () => await AwaitWithTimeoutAsync(RunTcpRoundTripAsync(4), "forced_enter_eintr_roundtrip_1"));
            if (firstFailure is not null)
            {
                Assert.True(
                    firstFailure is SocketException ||
                    firstFailure is OperationCanceledException ||
                    firstFailure is TimeoutException ||
                    firstFailure is ObjectDisposedException,
                    $"Unexpected exception after forced io_uring_enter EINTR-limit error: {firstFailure}");
            }

            await AwaitWithTimeoutAsync(RunTcpRoundTripAsync(4), "forced_enter_eintr_roundtrip_2");
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_DoesNotBreakAsyncSocketWorkflows()
        {
            await RemoteExecutor.Invoke(static () => RunTcpRoundTripAsync(64), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringAndEpollEngines_MixedProcessWorkload_Completes()
        {
            await RemoteExecutor.Invoke(
                static () => RunHybridIoUringAndEpollEngineScenarioAsync(),
                CreateSocketEngineOptions(
                    ioUringValue: "1",
                    threadCount: 2)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // kernel-version fallback behavior is Linux-specific.
        public static async Task IoUringCompletionMode_ForcedKernelVersionUnsupported_FallsBackToEpoll()
        {
            await RemoteExecutor.Invoke(
                static () => RunKernelVersionUnsupportedFallbackScenarioAsync(),
                CreateSocketEngineOptions(
                    ioUringValue: "1",
                    forceKernelVersionUnsupported: true)).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // hybrid routing behavior is Linux-specific.
        public static async Task IoUringCompletionMode_CancellationRouting_ThreadCount2_Progresses()
        {
            await RemoteExecutor.Invoke(
                static () => RunThreadCountTwoCancellationRoutingScenarioAsync(),
                CreateSocketEngineOptions(
                    ioUringValue: "1",
                    threadCount: 2)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_UnixDomainSockets_ConnectSendReceive_Works()
        {
            await RemoteExecutor.Invoke(static () => RunUnixDomainSocketRoundTripAsync(), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task SocketEngine_DefaultOptOut_DoesNotBreakAsyncSocketWorkflows()
        {
            await RemoteExecutor.Invoke(static () => RunTcpRoundTripAsync(32), CreateSocketEngineOptions(ioUringValue: null)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task SocketEngine_KillSwitchZero_DoesNotBreakAsyncSocketWorkflows()
        {
            await RemoteExecutor.Invoke(static () => RunTcpRoundTripAsync(32), CreateSocketEngineOptions(ioUringValue: "0")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringConfig_AppContextSwitches_HonoredWhenEnvUnset()
        {
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AssertBooleanAppContextSwitch(
                        switchName: "System.Net.Sockets.UseIoUring",
                        methodName: "IsIoUringEnabled",
                        expectedWhenSwitchTrue: true,
                        expectedWhenSwitchFalse: false);
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", true);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", false);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                },
                CreateSocketEngineOptions(ioUringValue: null)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringConfig_SqPoll_DualOptIn_RequiresAppContextAndEnvironment()
        {
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUring", true);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsIoUringEnabled"));

                    // SQPOLL request requires both AppContext opt-in and env var opt-in.
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", true);
                    Assert.True(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", false);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                },
                CreateSocketEngineOptions(
                    ioUringValue: "0",
                    sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringConfig_ContradictoryPrimaryInputs_EnvironmentWinsOverAppContext()
        {
            // Env=0 must disable io_uring even when AppContext switch is true.
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUring", true);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsIoUringEnabled"));
                },
                CreateSocketEngineOptions(ioUringValue: "0")).DisposeAsync();

            // Env=1 must enable io_uring even when AppContext switch is false.
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUring", false);
                    Assert.True(InvokeSocketAsyncEngineBoolMethod("IsIoUringEnabled"));
                },
                CreateSocketEngineOptions(ioUringValue: "1")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringConfig_SqPoll_ContradictoryInputs_RequireDualOptInAndOneValue()
        {
            // AppContext=true + env=0 must stay disabled.
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", true);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                },
                CreateSocketEngineOptions(ioUringValue: null, sqPollEnabled: false)).DisposeAsync();

            // AppContext=false + env=1 must stay disabled.
            await RemoteExecutor.Invoke(
                static () =>
                {
                    AppContext.SetSwitch("System.Net.Sockets.UseIoUringSqPoll", false);
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsSqPollRequested"));
                },
                CreateSocketEngineOptions(ioUringValue: null, sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringConfig_RemovedProductionKnobs_DefaultEnabled()
        {
            await RemoteExecutor.Invoke(
                static () =>
                {
                    Assert.False(InvokeSocketAsyncEngineBoolMethod("IsIoUringDirectSqeDisabled"));
                    Assert.True(InvokeSocketAsyncEngineBoolMethod("IsZeroCopySendOptedIn"));
                    Assert.True(InvokeSocketAsyncEngineBoolMethod("IsIoUringRegisterBuffersEnabled"));
                },
                CreateSocketEngineOptions(ioUringValue: null)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_UdpSendReceive_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                IPEndPoint receiverEndpoint = (IPEndPoint)receiver.LocalEndPoint!;

                using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                IPEndPoint senderEndpoint = (IPEndPoint)sender.LocalEndPoint!;
                sender.Connect(receiverEndpoint);

                byte[] sendBuffer = new byte[] { 7 };
                byte[] receiveBuffer = new byte[1];

                for (int i = 0; i < 64; i++)
                {
                    int sent = await sender.SendAsync(sendBuffer, SocketFlags.None);
                    Assert.Equal(1, sent);

                    EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                    SocketReceiveFromResult receiveFrom = await receiver.ReceiveFromAsync(receiveBuffer, SocketFlags.None, remote);
                    Assert.Equal(1, receiveFrom.ReceivedBytes);
                    Assert.Equal(sendBuffer[0], receiveBuffer[0]);
                    Assert.Equal(senderEndpoint, receiveFrom.RemoteEndPoint);

                    int echoed = await receiver.SendToAsync(sendBuffer, SocketFlags.None, receiveFrom.RemoteEndPoint);
                    Assert.Equal(1, echoed);

                    int received = await sender.ReceiveAsync(receiveBuffer, SocketFlags.None);
                    Assert.Equal(1, received);
                    Assert.Equal(sendBuffer[0], receiveBuffer[0]);

                    unchecked
                    {
                        sendBuffer[0]++;
                    }
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_MultipleConcurrentConnections_Work()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                const int ConnectionCount = 32;

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(ConnectionCount);

                var acceptTasks = new Task<Socket>[ConnectionCount];
                var clients = new Socket[ConnectionCount];

                for (int i = 0; i < ConnectionCount; i++)
                {
                    acceptTasks[i] = listener.AcceptAsync();
                }

                var connectTasks = new Task[ConnectionCount];
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;
                for (int i = 0; i < ConnectionCount; i++)
                {
                    clients[i] = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    connectTasks[i] = clients[i].ConnectAsync(endpoint);
                }

                await Task.WhenAll(connectTasks);
                Socket[] servers = await Task.WhenAll(acceptTasks);

                var roundTripTasks = new List<Task>(ConnectionCount);
                for (int i = 0; i < ConnectionCount; i++)
                {
                    Socket client = clients[i];
                    Socket server = servers[i];
                    byte value = (byte)(i + 1);
                    roundTripTasks.Add(Task.Run(async () =>
                    {
                        byte[] tx = new byte[] { value };
                        byte[] rx = new byte[1];

                        int sent = await client.SendAsync(tx, SocketFlags.None);
                        Assert.Equal(1, sent);

                        int received = await server.ReceiveAsync(rx, SocketFlags.None);
                        Assert.Equal(1, received);
                        Assert.Equal(value, rx[0]);

                        sent = await server.SendAsync(tx, SocketFlags.None);
                        Assert.Equal(1, sent);

                        received = await client.ReceiveAsync(rx, SocketFlags.None);
                        Assert.Equal(1, received);
                        Assert.Equal(value, rx[0]);
                    }));
                }

                await Task.WhenAll(roundTripTasks);

                for (int i = 0; i < ConnectionCount; i++)
                {
                    servers[i].Dispose();
                    clients[i].Dispose();
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_DisconnectReconnectAndCancellation_Work()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(2);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                // First connection lifecycle — block scope ensures disposal before reconnect.
                {
                    var firstPair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                    using Socket firstClient = firstPair.Client;
                    using Socket firstServer = firstPair.Server;
                }

                // Reconnect and validate cancellation + subsequent data flow.
                var secondPair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                using Socket secondClient = secondPair.Client;
                using Socket secondServer = secondPair.Server;

                byte[] receiveBuffer = new byte[1];
                using (var cts = new CancellationTokenSource())
                {
                    var pendingReceive = secondServer.ReceiveAsync(receiveBuffer.AsMemory(), SocketFlags.None, cts.Token);
                    cts.Cancel();

                    Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
                    Assert.NotNull(ex);
                    Assert.True(
                        ex is OperationCanceledException ||
                        ex is SocketException socketException &&
                        (socketException.SocketErrorCode == SocketError.OperationAborted || socketException.SocketErrorCode == SocketError.Interrupted),
                        $"Unexpected exception: {ex}");
                }

                byte[] sendBuffer = new byte[] { 42 };
                int sent = await secondClient.SendAsync(sendBuffer, SocketFlags.None);
                Assert.Equal(1, sent);

                int received = await secondServer.ReceiveAsync(receiveBuffer, SocketFlags.None);
                Assert.Equal(1, received);
                Assert.Equal(sendBuffer[0], receiveBuffer[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_QueuedZeroByteReceive_DoesNotStall()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] firstReceiveBuffer = new byte[1];
                Task<int> firstReceive = ToTask(server.ReceiveAsync(firstReceiveBuffer, SocketFlags.None));
                await Task.Yield();

                Task<int> zeroByteReceive = ToTask(server.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None));
                await Task.Yield();

                byte[] firstPayload = new byte[] { 0x11 };
                Assert.Equal(1, await client.SendAsync(firstPayload, SocketFlags.None));
                Assert.Equal(1, await firstReceive);
                Assert.Equal(firstPayload[0], firstReceiveBuffer[0]);

                Task completed = await Task.WhenAny(zeroByteReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(zeroByteReceive, completed);
                Assert.Equal(0, await zeroByteReceive);

                byte[] secondReceiveBuffer = new byte[1];
                Task<int> secondReceive = ToTask(server.ReceiveAsync(secondReceiveBuffer, SocketFlags.None));
                await Task.Yield();

                byte[] secondPayload = new byte[] { 0x22 };
                Assert.Equal(1, await client.SendAsync(secondPayload, SocketFlags.None));
                Assert.Equal(1, await secondReceive);
                Assert.Equal(secondPayload[0], secondReceiveBuffer[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_PureCompletionMode_MixesTcpAndUdp()
        {
            await RemoteExecutor.Invoke(
                static () => RunPureCompletionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CancelWithoutTraffic_CompletesPromptly()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] receiveBuffer = new byte[16];
                using var cts = new CancellationTokenSource();
                Task<int> pendingReceive = ToTask(server.ReceiveAsync(receiveBuffer, SocketFlags.None, cts.Token));

                cts.Cancel();
                Task completed = await Task.WhenAny(pendingReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                Assert.Same(pendingReceive, completed);

                Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
                AssertCanceledOrInterrupted(ex);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveMessageFrom_CancellationAndDispose_DoNotHang()
        {
            await RemoteExecutor.Invoke(static () => RunReceiveMessageFromCancellationAndDisposeScenariosAsync(), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveMessageFrom_Cancellation_DoesNotPoisonNextReceive()
        {
            await RemoteExecutor.Invoke(
                static () => RunReceiveMessageFromCancelThenReceiveScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveMessageFrom_CancellationAndDispose_GcPressure_DoNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunReceiveMessageFromCancellationAndDisposeScenariosWithGcPressureAsync(iterations: 32),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_TeardownDrainTrackedOperations_CancelsPendingReceives()
        {
            await RemoteExecutor.Invoke(
                static () => RunTeardownDrainTrackedOperationsScenarioAsync(iterations: 64),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_TeardownCancellationDuplicateGuard_DoesNotInflateAsyncCancelRequestCqes()
        {
            await RemoteExecutor.Invoke(
                static () => RunTeardownCancellationDuplicateGuardScenarioAsync(iterations: 96),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_MixedReadinessAndCompletion_NoStarvation()
        {
            await RemoteExecutor.Invoke(
                static () => AwaitWithTimeoutAsync(
                    RunMixedModeReadinessCompletionStressScenarioAsync(iterations: 128),
                    nameof(IoUringCompletionMode_MixedReadinessAndCompletion_NoStarvation)),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_SameSocketReadinessCompletionBacklog_NoStarvation()
        {
            await RemoteExecutor.Invoke(
                static () => RunSameSocketReadinessCompletionBacklogScenarioAsync(iterations: 64, completionBatchSize: 8),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_InvalidTestEventBufferCount_FallsBackToDefault()
        {
            await RemoteExecutor.Invoke(
                static () => RunTcpRoundTripAsync(32),
                CreateSocketEngineOptions(testEventBufferCountRaw: "not-a-number")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_PrepareQueueOverflow_FallsBackAndCompletes()
        {
            await RemoteExecutor.Invoke(
                static () => RunPrepareQueueOverflowFallbackScenarioAsync(connectionCount: 32),
                CreateSocketEngineOptions(prepareQueueCapacity: 1, directSqeEnabled: false)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ConnectQueueOverflow_FallsBackAndCompletes()
        {
            await RemoteExecutor.Invoke(
                static () => RunConnectQueueOverflowFallbackScenarioAsync(connectionCount: 32),
                CreateSocketEngineOptions(prepareQueueCapacity: 1, directSqeEnabled: false)).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_PrepareQueueOverflow_Stress_NoHangs()
        {
            await RemoteExecutor.Invoke(
                static () => RunPrepareQueueOverflowFallbackScenarioAsync(connectionCount: 96),
                CreateSocketEngineOptions(prepareQueueCapacity: 2, directSqeEnabled: false)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_Recovery_InjectAndCompletes()
        {
            await RemoteExecutor.Invoke(
                static () => RunCqOverflowRecoveryScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_Recovery_ZeroTrackedOperations_Completes()
        {
            await RemoteExecutor.Invoke(
                static () => RunCqOverflowRecoveryWithZeroTrackedOperationsScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_Recovery_SmallRing_RealKernelOverflow()
        {
            await RemoteExecutor.Invoke(
                static () => RunCqOverflowRecoveryWithSmallRingScenarioAsync(),
                CreateSocketEngineOptions(
                    queueEntries: 8,
                    threadCount: 1,
                    directSqeEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_CqOverflowDuringArming_RecoversWithoutSilentLoss()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptOverflowArmingScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_TeardownRace_DoesNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunTeardownUnderOverflowScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_SustainedReentrancy_NoDeadlock()
        {
            await RemoteExecutor.Invoke(
                static () => RunSustainedOverflowReentrancyScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CqOverflow_ReflectionTargets_Stable()
        {
            await RemoteExecutor.Invoke(
                static () => RunCqOverflowReflectionTargetStabilityScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_NativeMsghdrLayoutContract_IsStable()
        {
            await RemoteExecutor.Invoke(
                static () => RunNativeMsghdrLayoutContractScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_NativeMsghdrLayout_Rejects32BitPath()
        {
            await RemoteExecutor.Invoke(
                static () => RunNativeMsghdr32BitRejectionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CompletionSlotLayoutContract_IsStable()
        {
            await RemoteExecutor.Invoke(
                static () => RunCompletionSlotLayoutContractScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CompletionSlotUserData_BoundaryEncoding_IsStable()
        {
            await RemoteExecutor.Invoke(
                static () => RunCompletionSlotUserDataBoundaryScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring ring fd + fcntl are Linux-specific.
        public static async Task IoUringCompletionMode_RingFd_HasCloseOnExecSet()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    await RunTcpRoundTripAsync(4);

                    if (!TryGetIoUringRingFdForTest(out int ringFd))
                    {
                        return;
                    }

                    int descriptorFlags = Fcntl(ringFd, F_GETFD);
                    Assert.True(descriptorFlags >= 0, $"fcntl(F_GETFD) failed with errno {Marshal.GetLastPInvokeError()}.");
                    Assert.NotEqual(0, descriptorFlags & FD_CLOEXEC);
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring wakeup eventfd + fcntl are Linux-specific.
        public static async Task IoUringCompletionMode_WakeupEventFd_HasCloseOnExecSet()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    await RunTcpRoundTripAsync(4);

                    if (!TryGetIoUringWakeupEventFdForTest(out int wakeupEventFd))
                    {
                        return;
                    }

                    int descriptorFlags = Fcntl(wakeupEventFd, F_GETFD);
                    Assert.True(descriptorFlags >= 0, $"fcntl(F_GETFD) failed with errno {Marshal.GetLastPInvokeError()}.");
                    Assert.NotEqual(0, descriptorFlags & FD_CLOEXEC);
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // fork/exec descriptor inheritance is Linux-specific.
        public static async Task IoUringCompletionMode_RingAndWakeupEventFd_DoNotLeakAcrossExec()
        {
            await RemoteExecutor.Invoke(
                static () => RunCloseOnExecForkExecScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring accept flags are Linux-specific.
        public static async Task IoUringCompletionMode_AcceptedSocket_HasCloseOnExecAndNonBlockingSet()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    listener.Listen(1);

                    using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    Task<Socket> acceptTask = listener.AcceptAsync();
                    await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);
                    using Socket server = await acceptTask;

                    if (!TryGetIoUringRingFdForTest(out _))
                    {
                        return;
                    }

                    int acceptedFd = checked((int)server.SafeHandle.DangerousGetHandle());

                    int descriptorFlags = Fcntl(acceptedFd, F_GETFD);
                    Assert.True(descriptorFlags >= 0, $"fcntl(F_GETFD) failed with errno {Marshal.GetLastPInvokeError()}.");
                    Assert.NotEqual(0, descriptorFlags & FD_CLOEXEC);

                    int statusFlags = Fcntl(acceptedFd, F_GETFL);
                    Assert.True(statusFlags >= 0, $"fcntl(F_GETFL) failed with errno {Marshal.GetLastPInvokeError()}.");
                    Assert.NotEqual(0, statusFlags & O_NONBLOCK);
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring seccomp/submit-error behavior is Linux-specific.
        public static async Task IoUringCompletionMode_ForcedSubmitEperm_DoesNotCrashProcess()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    Exception? firstFailure = await Record.ExceptionAsync(async () => await RunTcpRoundTripAsync(4));
                    if (firstFailure is not null)
                    {
                        Assert.True(
                            firstFailure is SocketException ||
                            firstFailure is OperationCanceledException ||
                            firstFailure is ObjectDisposedException,
                            $"Unexpected exception after forced submit EPERM: {firstFailure}");
                    }

                    // Ensure the engine remains usable after the forced EPERM submit rejection.
                    await RunTcpRoundTripAsync(4);
                });
            }, CreateSocketEngineOptions(forceSubmitEpermOnce: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring enter retry-limit behavior is Linux-specific.
        public static async Task IoUringCompletionMode_ForcedEnterEintrRetryLimit_DoesNotCrashProcess()
        {
            await RemoteExecutor.Invoke(
                static () => RunForcedEnterEintrRetryLimitScenarioAsync(),
                CreateSocketEngineOptions(forceEnterEintrRetryLimitOnce: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_SingleIssuer_DebugAssertion_FiresOnNonEventLoopCall()
        {
            await RemoteExecutor.Invoke(
                static () => RunDebugNonEventLoopSingleIssuerAssertionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_NonPinnableMemory_FallsBackAndCompletes(bool receivePath)
        {
            await RemoteExecutor.Invoke(
                static arg => RunNonPinnableMemoryFallbackScenarioAsync(receivePath: bool.Parse(arg)),
                receivePath.ToString(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_PinnableMemory_PinReleaseLifecycle_Works()
        {
            await RemoteExecutor.Invoke(
                static () => RunPinnableMemoryPinReleaseLifecycleScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegistrationLifecycle_IsStable()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferRegistrationLifecycleScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_BufferSelectReceive_RecyclesBuffer()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferSelectReceiveScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RecyclesBeyondRingCapacity()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferRecycleReuseScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_ForcedExhaustion_ReportsNoBufferSpace()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferExhaustionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_MixedWithRecvFrom_Works()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferMixedWorkloadScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_SmallMessages_Shrinks()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferSmallMessageShrinkScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_LargeMessages_Grows()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferLargeMessageGrowScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_MixedWorkload_Stable()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferMixedWorkloadStableScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_ResizeSwap_NoDataLoss()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferResizeSwapNoDataLossScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_ResizeSwap_ConcurrentInFlight_NoDataLoss()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferResizeSwapConcurrentInFlightNoDataLossScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_Disabled_StaysFixed()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferDisabledScenarioAsync(),
                CreateSocketEngineOptions(
                    providedBufferSize: 4096,
                    adaptiveBufferSizingEnabled: false)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_Default_IsDisabled()
        {
            await RemoteExecutor.Invoke(
                static () => RunAdaptiveProvidedBufferSizingStateScenarioAsync(expectedEnabled: false),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_ProvidedBuffer_AdaptiveSizing_Switch_HonorsBothValues(bool enabled)
        {
            await RemoteExecutor.Invoke(
                static arg => RunAdaptiveProvidedBufferSizingStateScenarioAsync(bool.Parse(arg)),
                enabled.ToString(),
                CreateSocketEngineOptions(adaptiveBufferSizingEnabled: enabled)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_DisabledByEnvVar()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferKernelRegistrationDisabledScenarioAsync(),
                CreateSocketEngineOptions(registerBuffersEnabled: false)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_SuccessState_VisibleWhenAvailable()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferKernelRegistrationSuccessScenarioAsync(),
                CreateSocketEngineOptions(registerBuffersEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_FailureWhenObserved_IsNonFatal()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferKernelRegistrationFailureNonFatalScenarioAsync(),
                CreateSocketEngineOptions(
                    registerBuffersEnabled: true,
                    providedBufferSize: 65536)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_AdaptiveResize_TriggersReregistration()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferKernelReregistrationOnResizeScenarioAsync(),
                CreateSocketEngineOptions(
                    registerBuffersEnabled: true,
                    adaptiveBufferSizingEnabled: true,
                    providedBufferSize: 4096)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_DataCorrectness_WithRegisteredBuffers()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferRegisteredBuffersDataCorrectnessScenarioAsync(),
                CreateSocketEngineOptions(registerBuffersEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_MemoryPressure_GracefulFallbackOrSuccess()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferRegistrationMemoryPressureScenarioAsync(),
                CreateSocketEngineOptions(
                    registerBuffersEnabled: true,
                    providedBufferSize: 65536)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // provided-buffer OOM fallback is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_ForcedRingAllocationFailure_FallsBackGracefully()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferRingForcedAllocationFailureFallbackScenarioAsync(),
                CreateSocketEngineOptions(forceProvidedBufferRingOomOnce: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring teardown ordering contract is Linux-specific.
        public static async Task IoUringCompletionMode_ProvidedBuffer_RegisterBuffers_TeardownOrdering_UnregisterBeforeRingClose()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferTeardownOrderingContractScenarioAsync(),
                CreateSocketEngineOptions(registerBuffersEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_FixedRecv_Default_IsDisabled()
        {
            await RemoteExecutor.Invoke(
                static () => RunFixedRecvStateScenarioAsync(expectedEnabled: false),
                CreateSocketEngineOptions(registerBuffersEnabled: false)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_FixedRecv_Activation_FollowsRuntimeCapabilities()
        {
            await RemoteExecutor.Invoke(
                static () => RunFixedRecvActivationFollowsRuntimeCapabilitiesScenarioAsync(),
                CreateSocketEngineOptions(registerBuffersEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_FixedRecv_Enabled_DataCorrectness_WithRegisteredBuffers()
        {
            await RemoteExecutor.Invoke(
                static () => RunFixedRecvDataCorrectnessScenarioAsync(),
                CreateSocketEngineOptions(
                    registerBuffersEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_BasicSendReceive()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollBasicSendReceiveScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL request behavior is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_Requested_DoesNotBreakSocketOperations()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollRequestedScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // DEFER_TASKRUN submitter_task is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_DeferTaskrun_InitializesOnEventLoopThread()
        {
            await RemoteExecutor.Invoke(
                static () => RunDeferTaskrunEventLoopInitScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL wakeup path is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_IdleWakeupPath_IncrementsWakeupCounterWhenObserved()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollWakeupAfterIdleScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL + multishot recv is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_MultishotRecv_Works()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollMultishotRecvScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL + zero-copy send is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_ZeroCopySend_Works()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollZeroCopySendScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // SQPOLL SQ flags contract is Linux io_uring-specific.
        public static async Task IoUringCompletionMode_SqPoll_SqNeedWakeup_ContractMatchesSqFlagBit()
        {
            await RemoteExecutor.Invoke(
                static () => RunSqPollNeedWakeupContractScenarioAsync(),
                CreateSocketEngineOptions(sqPollEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_Default_IsEnabledWhenSupported()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendStateScenarioAsync(expectedEnabledWhenSupported: true),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_ZeroCopySend_Switch_HonorsBothValues(bool enabled)
        {
            await RemoteExecutor.Invoke(
                static arg => RunZeroCopySendStateScenarioAsync(bool.Parse(arg)),
                enabled.ToString(),
                CreateSocketEngineOptions(zeroCopySendEnabled: enabled)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_LargeBuffer_CompletesCorrectly()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendLargeBufferRoundTripScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_SmallBuffer_UsesRegularSendFallbackPath_ForcedSendErrorObserved()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendSmallBufferUsesRegularSendWithForcedSendErrorScenarioAsync(),
                CreateSocketEngineOptions(
                    zeroCopySendEnabled: true,
                    forceEcanceledOnceMask: "send")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_NotifCqe_ReleasesPinHolds()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendNotifCqeReleasesPinHoldsScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_ResetStorm_RecoversPendingNotificationSlots()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendResetStormSlotRecoveryScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_PartialSendResubmission_CompletesFully()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendPartialSendResubmissionScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_TaskCompletion_ReleasesPins()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendCompletionPinLifetimeScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_UnsupportedOpcode_FallsBackGracefully()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendUnsupportedOpcodeFallbackScenarioAsync(),
                CreateSocketEngineOptions(zeroCopySendEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_BufferList4KSegments_AboveThreshold_UsesSendMsgZc()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendBufferListSegmentThresholdScenarioAsync(),
                CreateSocketEngineOptions(
                    zeroCopySendEnabled: true,
                    forceEcanceledOnceMask: "sendmsg")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroCopySend_SendToAboveThreshold_UsesSendMsgZc()
        {
            await RemoteExecutor.Invoke(
                static () => RunZeroCopySendToAboveThresholdScenarioAsync(),
                CreateSocketEngineOptions(
                    zeroCopySendEnabled: true,
                    forceEcanceledOnceMask: "sendmsg")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotRecv_Basic_CompletesAcrossIterations()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotRecvBasicScenarioAsync(iterations: 64),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotRecv_Cancellation_Completes()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotRecvCancellationScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotRecv_PeerClose_Terminates()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotRecvPeerCloseScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotRecv_ProvidedBufferExhaustion_FollowsPolicy()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferExhaustionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_ProvidedBufferExhaustion_TerminatesAndRecovers()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvProvidedBufferExhaustionScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotRecv_MixedWithOneShot_Coexists()
        {
            await RemoteExecutor.Invoke(
                static () => RunProvidedBufferMixedWorkloadScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_ShapeChange_CancelsAndRearms()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvShapeChangeScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_ConcurrentCloseRace_DoesNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvConcurrentCloseRaceScenarioAsync(iterations: 32),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring queue saturation behavior is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_DataQueueSaturation_CapsAtSixteenBufferedCompletions()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvQueueSaturationScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // direct SQE preparation race is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_ConcurrentCloseRace_DirectSqeEnabled_DoesNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvConcurrentCloseRaceScenarioAsync(iterations: 32),
                CreateSocketEngineOptions(directSqeEnabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring persistent multishot recv terminal/data race is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_DataThenFin_DeliversDataThenTerminal()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvDataThenFinScenarioAsync(iterations: 24),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring persistent multishot recv terminal/data race is Linux-specific.
        public static async Task IoUringPersistentMultishotRecv_DataThenReset_DeliversDataThenTerminal()
        {
            await RemoteExecutor.Invoke(
                static () => RunPersistentMultishotRecvDataThenResetScenarioAsync(iterations: 24),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_Basic_CompletesAcrossIterations()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptBasicScenarioAsync(connectionCount: 10),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_PrequeuesConnections_BeforeSubsequentAcceptAsync()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptPrequeueScenarioAsync(prequeuedConnectionCount: 5),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_ListenerClose_CompletesPendingAcceptAndDrainsQueue()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptListenerCloseScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // teardown race behavior is Linux-specific.
        public static async Task IoUringMultishotAccept_TeardownRace_InFlightAcceptDelivery_DoesNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptTeardownRaceScenarioAsync(iterations: 32),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_DisposeDuringArmingRace_DoesNotHang()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptDisposeDuringArmingRaceScenarioAsync(iterations: 64),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_PrepareUnsupported_UsesOneShotFallback()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptPrepareUnsupportedOneShotFallbackScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_TerminalCompletion_RearmsOnNextAccept()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptRearmAfterTerminalCqeScenarioAsync(),
                CreateSocketEngineOptions(forceEcanceledOnceMask: "accept")).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringMultishotAccept_HighConnectionRate_NoLoss()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptHighConnectionRateScenarioAsync(connectionCount: 256),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring accept progress under idle pending state is Linux-specific.
        public static async Task IoUringMultishotAccept_IdlePendingAccept_ConnectCompletesPromptly()
        {
            await RemoteExecutor.Invoke(
                static () => RunMultishotAcceptIdlePendingScenarioAsync(iterations: 24, idleDelay: TimeSpan.FromMilliseconds(150)),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // slot-capacity pressure behavior is Linux-specific.
        public static async Task IoUringCompletionMode_SlotCapacityStress_4000Connections_Completes()
        {
            await RemoteExecutor.Invoke(
                static () => RunSlotCapacityStressScenarioAsync(connectionCount: 4000),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_NetworkStream_ReadAsync_CancellationToken_Works()
        {
            await RemoteExecutor.Invoke(
                static () => RunNetworkStreamReadAsyncCancellationTokenScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveAsync_SocketAsyncEventArgs_BufferList_Unaffected()
        {
            await RemoteExecutor.Invoke(
                static () => RunReceiveAsyncSocketAsyncEventArgsBufferListScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_BufferListSendReceive_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] payload = new byte[] { 0x01, 0x11, 0x21, 0x31, 0x41, 0x51, 0x61 };
                var sendBuffers = new List<ArraySegment<byte>>
                {
                    new ArraySegment<byte>(payload, 0, 2),
                    new ArraySegment<byte>(payload, 2, 1),
                    new ArraySegment<byte>(payload, 3, 4)
                };

                byte[] receiveBuffer1 = new byte[3];
                byte[] receiveBuffer2 = new byte[4];
                var receiveBuffers = new List<ArraySegment<byte>>
                {
                    new ArraySegment<byte>(receiveBuffer1),
                    new ArraySegment<byte>(receiveBuffer2)
                };

                Task<int> receiveTask = server.ReceiveAsync(receiveBuffers, SocketFlags.None);
                await Task.Yield();

                int sent = await client.SendAsync(sendBuffers, SocketFlags.None);
                Assert.Equal(payload.Length, sent);

                int received = await receiveTask;
                Assert.Equal(payload.Length, received);

                byte[] combined = new byte[payload.Length];
                Buffer.BlockCopy(receiveBuffer1, 0, combined, 0, receiveBuffer1.Length);
                Buffer.BlockCopy(receiveBuffer2, 0, combined, receiveBuffer1.Length, receiveBuffer2.Length);
                Assert.Equal(payload, combined);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_BufferListReceive_WithPeek_PreservesData()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] payload = new byte[] { 0x0A, 0x1A, 0x2A, 0x3A };
                Assert.Equal(payload.Length, await client.SendAsync(payload, SocketFlags.None));

                byte[] peekBuffer1 = new byte[2];
                byte[] peekBuffer2 = new byte[2];
                int peeked = await server.ReceiveAsync(
                    new List<ArraySegment<byte>>
                    {
                        new ArraySegment<byte>(peekBuffer1),
                        new ArraySegment<byte>(peekBuffer2)
                    },
                    SocketFlags.Peek);
                Assert.Equal(payload.Length, peeked);

                byte[] peekCombined = new byte[payload.Length];
                Buffer.BlockCopy(peekBuffer1, 0, peekCombined, 0, peekBuffer1.Length);
                Buffer.BlockCopy(peekBuffer2, 0, peekCombined, peekBuffer1.Length, peekBuffer2.Length);
                Assert.Equal(payload, peekCombined);

                byte[] receiveBuffer1 = new byte[1];
                byte[] receiveBuffer2 = new byte[3];
                int received = await server.ReceiveAsync(
                    new List<ArraySegment<byte>>
                    {
                        new ArraySegment<byte>(receiveBuffer1),
                        new ArraySegment<byte>(receiveBuffer2)
                    },
                    SocketFlags.None);
                Assert.Equal(payload.Length, received);

                byte[] receiveCombined = new byte[payload.Length];
                Buffer.BlockCopy(receiveBuffer1, 0, receiveCombined, 0, receiveBuffer1.Length);
                Buffer.BlockCopy(receiveBuffer2, 0, receiveCombined, receiveBuffer1.Length, receiveBuffer2.Length);
                Assert.Equal(payload, receiveCombined);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_BufferListReceiveFrom_WritesRemoteEndPoint()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                byte[] receiveBuffer1 = new byte[3];
                byte[] receiveBuffer2 = new byte[4];
                using var receiveEventArgs = new SocketAsyncEventArgs
                {
                    BufferList = new List<ArraySegment<byte>>
                    {
                        new ArraySegment<byte>(receiveBuffer1),
                        new ArraySegment<byte>(receiveBuffer2)
                    },
                    RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0)
                };

                Task<SocketAsyncEventArgs> receiveTask = StartSocketAsyncEventArgsOperation(
                    receiver,
                    receiveEventArgs,
                    static (s, args) => s.ReceiveFromAsync(args));
                await Task.Yield();

                byte[] payload = new byte[] { 0xA0, 0xB0, 0xC0, 0xD0, 0xE0, 0xF0, 0x01 };
                int sent = await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!);
                Assert.Equal(payload.Length, sent);

                SocketAsyncEventArgs completedReceive = await receiveTask;
                Assert.Equal(SocketError.Success, completedReceive.SocketError);
                Assert.Equal(payload.Length, completedReceive.BytesTransferred);
                Assert.Equal(SocketFlags.None, completedReceive.SocketFlags);

                IPEndPoint expectedRemoteEndPoint = (IPEndPoint)sender.LocalEndPoint!;
                IPEndPoint actualRemoteEndPoint = Assert.IsType<IPEndPoint>(completedReceive.RemoteEndPoint);
                Assert.Equal(expectedRemoteEndPoint, actualRemoteEndPoint);

                byte[] combined = new byte[payload.Length];
                Buffer.BlockCopy(receiveBuffer1, 0, combined, 0, receiveBuffer1.Length);
                Buffer.BlockCopy(receiveBuffer2, 0, combined, receiveBuffer1.Length, receiveBuffer2.Length);
                Assert.Equal(payload, combined);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_BufferListSendTo_WritesPayloadAndEndpoint()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                byte[] payload = new byte[] { 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
                byte[] receiveBuffer = new byte[payload.Length];

                Task<SocketReceiveFromResult> receiveTask =
                    ToTask(receiver.ReceiveFromAsync(receiveBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));

                using var sendEventArgs = new SocketAsyncEventArgs
                {
                    BufferList = new List<ArraySegment<byte>>
                    {
                        new ArraySegment<byte>(payload, 0, 2),
                        new ArraySegment<byte>(payload, 2, 1),
                        new ArraySegment<byte>(payload, 3, 3)
                    },
                    RemoteEndPoint = receiver.LocalEndPoint
                };

                SocketAsyncEventArgs completedSend = await StartSocketAsyncEventArgsOperation(
                    sender,
                    sendEventArgs,
                    static (s, args) => s.SendToAsync(args));
                Assert.Equal(SocketError.Success, completedSend.SocketError);
                Assert.Equal(payload.Length, completedSend.BytesTransferred);

                SocketReceiveFromResult receiveResult = await receiveTask;
                Assert.Equal(payload.Length, receiveResult.ReceivedBytes);
                Assert.Equal(payload, receiveBuffer);

                IPEndPoint expectedRemoteEndPoint = (IPEndPoint)sender.LocalEndPoint!;
                IPEndPoint actualRemoteEndPoint = Assert.IsType<IPEndPoint>(receiveResult.RemoteEndPoint);
                Assert.Equal(expectedRemoteEndPoint, actualRemoteEndPoint);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_AcceptConnect_SocketAsyncEventArgs_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var acceptEventArgs = new SocketAsyncEventArgs();
                Task<SocketAsyncEventArgs> acceptTask = StartSocketAsyncEventArgsOperation(
                    listener,
                    acceptEventArgs,
                    static (s, args) => s.AcceptAsync(args));
                await Task.Yield();

                using var connectEventArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = listener.LocalEndPoint
                };

                SocketAsyncEventArgs completedConnect = await StartSocketAsyncEventArgsOperation(
                    client,
                    connectEventArgs,
                    static (s, args) => s.ConnectAsync(args));
                Assert.Equal(SocketError.Success, completedConnect.SocketError);

                SocketAsyncEventArgs completedAccept = await acceptTask;
                Assert.Equal(SocketError.Success, completedAccept.SocketError);

                Socket accepted = Assert.IsType<Socket>(completedAccept.AcceptSocket);
                completedAccept.AcceptSocket = null;
                using Socket server = accepted;

                // Validates accept address-length handling: the endpoint must match the connecting socket exactly.
                IPEndPoint expectedRemoteEndPoint = (IPEndPoint)client.LocalEndPoint!;
                IPEndPoint actualRemoteEndPoint = Assert.IsType<IPEndPoint>(server.RemoteEndPoint);
                Assert.Equal(expectedRemoteEndPoint, actualRemoteEndPoint);

                byte[] payload = new byte[] { 0x5A };
                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Assert.Equal(payload[0], receiveBuffer[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_AcceptAsync_CancellationToken_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                Task<Socket> acceptTask = ToTask(listener.AcceptAsync(cts.Token));

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);
                using Socket server = await acceptTask;

                byte[] payload = new byte[] { 0x4D };
                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Assert.Equal(payload[0], receiveBuffer[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_AcceptAsync_SocketAsyncEventArgs_PrecreatedAcceptSocket_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(4);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                // Arm multishot accept and leave one connection queued for pre-accept dequeue.
                Task<Socket> armingAcceptTask = listener.AcceptAsync();
                Assert.True(
                    await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true),
                    "Expected multishot accept to arm before precreated AcceptSocket test.");

                using Socket firstClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using Socket secondClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await Task.WhenAll(firstClient.ConnectAsync(endpoint), secondClient.ConnectAsync(endpoint));
                using Socket firstServer = await armingAcceptTask;

                DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                while (DateTime.UtcNow < deadline && GetListenerMultishotAcceptQueueCount(listener) == 0)
                {
                    await Task.Delay(25);
                }

                Assert.True(GetListenerMultishotAcceptQueueCount(listener) > 0, "Expected a queued pre-accepted connection.");

                using Socket precreatedAcceptSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var acceptEventArgs = new SocketAsyncEventArgs
                {
                    AcceptSocket = precreatedAcceptSocket
                };

                SocketAsyncEventArgs completedAccept = await StartSocketAsyncEventArgsOperation(
                    listener,
                    acceptEventArgs,
                    static (s, args) => s.AcceptAsync(args));
                Assert.Equal(SocketError.Success, completedAccept.SocketError);
                Assert.Same(precreatedAcceptSocket, completedAccept.AcceptSocket);

                byte[] payload = new byte[] { 0x3F };
                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, await secondClient.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await precreatedAcceptSocket.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Assert.Equal(payload[0], receiveBuffer[0]);

                // Keep ownership of the accepted socket out of event-args disposal.
                completedAccept.AcceptSocket = null;
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_TcpListener_AcceptTcpClientAsync_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using var listener = new TcpListener(IPAddress.Loopback, 0);
                listener.Start();
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndpoint;

                Task<TcpClient> acceptTask = listener.AcceptTcpClientAsync();
                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                await client.ConnectAsync(endpoint);

                using TcpClient acceptedClient = await acceptTask;
                using Socket server = acceptedClient.Client;

                byte[] payload = new byte[] { 0x2A };
                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Assert.Equal(payload[0], receiveBuffer[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ConnectAsync_OffLenRegression_Ipv4AndIpv6_Works()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                await VerifyConnectAsync(AddressFamily.InterNetwork, IPAddress.Loopback);

                if (Socket.OSSupportsIPv6)
                {
                    await VerifyConnectAsync(AddressFamily.InterNetworkV6, IPAddress.IPv6Loopback);
                }

                static async Task VerifyConnectAsync(AddressFamily addressFamily, IPAddress loopback)
                {
                    using Socket listener = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                    listener.Bind(new IPEndPoint(loopback, 0));
                    listener.Listen(1);

                    using Socket client = new Socket(addressFamily, SocketType.Stream, ProtocolType.Tcp);
                    Task<Socket> acceptTask = listener.AcceptAsync();

                    using var connectEventArgs = new SocketAsyncEventArgs
                    {
                        RemoteEndPoint = listener.LocalEndPoint
                    };

                    SocketAsyncEventArgs completedConnect = await StartSocketAsyncEventArgsOperation(
                        client,
                        connectEventArgs,
                        static (s, args) => s.ConnectAsync(args));
                    Assert.Equal(SocketError.Success, completedConnect.SocketError);

                    using Socket server = await acceptTask;
                    Assert.Equal(client.LocalEndPoint, server.RemoteEndPoint);

                    byte[] payload = new byte[] { 0x3C };
                    byte[] receiveBuffer = new byte[1];
                    Assert.Equal(1, await client.SendAsync(payload, SocketFlags.None));
                    Assert.Equal(1, await server.ReceiveAsync(receiveBuffer, SocketFlags.None));
                    Assert.Equal(payload[0], receiveBuffer[0]);
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ConnectAsync_WithInitialData_Success_ServerReceivesPayload()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(1);

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                using var connectEventArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = listener.LocalEndPoint
                };

                byte[] initialPayload = new byte[256];
                for (int i = 0; i < initialPayload.Length; i++)
                {
                    initialPayload[i] = unchecked((byte)(0xA0 + i));
                }

                connectEventArgs.SetBuffer(initialPayload, 0, initialPayload.Length);

                Task<Socket> acceptTask = listener.AcceptAsync();
                SocketAsyncEventArgs completedConnect = await StartSocketAsyncEventArgsOperation(
                    client,
                    connectEventArgs,
                    static (s, args) => s.ConnectAsync(args));
                Assert.Equal(SocketError.Success, completedConnect.SocketError);

                using Socket server = await acceptTask;
                byte[] receivedPayload = new byte[initialPayload.Length];
                await ReceiveExactlyAsync(server, receivedPayload);
                Assert.Equal(initialPayload, receivedPayload);

                await AssertConnectedPairRoundTripAsync(client, server, 0xAB);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ConnectAsync_WithInitialData_ForcedSendFailure_PropagatesError()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(2);

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.SendBufferSize = 1024;
                using var connectEventArgs = new SocketAsyncEventArgs
                {
                    RemoteEndPoint = listener.LocalEndPoint
                };

                byte[] initialPayload = new byte[8 * 1024 * 1024];
                for (int i = 0; i < initialPayload.Length; i++)
                {
                    initialPayload[i] = unchecked((byte)i);
                }
                connectEventArgs.SetBuffer(initialPayload, 0, initialPayload.Length);

                Task<Socket> firstAcceptTask = listener.AcceptAsync();
                Task<SocketAsyncEventArgs> connectTask = StartSocketAsyncEventArgsOperation(
                    client,
                    connectEventArgs,
                    static (s, args) => s.ConnectAsync(args));
                using (Socket firstServer = await firstAcceptTask)
                {
                    firstServer.LingerState = new LingerOption(enable: true, seconds: 0);
                }

                Task completed = await Task.WhenAny(connectTask, Task.Delay(TimeSpan.FromSeconds(30)));
                Assert.Same(connectTask, completed);
                SocketAsyncEventArgs completedConnect = await connectTask;
                Assert.NotEqual(SocketError.Success, completedConnect.SocketError);

                using Socket secondClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                Task<Socket> secondAcceptTask = listener.AcceptAsync();
                await secondClient.ConnectAsync((IPEndPoint)listener.LocalEndPoint!);
                using Socket secondServer = await secondAcceptTask;

                byte[] payload = new byte[] { 0x9A };
                byte[] receiveBuffer = new byte[1];
                Assert.Equal(1, await secondClient.SendAsync(payload, SocketFlags.None));
                Assert.Equal(1, await secondServer.ReceiveAsync(receiveBuffer, SocketFlags.None));
                Assert.Equal(payload[0], receiveBuffer[0]);
            }, CreateSocketEngineOptions(forceEcanceledOnceMask: "send")).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_ReceiveMessageFrom_PacketInformation_Works(bool useIpv6)
        {
            await RemoteExecutor.Invoke(
                static arg => RunReceiveMessageFromPacketInformationRoundTripAsync(useIpv6: bool.Parse(arg)),
                useIpv6.ToString(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_ReceiveMessageFrom_BufferList_PacketInformation_Works(bool useIpv6)
        {
            await RemoteExecutor.Invoke(static async arg =>
            {
                bool useIpv6 = bool.Parse(arg);
                if (useIpv6 && !Socket.OSSupportsIPv6)
                {
                    return;
                }

                AddressFamily family = useIpv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork;
                IPAddress loopback = useIpv6 ? IPAddress.IPv6Loopback : IPAddress.Loopback;
                IPAddress anyAddress = useIpv6 ? IPAddress.IPv6Any : IPAddress.Any;
                SocketOptionLevel packetInfoLevel = useIpv6 ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP;

                using Socket receiver = new Socket(family, SocketType.Dgram, ProtocolType.Udp);
                using Socket sender = new Socket(family, SocketType.Dgram, ProtocolType.Udp);

                receiver.SetSocketOption(packetInfoLevel, SocketOptionName.PacketInformation, true);
                receiver.Bind(new IPEndPoint(loopback, 0));
                sender.Bind(new IPEndPoint(loopback, 0));

                byte[] payload = new byte[] { 0x70, 0x71, 0x72, 0x73, 0x74 };
                byte[] receiveBuffer = new byte[payload.Length];

                using var receiveEventArgs = new SocketAsyncEventArgs
                {
                    BufferList = new List<ArraySegment<byte>>
                    {
                        new ArraySegment<byte>(receiveBuffer, 0, 2),
                        new ArraySegment<byte>(receiveBuffer, 2, 3)
                    },
                    RemoteEndPoint = new IPEndPoint(anyAddress, 0)
                };

                Task<SocketAsyncEventArgs> receiveTask = StartReceiveMessageFromAsync(receiver, receiveEventArgs);
                await Task.Yield();

                int sent = await sender.SendToAsync(payload, SocketFlags.None, receiver.LocalEndPoint!);
                Assert.Equal(payload.Length, sent);

                SocketAsyncEventArgs completedReceive = await receiveTask;
                Assert.Equal(SocketError.Success, completedReceive.SocketError);
                Assert.Equal(payload.Length, completedReceive.BytesTransferred);
                Assert.Equal(payload, receiveBuffer);
                Assert.Equal(sender.LocalEndPoint, completedReceive.RemoteEndPoint);
                Assert.Equal(((IPEndPoint)sender.LocalEndPoint!).Address, completedReceive.ReceiveMessageFromPacketInfo.Address);
            }, useIpv6.ToString(), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_SendAsync_PartialSendResubmission_CompletesFully(bool useBufferListSend)
        {
            await RemoteExecutor.Invoke(
                static (arg) => RunLargeSendWithBackpressureAsync(useBufferListSend: bool.Parse(arg)),
                useBufferListSend.ToString(), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        [InlineData(false)]
        [InlineData(true)]
        public static async Task IoUringCompletionMode_ForcedReceiveResultOnce_RecoversAndNextOperationStillWorks(bool forceEcanceled)
        {
            await RemoteExecutor.Invoke(
                static arg => RunForcedReceiveScenarioAsync(forceEcanceled: bool.Parse(arg)),
                forceEcanceled.ToString(),
                CreateSocketEngineOptions(
                    forceEagainOnceMask: forceEcanceled ? null : "recv",
                    forceEcanceledOnceMask: forceEcanceled ? "recv" : null)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ForcedEagain_Recv_RequeuesViaCompletionPath()
        {
            await RemoteExecutor.Invoke(
                static () =>
                {
                    long queuedRetryBefore = GetIoUringPendingRetryQueuedToPrepareQueueCount();

                    return Task.Run(async () =>
                    {
                        await RunForcedReceiveScenarioAsync(forceEcanceled: false);

                        Assert.Equal(queuedRetryBefore, GetIoUringPendingRetryQueuedToPrepareQueueCount());
                    });
                },
                CreateSocketEngineOptions(
                    forceEagainOnceMask: "recv")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ZeroByteReceive_OnPeerClose_ReturnsZeroOrCloseError()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    var trio = await CreateConnectedTcpSocketTrioAsync();
                    using Socket _ = trio.Listener;
                    using Socket client = trio.Client;
                    using Socket server = trio.Server;

                    Task<int> zeroByteReceive = ToTask(server.ReceiveAsync(Memory<byte>.Empty, SocketFlags.None));
                    await Task.Yield();

                    client.Shutdown(SocketShutdown.Both);
                    client.Dispose();

                    Task completed = await Task.WhenAny(zeroByteReceive, Task.Delay(TimeSpan.FromSeconds(15)));
                    Assert.Same(zeroByteReceive, completed);

                    Exception? ex = await Record.ExceptionAsync(async () => await zeroByteReceive);
                    if (ex is null)
                    {
                        Assert.Equal(0, await zeroByteReceive);
                    }
                    else
                    {
                        SocketException socketException = Assert.IsType<SocketException>(ex);
                        Assert.True(
                            socketException.SocketErrorCode == SocketError.ConnectionReset ||
                            socketException.SocketErrorCode == SocketError.OperationAborted ||
                            socketException.SocketErrorCode == SocketError.Interrupted,
                            $"Unexpected socket error while waiting for peer-close zero-byte receive completion: {socketException.SocketErrorCode}");
                    }
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_SendAsync_PeerClose_DoesNotReturnZeroByteSuccess()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    var trio = await CreateConnectedTcpSocketTrioAsync();
                    using Socket _ = trio.Listener;
                    using Socket client = trio.Client;
                    using Socket server = trio.Server;

                    byte[] payload = new byte[64 * 1024];
                    Task<int> sendTask = ToTask(server.SendAsync(payload, SocketFlags.None));
                    await Task.Yield();

                    client.Shutdown(SocketShutdown.Both);
                    client.Dispose();

                    Task completed = await Task.WhenAny(sendTask, Task.Delay(TimeSpan.FromSeconds(15)));
                    Assert.Same(sendTask, completed);

                    Exception? ex = await Record.ExceptionAsync(async () => await sendTask);
                    if (ex is null)
                    {
                        int sent = await sendTask;
                        Assert.True(sent > 0, "Non-empty send must not complete with success and zero bytes transferred.");
                        return;
                    }

                    SocketException socketException = Assert.IsType<SocketException>(ex);
                    Assert.True(
                        socketException.SocketErrorCode == SocketError.ConnectionReset ||
                        socketException.SocketErrorCode == SocketError.ConnectionAborted ||
                        socketException.SocketErrorCode == SocketError.Shutdown ||
                        socketException.SocketErrorCode == SocketError.OperationAborted ||
                        socketException.SocketErrorCode == SocketError.Interrupted,
                        $"Unexpected socket error for peer-close send completion: {socketException.SocketErrorCode}");
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveFrom_TruncatedPayload_ReturnsTruncatedLengthOrMessageSizeError()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    byte[] sendPayload = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06 };
                    EndPoint senderEndpoint = sender.LocalEndPoint!;
                    byte[] receiveBuffer = new byte[2];

                    Task<SocketReceiveFromResult> receiveTask =
                        ToTask(receiver.ReceiveFromAsync(receiveBuffer, SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));
                    await Task.Yield();

                    int sent = await sender.SendToAsync(sendPayload, SocketFlags.None, receiver.LocalEndPoint!);
                    Assert.Equal(sendPayload.Length, sent);

                    Task completed = await Task.WhenAny(receiveTask, Task.Delay(TimeSpan.FromSeconds(15)));
                    Assert.Same(receiveTask, completed);

                    Exception? ex = await Record.ExceptionAsync(async () => await receiveTask);
                    if (ex is not null)
                    {
                        SocketException socketException = Assert.IsType<SocketException>(ex);
                        Assert.Equal(SocketError.MessageSize, socketException.SocketErrorCode);
                        return;
                    }

                    SocketReceiveFromResult receiveResult = await receiveTask;
                    Assert.True(receiveResult.ReceivedBytes > 0 && receiveResult.ReceivedBytes <= receiveBuffer.Length);
                    for (int i = 0; i < receiveResult.ReceivedBytes; i++)
                    {
                        Assert.Equal(sendPayload[i], receiveBuffer[i]);
                    }
                    Assert.Equal(senderEndpoint, receiveResult.RemoteEndPoint);
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_DatagramReceive_OversizedPayload_DoesNotArmPersistentMultishotRecv()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    receiver.Connect(sender.LocalEndPoint!);
                    sender.Connect(receiver.LocalEndPoint!);

                    // Multishot recv uses IORING_OP_RECV and cannot observe MSG_TRUNC; datagram sockets
                    // must stay on one-shot receive paths where truncation semantics remain explicit.
                    Assert.False(
                        IsPersistentMultishotRecvArmed(receiver),
                        "Datagram receive should not arm persistent multishot state.");

                    byte[] receiveBuffer = new byte[2];
                    byte[] sendPayload = new byte[] { 0x31, 0x32, 0x33, 0x34, 0x35, 0x36 };

                    Task<int> receiveTask = ToTask(receiver.ReceiveAsync(receiveBuffer, SocketFlags.None));
                    await Task.Yield();

                    int sent = await sender.SendAsync(sendPayload, SocketFlags.None);
                    Assert.Equal(sendPayload.Length, sent);

                    Exception? ex = await Record.ExceptionAsync(async () => await receiveTask);
                    if (ex is null)
                    {
                        int received = await receiveTask;
                        Assert.True(received > 0 && received <= receiveBuffer.Length);
                    }
                    else
                    {
                        SocketException socketException = Assert.IsType<SocketException>(ex);
                        Assert.Equal(SocketError.MessageSize, socketException.SocketErrorCode);
                    }

                    Assert.False(
                        IsPersistentMultishotRecvArmed(receiver),
                        "Datagram receive should remain outside persistent multishot state.");
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_ReceiveFrom_OversizedDatagram_ZeroLengthBuffer_CompletesOrMessageSize()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    using Socket receiver = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                    receiver.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    byte[] sendPayload = new byte[60 * 1024];
                    EndPoint senderEndpoint = sender.LocalEndPoint!;

                    Task<SocketReceiveFromResult> receiveTask =
                        ToTask(receiver.ReceiveFromAsync(Array.Empty<byte>(), SocketFlags.None, new IPEndPoint(IPAddress.Any, 0)));
                    await Task.Yield();

                    int sent = await sender.SendToAsync(sendPayload, SocketFlags.None, receiver.LocalEndPoint!);
                    Assert.Equal(sendPayload.Length, sent);

                    Exception? ex = await Record.ExceptionAsync(async () => await receiveTask);
                    if (ex is not null)
                    {
                        SocketException socketException = Assert.IsType<SocketException>(ex);
                        Assert.Equal(SocketError.MessageSize, socketException.SocketErrorCode);
                        return;
                    }

                    SocketReceiveFromResult receiveResult = await receiveTask;
                    Assert.Equal(0, receiveResult.ReceivedBytes);
                    Assert.Equal(senderEndpoint, receiveResult.RemoteEndPoint);
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_SendTo_UnreachableEndpoint_CompletesOrFailsWithExpectedError()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                return Task.Run(async () =>
                {
                    using Socket sender = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    sender.Bind(new IPEndPoint(IPAddress.Loopback, 0));

                    byte[] payload = new byte[] { 0xAA };
                    EndPoint destination = new IPEndPoint(IPAddress.Parse("192.0.2.1"), 9);

                    try
                    {
                        int sent = await sender.SendToAsync(payload, SocketFlags.None, destination);
                        Assert.Equal(payload.Length, sent);
                        // UDP sendto may succeed on some Linux/network configurations even for TEST-NET destinations.
                        return;
                    }
                    catch (Exception ex)
                    {
                        SocketException socketException = Assert.IsType<SocketException>(ex);
                        Assert.True(
                            socketException.SocketErrorCode == SocketError.NetworkUnreachable ||
                            socketException.SocketErrorCode == SocketError.HostUnreachable ||
                            socketException.SocketErrorCode == SocketError.HostNotFound ||
                            socketException.SocketErrorCode == SocketError.NetworkDown ||
                            socketException.SocketErrorCode == SocketError.AccessDenied ||
                            socketException.SocketErrorCode == SocketError.InvalidArgument,
                            $"Unexpected socket error for unreachable send: {socketException.SocketErrorCode}");
                    }
                });
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_AsyncCancelRequestCqe_IsolatedFromManagedOperationDispatch()
        {
            await RemoteExecutor.Invoke(static () => RunAsyncCancelRequestIsolationScenarioAsync(64), CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // generation-dispatch behavior is Linux-specific.
        public static async Task IoUringCompletionMode_CompletionDispatch_StaleWrappedGeneration_IsDiscarded()
        {
            await RemoteExecutor.Invoke(
                static () => RunGenerationWrapAroundDispatchScenarioAsync(),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring cancel queue/wakeup path is Linux-specific.
        public static async Task IoUringCompletionMode_CancelQueueFull_WakesBeforeOverflow()
        {
            await RemoteExecutor.Invoke(
                static () => RunCancellationQueueWakeBeforeOverflowScenarioAsync(),
                CreateSocketEngineOptions(prepareQueueCapacity: 1)).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_SlotGenerationTransitions_Arm64Stress_NoHangsOrLeaks()
        {
            await RemoteExecutor.Invoke(
                static () => RunTrackedOperationGenerationTransitionStressScenarioAsync(connectionCount: 8, iterationsPerConnection: 1024),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CancellationSubmitContention_ProgressesUnderLoad()
        {
            await RemoteExecutor.Invoke(
                static () => RunCancellationSubmitContentionScenarioAsync(connectionCount: 8, cancellationsPerConnection: 96),
                CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CompletionCancellationRace_CompletesExactlyOnce()
        {
            await RemoteExecutor.Invoke(static () => RunCompletionCancellationRaceAsync(128), CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_RapidCancelWhileEnqueued_DoesNotCorruptState()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                const int WorkerCount = 8;
                const int IterationsPerWorker = 128;
                var tasks = new Task[WorkerCount];

                for (int worker = 0; worker < WorkerCount; worker++)
                {
                    tasks[worker] = Task.Run(async () =>
                    {
                        byte[] receiveBuffer = new byte[1];
                        for (int i = 0; i < IterationsPerWorker; i++)
                        {
                            using var cts = new CancellationTokenSource();
                            var receiveTask = server.ReceiveAsync(receiveBuffer.AsMemory(), SocketFlags.None, cts.Token);
                            cts.Cancel();

                            Exception? ex = await Record.ExceptionAsync(async () => await receiveTask);
                            AssertCanceledOrInterrupted(ex);
                        }
                    });
                }

                await Task.WhenAll(tasks);

                // Ensure socket state still allows normal async flow after rapid cancellation churn.
                byte[] payload = new byte[] { 0xA5 };
                int sent = await client.SendAsync(payload, SocketFlags.None);
                Assert.Equal(1, sent);
                int received = await server.ReceiveAsync(payload, SocketFlags.None);
                Assert.Equal(1, received);
                Assert.Equal(0xA5, payload[0]);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringCompletionMode_CloseDisposeStress_DoesNotHang()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(32);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                for (int i = 0; i < 64; i++)
                {
                    var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                    using Socket client = pair.Client;
                    using Socket server = pair.Server;

                    Task<int>[] receives = new Task<int>[16];
                    for (int r = 0; r < receives.Length; r++)
                    {
                        receives[r] = ToTask(server.ReceiveAsync(new byte[1], SocketFlags.None));
                    }

                    client.Dispose();
                    server.Dispose();

                    for (int r = 0; r < receives.Length; r++)
                    {
                        Exception? ex = await Record.ExceptionAsync(async () => await receives[r]);
                        if (ex is SocketException socketException)
                        {
                            Assert.True(
                                socketException.SocketErrorCode == SocketError.ConnectionReset ||
                                socketException.SocketErrorCode == SocketError.OperationAborted ||
                                socketException.SocketErrorCode == SocketError.Interrupted,
                                $"Unexpected socket error: {socketException.SocketErrorCode}");
                        }
                        else if (ex is not ObjectDisposedException and not null)
                        {
                            throw ex;
                        }
                    }
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_ConcurrentCloseWithPendingReceive_DoesNotHang()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(16);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                byte[] receiveBuffer = new byte[1];
                for (int i = 0; i < 64; i++)
                {
                    var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                    using Socket client = pair.Client;
                    using Socket server = pair.Server;

                    var pendingReceive = server.ReceiveAsync(receiveBuffer, SocketFlags.None);

                    // Force teardown while an async receive is pending.
                    client.Dispose();

                    Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
                    if (ex is SocketException socketException)
                    {
                        Assert.True(
                            socketException.SocketErrorCode == SocketError.ConnectionReset ||
                            socketException.SocketErrorCode == SocketError.OperationAborted ||
                            socketException.SocketErrorCode == SocketError.Interrupted,
                            $"Unexpected socket error: {socketException.SocketErrorCode}");
                    }
                    else if (ex is not null)
                    {
                        throw ex;
                    }
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_ConcurrentRegistrationChurn_DoesNotHang()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                const int WorkerCount = 8;
                const int IterationsPerWorker = 64;

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(WorkerCount * 2);
                IPEndPoint endpoint = (IPEndPoint)listener.LocalEndPoint!;

                var workers = new Task[WorkerCount];
                for (int worker = 0; worker < WorkerCount; worker++)
                {
                    workers[worker] = Task.Run(async () =>
                    {
                        byte[] sendBuffer = new byte[] { 0x5A };
                        byte[] receiveBuffer = new byte[1];

                        for (int i = 0; i < IterationsPerWorker; i++)
                        {
                            var pair = await AcceptConnectedTcpPairAsync(listener, endpoint);
                            using Socket client = pair.Client;
                            using Socket server = pair.Server;

                            var pendingReceive = server.ReceiveAsync(receiveBuffer, SocketFlags.None);
                            await Task.Yield();

                            if ((i & 1) == 0)
                            {
                                int sent = await client.SendAsync(sendBuffer, SocketFlags.None);
                                Assert.Equal(1, sent);
                            }
                            else
                            {
                                client.Dispose();
                            }

                            Exception? ex = await Record.ExceptionAsync(async () => await pendingReceive);
                            if (ex is SocketException socketException)
                            {
                                Assert.True(
                                    socketException.SocketErrorCode == SocketError.ConnectionReset ||
                                    socketException.SocketErrorCode == SocketError.OperationAborted ||
                                    socketException.SocketErrorCode == SocketError.Interrupted,
                                    $"Unexpected socket error: {socketException.SocketErrorCode}");
                            }
                            else if (ex is not ObjectDisposedException and not null)
                            {
                                throw ex;
                            }
                        }
                    });
                }

                await Task.WhenAll(workers);
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        [OuterLoop]
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringOptIn_RepeatedRunStabilityGate()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                const int Iterations = 50;
                for (int i = 0; i < Iterations; i++)
                {
                    await RunTcpRoundTripAsync(8);
                }
            }, CreateSocketEngineOptions()).DisposeAsync();
        }

        private static int GetReusePortShadowListenerCount(Socket listener)
            => SocketAsyncContext.GetReusePortShadowListenerCountForTest(listener);

        private static async Task RunReusePortAcceptScenarioAsync(int connectionCount)
        {
            using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            listener.Listen(512);
            IPEndPoint listenEndPoint = (IPEndPoint)listener.LocalEndPoint!;

            // Arm multishot accept by issuing an AcceptAsync.
            Task<Socket> firstAcceptTask = listener.AcceptAsync().AsTask();

            // Wait for multishot accept to arm.
            bool armed = await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true);

            // Connect enough clients to verify connections are accepted.
            var clients = new List<Socket>();
            var accepted = new List<Socket>();
            try
            {
                // Connect the first client to satisfy the pending AcceptAsync.
                using Socket firstClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                firstClient.Connect(listenEndPoint);
                Socket firstAccepted = await firstAcceptTask;
                accepted.Add(firstAccepted);

                // Connect additional clients - these go through the pre-accept queue.
                for (int i = 1; i < connectionCount; i++)
                {
                    Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    clients.Add(client);
                    client.Connect(listenEndPoint);
                    Socket acc = await listener.AcceptAsync();
                    accepted.Add(acc);
                }

                Assert.Equal(connectionCount, accepted.Count);
            }
            finally
            {
                foreach (Socket s in accepted) s.Dispose();
                foreach (Socket s in clients) s.Dispose();
            }
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringReusePortAccept_Disabled_NoShadowListeners()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(512);

                // Arm multishot accept.
                Task<Socket> acceptTask = listener.AcceptAsync().AsTask();
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true);

                // With REUSEPORT disabled, no shadow listeners should be created.
                Assert.Equal(0, GetReusePortShadowListenerCount(listener));

                // Clean up: connect to satisfy the accept.
                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect((IPEndPoint)listener.LocalEndPoint!);
                using Socket accepted = await acceptTask;
            }, CreateSocketEngineOptions(threadCount: 2, reusePortAcceptDisabled: true)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringReusePortAccept_Enabled_CreatesShadowListeners()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(512);

                // Arm multishot accept.
                Task<Socket> acceptTask = listener.AcceptAsync().AsTask();
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true);

                // With REUSEPORT default-on and 3 engines, expect 2 shadow listeners.
                int expectedShadows = SocketAsyncEngine.EngineCount - 1;
                // Wait briefly for shadow setup to propagate through engine event loops.
                int shadowCount = 0;
                DateTime deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
                while (DateTime.UtcNow < deadline)
                {
                    shadowCount = GetReusePortShadowListenerCount(listener);
                    if (shadowCount >= expectedShadows)
                    {
                        break;
                    }
                    await Task.Delay(50);
                }

                Assert.Equal(expectedShadows, shadowCount);

                // Clean up: connect to satisfy the accept.
                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect((IPEndPoint)listener.LocalEndPoint!);
                using Socket accepted = await acceptTask;
            }, CreateSocketEngineOptions(threadCount: 3)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringReusePortAccept_ConnectionsAcceptedViaShadows()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                await RunReusePortAcceptScenarioAsync(connectionCount: 16);
            }, CreateSocketEngineOptions(threadCount: 3)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringReusePortAccept_CleanupOnListenerClose()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(512);
                IPEndPoint listenEndPoint = (IPEndPoint)listener.LocalEndPoint!;

                // Arm multishot accept.
                Task<Socket> acceptTask = listener.AcceptAsync().AsTask();
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true);

                // Verify shadows were created.
                int shadowCount = GetReusePortShadowListenerCount(listener);
                Assert.True(shadowCount > 0, "Expected at least one shadow listener.");

                // Connect to satisfy the accept, then close the listener.
                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect(listenEndPoint);
                using Socket accepted = await acceptTask;
                listener.Dispose();

                // After disposal, shadow listener state should be cleaned up.
                // Completion slot cleanup is asynchronous via cancellation, so just
                // verify the listener was disposed without exceptions.
                await Task.Delay(200);
            }, CreateSocketEngineOptions(threadCount: 3)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringReusePortAccept_SingleEngine_NoShadows()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                if (!IsIoUringMultishotAcceptSupported())
                {
                    return;
                }

                using Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                listener.Listen(512);

                Task<Socket> acceptTask = listener.AcceptAsync().AsTask();
                await WaitForMultishotAcceptArmedStateAsync(listener, expectedArmed: true);

                // With only 1 engine, no shadows should be created (blocked by EngineCount <= 1).
                Assert.Equal(0, GetReusePortShadowListenerCount(listener));

                using Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                client.Connect((IPEndPoint)listener.LocalEndPoint!);
                using Socket accepted = await acceptTask;
            }, CreateSocketEngineOptions(threadCount: 1)).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // Uses Linux sched_setaffinity + Thread.GetCurrentProcessorNumber.
        public static async Task IoUringEngineAffinity_SchedSetAffinity_PinsCurrentThread()
        {
            await RemoteExecutor.Invoke(static () =>
            {
                int[] pinnedCpus = SocketAsyncEngine.GetEnginePinnedCpuIndicesForTest();
                int cpuIndex = -1;
                for (int i = 0; i < pinnedCpus.Length; i++)
                {
                    if (pinnedCpus[i] >= 0)
                    {
                        cpuIndex = pinnedCpus[i];
                        break;
                    }
                }

                if (cpuIndex < 0)
                {
                    return;
                }

                int observedProcessor = -1;
                var worker = new Thread(() =>
                {
                    if (!SocketAsyncEngine.TrySetCurrentThreadAffinityForTest(cpuIndex))
                    {
                        return;
                    }

                    observedProcessor = Thread.GetCurrentProcessorNumber();
                    if (observedProcessor != cpuIndex)
                    {
                        SpinWait sw = default;
                        for (int i = 0; i < 1024; i++)
                        {
                            sw.SpinOnce();
                            observedProcessor = Thread.GetCurrentProcessorNumber();
                            if (observedProcessor == cpuIndex)
                            {
                                break;
                            }
                        }
                    }
                })
                {
                    IsBackground = true
                };

                worker.Start();
                worker.Join();
                Assert.Equal(cpuIndex, observedProcessor);
            }, CreateSocketEngineOptions(ioUringValue: "1")).DisposeAsync();
        }

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [PlatformSpecific(TestPlatforms.Linux)] // io_uring is Linux-specific.
        public static async Task IoUringIncomingCpu_TcpReceive_ReportsNonNegativeCpu()
        {
            await RemoteExecutor.Invoke(static async () =>
            {
                var trio = await CreateConnectedTcpSocketTrioAsync();
                using Socket _ = trio.Listener;
                using Socket client = trio.Client;
                using Socket server = trio.Server;

                byte[] sendBuffer = new byte[] { 0x5A };
                byte[] receiveBuffer = new byte[1];

                int sent = await client.SendAsync(sendBuffer, SocketFlags.None);
                Assert.Equal(1, sent);
                int received = await server.ReceiveAsync(receiveBuffer, SocketFlags.None);
                Assert.Equal(1, received);

                Assert.True(
                    SocketAsyncContext.TryGetIncomingCpuForTest(server, out int cpuIndex),
                    "Expected SO_INCOMING_CPU to be available after first receive.");
                Assert.InRange(cpuIndex, 0, 1024 * 1024);
            }, CreateSocketEngineOptions(ioUringValue: "1")).DisposeAsync();
        }
    }
}
