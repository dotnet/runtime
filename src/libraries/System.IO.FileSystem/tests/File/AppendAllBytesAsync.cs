// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendAllBytesAsync : FileSystemTest
    {

        [Fact]
        public async Task NullParametersAsync()
        {
            string path = GetTestFilePath();

            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await File.AppendAllBytesAsync(null, new byte[0]));
            await Assert.ThrowsAsync<ArgumentNullException>("bytes", async () => await File.AppendAllBytesAsync(path, null));
        }

        [Fact]
        public void NonExistentPathAsync()
        {
            Assert.ThrowsAsync<DirectoryNotFoundException>(() => File.AppendAllBytesAsync(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), new byte[0]));
        }

        [Fact]
        public async Task InvalidParametersAsync()
        {
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await File.AppendAllBytesAsync(string.Empty, new byte[0]));
        }

        [Fact]
        public async Task AppendAllBytesAsync_WithValidInput_AppendsBytes()
        {
            string path = GetTestFilePath();

            byte[] initialBytes = Encoding.UTF8.GetBytes("bytes");
            byte[] additionalBytes = Encoding.UTF8.GetBytes("additional bytes");

            await File.WriteAllBytesAsync(path, initialBytes);
            await File.AppendAllBytesAsync(path, additionalBytes);

            byte[] result = await File.ReadAllBytesAsync(path);

            byte[] expectedBytes = initialBytes.Concat(additionalBytes).ToArray();

            Assert.True(result.SequenceEqual(expectedBytes));
        }

        [Fact]
        public async Task EmptyContentCreatesFileAsync()
        {
            string path = GetTestFilePath();
            await File.AppendAllBytesAsync(path, new byte[0]);
            Assert.True(File.Exists(path));
            Assert.Empty(await File.ReadAllBytesAsync(path));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
        public async Task OpenFile_ThrowsIOExceptionAsync()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes("bytes");

            using (File.Create(path))
            {
                await Assert.ThrowsAsync<IOException>(async () => await File.AppendAllBytesAsync(path, bytes));
            }
        }

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed. On Windows, modifying a file that is ReadOnly will always fail.
        /// </summary>
        [Fact]
        public async Task AppendToReadOnlyFileAsync()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            byte[] dataToAppend = Encoding.UTF8.GetBytes("bytes");

            try
            {
                if (PlatformDetection.IsNotWindows && PlatformDetection.IsPrivilegedProcess)
                {
                    await File.AppendAllBytesAsync(path, dataToAppend);
                    Assert.Equal(dataToAppend, await File.ReadAllBytesAsync(path));
                }
                else
                {
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await File.AppendAllBytesAsync(path, dataToAppend));
                }
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }
    }
}
