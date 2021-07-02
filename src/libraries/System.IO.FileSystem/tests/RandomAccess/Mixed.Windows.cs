// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
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

                        Assert.Equal(writeBuffer.Length, syncWrite ? RandomAccess.Write(handle, writeBuffer, fileOffset) : await RandomAccess.WriteAsync(handle, writeBuffer, fileOffset));
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

                        Assert.Equal(writeBuffer_1.Length + writeBuffer_2.Length, syncWrite ? RandomAccess.Write(handle, writeBuffers, fileOffset) : await RandomAccess.WriteAsync(handle, writeBuffers, fileOffset));
                        Assert.Equal(writeBuffer_1.Length + writeBuffer_2.Length, syncRead ? RandomAccess.Read(handle, readBuffers, fileOffset) : await RandomAccess.ReadAsync(handle, readBuffers, fileOffset));
                        Assert.Equal(writeBuffer_1[0], readBuffer_1[0]);
                        Assert.Equal(writeBuffer_2[0], readBuffer_2[0]);

                        fileOffset += 2;
                    }
                }
            }
        }
    }
}
