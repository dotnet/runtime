// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_ReadWriteAllTextAsync : FileSystemTest
    {
        #region Utilities

        protected virtual Task WriteAsync(string path, string content) => File.WriteAllTextAsync(path, content);

        protected virtual Task<string> ReadAsync(string path) => File.ReadAllTextAsync(path);

        #endregion

        #region UniversalTests

        [Fact]
        public async Task NullParametersAsync()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await WriteAsync(null, "Text"));
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await ReadAsync(null));
        }

        [Fact]
        public Task NonExistentPathAsync() => Assert.ThrowsAsync<DirectoryNotFoundException>(
            async () => await WriteAsync(Path.Combine(TestDirectory, GetTestFileName(), GetTestFileName()), "Text"));

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task NullContent_CreatesFileAsync()
        {
            string path = GetTestFilePath();
            await WriteAsync(path, null);
            Assert.Empty(await ReadAsync(path));
        }

        [Fact]
        public async Task EmptyStringContent_CreatesFileAsync()
        {
            string path = GetTestFilePath();
            await WriteAsync(path, string.Empty);
            Assert.Empty(await ReadAsync(path));
        }

        [Fact]
        public async Task InvalidParametersAsync()
        {
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await WriteAsync(string.Empty, "Text"));
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await ReadAsync(""));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(4096)]
        [InlineData(4097)]
        [InlineData(10000)]
        public async Task ValidWriteAsync(int size)
        {
            string path = GetTestFilePath();
            string toWrite = new string(Enumerable.Range(0, size).Select(i => (char)(i + 1)).ToArray());

            File.Create(path).Dispose();
            await WriteAsync(path, toWrite);
            Assert.Equal(toWrite, await ReadAsync(path));
        }

        [Fact]
        public virtual async Task OverwriteAsync()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string overwriteLines = new string('b', 100);
            await WriteAsync(path, lines);
            await WriteAsync(path, overwriteLines);
            Assert.Equal(overwriteLines, await ReadAsync(path));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsFileLockingEnabled))]
        public async Task OpenFile_ThrowsIOExceptionAsync()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);

            using (File.Create(path))
            {
                await Assert.ThrowsAsync<IOException>(async () => await WriteAsync(path, lines));
                await Assert.ThrowsAsync<IOException>(async () => await ReadAsync(path));
            }
        }

        [Fact]
        public Task Read_FileNotFoundAsync() =>
            Assert.ThrowsAsync<FileNotFoundException>(async () => await ReadAsync(GetTestFilePath()));

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/53021", TestPlatforms.Browser)]
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
                    await WriteAsync(path, "text");
                    Assert.Equal("text", await ReadAsync(path));
                }
                else
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await WriteAsync(path, "text"));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public virtual Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.WriteAllTextAsync(path, "", token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.WriteAllTextAsync(path, "", token));
        }

        #endregion
    }

    [ActiveIssue("https://github.com/dotnet/runtime/issues/34582", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_ReadWriteAllText_EncodedAsync : File_ReadWriteAllTextAsync
    {
        protected override Task WriteAsync(string path, string content) =>
            File.WriteAllTextAsync(path, content, new UTF8Encoding(false));

        protected override Task<string> ReadAsync(string path) =>
            File.ReadAllTextAsync(path, new UTF8Encoding(false));

        [Fact]
        public async Task NullEncodingAsync()
        {
            string path = GetTestFilePath();
            await Assert.ThrowsAsync<ArgumentNullException>("encoding", async () => await File.WriteAllTextAsync(path, "Text", null));
            await Assert.ThrowsAsync<ArgumentNullException>("encoding", async () => await File.ReadAllTextAsync(path, null));
        }

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.WriteAllTextAsync(path, "", Encoding.UTF8, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.WriteAllTextAsync(path, "", Encoding.UTF8, token));
        }
    }
}
