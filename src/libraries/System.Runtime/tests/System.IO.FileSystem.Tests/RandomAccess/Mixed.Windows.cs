// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [PlatformSpecific(TestPlatforms.Windows)]
    public class RandomAccess_Mixed : FileSystemTest
    {
        [DllImport(Interop.Libraries.Kernel32, EntryPoint = "CreateFileW", SetLastError = true, CharSet = CharSet.Unicode, BestFitMapping = false, ExactSpelling = true)]
        private static extern unsafe SafeFileHandle CreateFileW(
             string lpFileName,
             FileAccess dwDesiredAccess,
             FileShare dwShareMode,
             IntPtr lpSecurityAttributes,
             FileMode dwCreationDisposition,
             int dwFlagsAndAttributes,
             IntPtr hTemplateFile);

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UsingSingleBuffer(bool async)
        {
            string filePath = GetTestFilePath();
            FileOptions options = async ? FileOptions.Asynchronous : FileOptions.None;

            // we want to test all combinations: starting with sync|async write, then sync|async read etc
            foreach (bool syncWrite in new bool[] { true, false })
            {
                foreach (bool syncRead in new bool[] { true, false })
                {
                    // File.OpenHandle initializes ThreadPoolBinding for async file handles on Windows
                    using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.ReadWrite, options: options))
                    {
                        await Validate(handle, options, new bool[] { syncWrite, !syncWrite }, new bool[] { syncRead, !syncRead });
                    }

                    // tests code path where ThreadPoolBinding is not initialized
                    using (SafeFileHandle tpBindingNotInitialized = CreateFileW(filePath, FileAccess.ReadWrite, FileShare.None, IntPtr.Zero, FileMode.Create, (int)options, IntPtr.Zero))
                    {
                        await Validate(tpBindingNotInitialized, options, new bool[] { syncWrite, !syncWrite }, new bool[] { syncRead, !syncRead });
                    }
                }
            }

            static async Task Validate(SafeFileHandle handle, FileOptions options, bool[] syncWrites, bool[] syncReads)
            {
                byte[] writeBuffer = new byte[1];
                byte[] readBuffer = new byte[2];
                long fileOffset = 0;

                foreach (bool syncWrite in syncWrites)
                {
                    foreach (bool syncRead in syncReads)
                    {
                        writeBuffer[0] = (byte)fileOffset;

                        if (syncWrite)
                        {
                            RandomAccess.Write(handle, writeBuffer, fileOffset);
                        }
                        else
                        {
                            await RandomAccess.WriteAsync(handle, writeBuffer, fileOffset);
                        }

                        Assert.Equal(writeBuffer.Length, syncRead ? RandomAccess.Read(handle, readBuffer, fileOffset) : await RandomAccess.ReadAsync(handle, readBuffer, fileOffset));

                        Assert.Equal(writeBuffer[0], readBuffer[0]);

                        fileOffset += 1;
                    }
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task UsingMultipleBuffers(bool async)
        {
            string filePath = GetTestFilePath();
            FileOptions options = async ? FileOptions.Asynchronous : FileOptions.None;

            foreach (bool syncWrite in new bool[] { true, false })
            {
                foreach (bool syncRead in new bool[] { true, false })
                {
                    // File.OpenHandle initializes ThreadPoolBinding for async file handles on Windows
                    using (SafeFileHandle handle = File.OpenHandle(filePath, FileMode.Create, FileAccess.ReadWrite, options: options))
                    {
                        await Validate(handle, options, new bool[] { syncWrite, !syncWrite }, new bool[] { syncRead, !syncRead });
                    }

                    // tests code path where ThreadPoolBinding is not initialized
                    using (SafeFileHandle tpBindingNotInitialized = CreateFileW(filePath, FileAccess.ReadWrite, FileShare.None, IntPtr.Zero, FileMode.Create, (int)options, IntPtr.Zero))
                    {
                        await Validate(tpBindingNotInitialized, options, new bool[] { syncWrite, !syncWrite }, new bool[] { syncRead, !syncRead });
                    }
                }
            }

            static async Task Validate(SafeFileHandle handle, FileOptions options, bool[] syncWrites, bool[] syncReads)
            {
                byte[] writeBuffer_1 = new byte[1];
                byte[] writeBuffer_2 = new byte[1];
                byte[] readBuffer_1 = new byte[1];
                byte[] readBuffer_2 = new byte[1];
                long fileOffset = 0;

                IReadOnlyList<Memory<byte>> readBuffers = new Memory<byte>[] { readBuffer_1, readBuffer_2 };
                IReadOnlyList<ReadOnlyMemory<byte>> writeBuffers = new ReadOnlyMemory<byte>[] { writeBuffer_1, writeBuffer_2 };

                foreach (bool syncWrite in syncWrites)
                {
                    foreach (bool syncRead in syncReads)
                    {
                        writeBuffer_1[0] = (byte)fileOffset;
                        writeBuffer_2[0] = (byte)(fileOffset+1);

                        if (syncWrite)
                        {
                            RandomAccess.Write(handle, writeBuffers, fileOffset);
                        }
                        else
                        {
                            await RandomAccess.WriteAsync(handle, writeBuffers, fileOffset);
                        }

                        Assert.Equal(writeBuffer_1.Length + writeBuffer_2.Length, syncRead ? RandomAccess.Read(handle, readBuffers, fileOffset) : await RandomAccess.ReadAsync(handle, readBuffers, fileOffset));

                        Assert.Equal(writeBuffer_1[0], readBuffer_1[0]);
                        Assert.Equal(writeBuffer_2[0], readBuffer_2[0]);

                        fileOffset += 2;
                    }
                }
            }
        }

        [Fact]
        public void SyncIOOnAsyncHandle_DoesNotCorruptMemory_WhenSynchronizationContextThrows()
        {
            // This test verifies that when WaitOne() throws via SynchronizationContext,
            // the pending IO is properly canceled before freeing the NativeOverlapped,
            // preventing use-after-free / heap corruption.
            byte[] expectedData = new byte[1024];
            Random.Shared.NextBytes(expectedData);

            SynchronizationContext previous = SynchronizationContext.Current;
            try
            {
                ThrowingSynchronizationContext throwingContext = new();
                SynchronizationContext.SetSynchronizationContext(throwingContext);

                SafeFileHandle.CreateAnonymousPipe(
                    out SafeFileHandle readHandle,
                    out SafeFileHandle writeHandle,
                    asyncRead: true,
                    asyncWrite: false);

                using (readHandle)
                using (writeHandle)
                {
                    byte[] pendingReadBuffer = new byte[1];

                    // The ThrowingSynchronizationContext.Wait throws, which should be caught
                    // and the IO should be canceled gracefully.
                    Assert.Throws<InvalidOperationException>(() => RandomAccess.Read(readHandle, pendingReadBuffer, 0));

                    // Restore the previous context and verify the read handle is still usable.
                    SynchronizationContext.SetSynchronizationContext(previous);

                    RandomAccess.Write(writeHandle, expectedData, 0);

                    byte[] readBuffer = new byte[expectedData.Length];
                    int totalRead = 0;
                    while (totalRead < readBuffer.Length)
                    {
                        int bytesRead = RandomAccess.Read(readHandle, readBuffer.AsSpan(totalRead), totalRead);
                        if (bytesRead == 0)
                        {
                            break;
                        }

                        totalRead += bytesRead;
                    }

                    Assert.Equal(expectedData.Length, totalRead);
                    Assert.Equal(expectedData, readBuffer);
                }
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(previous);
            }
        }

        /// <summary>
        /// A SynchronizationContext that throws from Wait to simulate the scenario
        /// where WaitOne() can throw arbitrary exceptions via user code.
        /// </summary>
        private sealed class ThrowingSynchronizationContext : SynchronizationContext
        {
            public ThrowingSynchronizationContext()
            {
                SetWaitNotificationRequired();
            }

            public override int Wait(IntPtr[] waitHandles, bool waitAll, int millisecondsTimeout)
            {
                throw new InvalidOperationException("SynchronizationContext.Wait threw an exception");
            }
        }
    }
}
