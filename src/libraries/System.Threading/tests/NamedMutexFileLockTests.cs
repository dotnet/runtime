// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Threading.Tests
{
    public class NamedMutexFileLockTests : FileCleanupTestBase
    {
        // Linux arm and arm64 use the file lock (non-pthread) named mutex implementation, which keeps a timed waiter count in shared memory.
        private static bool IsRemoteExecutorAndLinuxFileLockNamedMutex =>
            RemoteExecutor.IsSupported
            && PlatformDetection.IsNotMobile
            && OperatingSystem.IsLinux()
            && RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64;

        [ConditionalFact(nameof(IsRemoteExecutorAndLinuxFileLockNamedMutex))]
        [PlatformSpecific(TestPlatforms.Linux)]
        [UnsupportedOSPlatform("windows")]
        public void InterruptedTimedWait_RestoresTimedWaiterCount()
        {
            string userRuntimeDirectory = GetTestFilePath();
            Directory.CreateDirectory(userRuntimeDirectory);

            RemoteInvokeOptions remoteInvokeOptions = new();
            remoteInvokeOptions.StartInfo.EnvironmentVariables["XDG_RUNTIME_DIR"] = userRuntimeDirectory;

            string name = Guid.NewGuid().ToString("N");
            string mutexName = $"Global\\{name}";
            string sharedMemoryPath = Path.Combine(userRuntimeDirectory, ".dotnet", "shm", "global", name);
            string heldFilePath = GetTestFilePath();
            string releaseFilePath = GetTestFilePath();

            // The process-level lock is private to each process, so the mutex has to be held by a different process
            // than the one that waits for the timed wait to reach the shared timed waiter count.
            using RemoteInvokeHandle holder = RemoteExecutor.Invoke(
                (mutexName, heldFilePath, releaseFilePath) =>
                {
                    using Mutex mutex = new(true, mutexName, out bool createdNew);
                    Assert.True(createdNew);
                    File.WriteAllText(heldFilePath, string.Empty);
                    Assert.True(SpinWait.SpinUntil(() => File.Exists(releaseFilePath), ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                    mutex.ReleaseMutex();
                },
                mutexName,
                heldFilePath,
                releaseFilePath,
                remoteInvokeOptions);

            try
            {
                Assert.True(SpinWait.SpinUntil(() => File.Exists(heldFilePath), ThreadTestHelpers.UnexpectedTimeoutMilliseconds));

                RemoteExecutor.Invoke(
                    (mutexName, sharedMemoryPath) =>
                    {
                        // The count is the first field after the 8-byte shared memory header.
                        const int TimedWaiterCountOffset = 8;

                        static uint ReadTimedWaiterCount(string path)
                        {
                            using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                            stream.Position = TimedWaiterCountOffset;
                            Span<byte> buffer = stackalloc byte[sizeof(uint)];
                            stream.ReadExactly(buffer);
                            return BitConverter.ToUInt32(buffer);
                        }

                        using Mutex mutex = Mutex.OpenExisting(mutexName);
                        Assert.Equal(0u, ReadTimedWaiterCount(sharedMemoryPath));

                        Exception? exception = null;
                        Thread waiter = new(() =>
                        {
                            try
                            {
                                mutex.WaitOne(ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                            }
                        });
                        waiter.IsBackground = true;
                        waiter.Start();

                        SpinWait.SpinUntil(
                            () => ReadTimedWaiterCount(sharedMemoryPath) != 0,
                            ThreadTestHelpers.UnexpectedTimeoutMilliseconds);
                        Assert.Equal(1u, ReadTimedWaiterCount(sharedMemoryPath));

                        waiter.Interrupt();
                        Assert.True(waiter.Join(ThreadTestHelpers.UnexpectedTimeoutMilliseconds));
                        Assert.IsType<ThreadInterruptedException>(exception);
                        Assert.Equal(0u, ReadTimedWaiterCount(sharedMemoryPath));
                    },
                    mutexName,
                    sharedMemoryPath,
                    remoteInvokeOptions).Dispose();
            }
            finally
            {
                File.WriteAllText(releaseFilePath, string.Empty);
            }
        }
    }
}
