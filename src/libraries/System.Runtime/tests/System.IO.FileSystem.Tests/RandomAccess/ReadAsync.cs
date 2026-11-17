// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [SkipOnPlatform(TestPlatforms.Browser, "async file IO is not supported on browser")]
    public class RandomAccess_ReadAsync : RandomAccess_Base<ValueTask<int>>
    {
        protected override ValueTask<int> MethodUnderTest(SafeFileHandle handle, byte[] bytes, long fileOffset)
            => RandomAccess.ReadAsync(handle, bytes, fileOffset);

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task TaskAlreadyCanceledAsync(FileOptions options)
        {
            using (SafeFileHandle handle = File.OpenHandle(GetTestFilePath(), FileMode.CreateNew, FileAccess.ReadWrite, options: options))
            {
                CancellationTokenSource cts = GetCancelledTokenSource();
                CancellationToken token = cts.Token;

                Assert.True(RandomAccess.ReadAsync(handle, new byte[1], 0, token).IsCanceled);

                TaskCanceledException ex = await Assert.ThrowsAsync<TaskCanceledException>(() => RandomAccess.ReadAsync(handle, new byte[1], 0, token).AsTask());
                Assert.Equal(token, ex.CancellationToken);
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task ThrowsOnWriteAccess(FileOptions options)
        {
            using (SafeFileHandle handle = GetHandleToExistingFile(FileAccess.Write, options))
            {
                await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await RandomAccess.ReadAsync(handle, new byte[1], 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task ReadToAnEmptyBufferReturnsZeroAsync(FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[1]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                Assert.Equal(0, await RandomAccess.ReadAsync(handle, Array.Empty<byte>(), fileOffset: 0));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task ReadFromBeyondEndOfFileReturnsZeroAsync(FileOptions options)
        {
            string filePath = GetTestFilePath();
            File.WriteAllBytes(filePath, new byte[100]);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                long eof = RandomAccess.GetLength(handle);
                Assert.Equal(0, await RandomAccess.ReadAsync(handle, new byte[1], fileOffset: eof + 1));
            }
        }

        [Theory]
        [MemberData(nameof(GetSyncAsyncOptions))]
        public async Task HappyPath(FileOptions options)
        {
            const int fileSize = 4_001;
            string filePath = GetTestFilePath();
            byte[] expected = RandomNumberGenerator.GetBytes(fileSize);
            File.WriteAllBytes(filePath, expected);

            using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Open, options: options))
            {
                byte[] actual = new byte[fileSize + 1];
                int current = 0;
                int total = 0;

                do
                {
                    Memory<byte> buffer = actual.AsMemory(total, Math.Min(actual.Length - total, fileSize / 4));

                    current = await RandomAccess.ReadAsync(handle, buffer, fileOffset: total);

                    Assert.InRange(current, 0, buffer.Length);

                    total += current;
                } while (current != 0);

                Assert.Equal(fileSize, total);
                Assert.Equal(expected, actual.Take(total).ToArray());
            }
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        [PlatformSpecific(TestPlatforms.Windows)]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void HandlesAreFinalizedWhenNotDisposed(bool asyncHandle, bool cancellable)
        {
            string path = GetTestFilePath();
            File.WriteAllBytes(path, new byte[] { 42 });

            WeakReference<SafeFileHandle> handle = Read(path, asyncHandle, cancellable);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.False(handle.TryGetTarget(out _));

            // The helper is synchronous on purpose: the continuation of an async method references the handle
            // until the thread that completed it is done with it, which races with the GC above.
            [MethodImpl(MethodImplOptions.NoInlining)]
            static WeakReference<SafeFileHandle> Read(string path, bool asyncHandle, bool cancellable)
            {
                SafeFileHandle handle = File.OpenHandle(path, FileMode.Open, FileAccess.Read, options: asyncHandle ? FileOptions.Asynchronous : FileOptions.None);

                CancellationToken cancellationToken = default;
                if (cancellable)
                {
                    // The registration makes the handle reachable from the cancellation token source.
                    CancellationTokenSource cts = new();
                    cts.Token.Register(() => handle.Dispose());
                    cancellationToken = cts.Token;
                }

                Assert.Equal(1, RandomAccess.ReadAsync(handle, new byte[1], fileOffset: 0, cancellationToken).AsTask().Result);

                return new(handle, trackResurrection: true);
            }
        }
    }
}
