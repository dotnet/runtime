// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_ReadWriteAllBytesAsync : FileSystemTest
    {
        [Fact]
        public async Task NullParametersAsync()
        {
            string path = GetTestFilePath();
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await File.WriteAllBytesAsync(null, new byte[0]));
            await Assert.ThrowsAsync<ArgumentNullException>("bytes", async () => await File.WriteAllBytesAsync(path, null));
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await File.ReadAllBytesAsync(null));
        }

        [Fact]
        public async Task InvalidParametersAsync()
        {
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await File.WriteAllBytesAsync(string.Empty, new byte[0]));
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await File.ReadAllBytesAsync(string.Empty));
        }

        [Fact]
        public Task Read_FileNotFoundAsync()
        {
            string path = GetTestFilePath();
            return Assert.ThrowsAsync<FileNotFoundException>(async () => await File.ReadAllBytesAsync(path));
        }

        [Fact]
        public async Task EmptyContentCreatesFileAsync()
        {
            string path = GetTestFilePath();
            await File.WriteAllBytesAsync(path, new byte[0]);
            Assert.True(File.Exists(path));
            Assert.Empty(await File.ReadAllTextAsync(path));
            File.Delete(path);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public async Task ValidWriteAsync(int size)
        {
            string path = GetTestFilePath();
            byte[] buffer = Encoding.UTF8.GetBytes(new string('c', size));
            await File.WriteAllBytesAsync(path, buffer);
            Assert.Equal(buffer, await File.ReadAllBytesAsync(path));
            File.Delete(path);
        }

        [Fact]
        public Task AlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.WriteAllBytesAsync(path, new byte[0], token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.WriteAllBytesAsync(path, new byte[0], token));
        }

        [Fact]
        [OuterLoop]
        public Task ReadFileOver2GBAsync()
        {
            string path = GetTestFilePath();
            using (FileStream fs = File.Create(path))
            {
                fs.SetLength(int.MaxValue + 1L);
            }

            // File is too large for ReadAllBytes at once
            return Assert.ThrowsAsync<IOException>(async () => await File.ReadAllBytesAsync(path));
        }

        [Fact]
        public async Task OverwriteAsync()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            byte[] overwriteBytes = Encoding.UTF8.GetBytes(new string('b', 50));
            await File.WriteAllBytesAsync(path, bytes);
            await File.WriteAllBytesAsync(path, overwriteBytes);
            Assert.Equal(overwriteBytes, await File.ReadAllBytesAsync(path));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
        public async Task OpenFile_ThrowsIOExceptionAsync()
        {
            string path = GetTestFilePath();
            byte[] bytes = Encoding.UTF8.GetBytes(new string('c', 100));
            using (File.Create(path))
            {
                await Assert.ThrowsAsync<IOException>(async () => await File.WriteAllBytesAsync(path, bytes));
                await Assert.ThrowsAsync<IOException>(async () => await File.ReadAllBytesAsync(path));
            }
        }

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
        public async Task WriteToReadOnlyFileAsync()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                // Operation succeeds when being run by the Unix superuser
                if (PlatformDetection.IsSuperUser)
                {
                    await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes("text"));
                    Assert.Equal(Encoding.UTF8.GetBytes("text"), await File.ReadAllBytesAsync(path));
                }
                else
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await File.WriteAllBytesAsync(path, Encoding.UTF8.GetBytes("text")));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public async Task EmptyFile_ReturnsEmptyArray()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            Assert.Equal(0, (await File.ReadAllBytesAsync(path)).Length);
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("/proc/cmdline")]
        [InlineData("/proc/version")]
        [InlineData("/proc/filesystems")]
        public async Task ProcFs_EqualsReadAllText(string path)
        {
            byte[] bytes = null;
            string text = null;

            const int NumTries = 3; // some of these could theoretically change between reads, so allow retries just in case
            for (int i = 1; i <= NumTries; i++)
            {
                try
                {
                    bytes = await File.ReadAllBytesAsync(path);
                    text = await File.ReadAllTextAsync(path);
                    Assert.Equal(text, Encoding.UTF8.GetString(bytes));
                }
                catch when (i < NumTries) { }
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Linux)]
        public async Task ReadAllBytes_ProcFs_Uptime_ContainsTwoNumbers()
        {
            string text = Encoding.UTF8.GetString(await File.ReadAllBytesAsync("/proc/uptime"));
            string[] parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(2, parts.Length);
            Assert.True(double.TryParse(parts[0].Trim(), out _));
            Assert.True(double.TryParse(parts[1].Trim(), out _));
        }

        [Theory]
        [PlatformSpecific(TestPlatforms.Linux)]
        [InlineData("/proc/meminfo")]
        [InlineData("/proc/stat")]
        [InlineData("/proc/cpuinfo")]
        public async Task ProcFs_NotEmpty(string path)
        {
            Assert.InRange((await File.ReadAllBytesAsync(path)).Length, 1, int.MaxValue);
        }
    }
}
