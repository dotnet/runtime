// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_ReadWriteAllTextAsync : FileSystemTest
    {
        protected virtual bool IsAppend { get; }

        #region Utilities

        protected virtual Task WriteAsync(string path, string content) => File.WriteAllTextAsync(path, content);

        protected virtual Task WriteAsync(string path, string content, Encoding encoding) => File.WriteAllTextAsync(path, content, encoding);

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

        [Theory]
        [InlineData(200, 100)]
        [InlineData(50_000, 40_000)] // tests a different code path than the line above
        public async Task AppendOrOverwriteAsync(int linesSizeLength, int overwriteLinesLength)
        {
            string path = GetTestFilePath();
            string lines = new string('c', linesSizeLength);
            string overwriteLines = new string('b', overwriteLinesLength);

            await WriteAsync(path, lines);
            await WriteAsync(path, overwriteLines); ;

            if (IsAppend)
            {
                Assert.Equal(lines + overwriteLines, await ReadAsync(path));
            }
            else
            {
                Assert.DoesNotContain("Append", GetType().Name); // ensure that all "Append" types override this property

                Assert.Equal(overwriteLines, await ReadAsync(path));
            }
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
        /// file is allowed. On Windows, modifying a file that is ReadOnly will always fail.
        /// </summary>
        [Fact]
        public async Task WriteToReadOnlyFileAsync()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                if (PlatformDetection.IsNotWindows && PlatformDetection.IsPrivilegedProcess)
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

        [Theory]
        [MemberData(nameof(File_ReadWriteAllText.OutputIsTheSameAsForStreamWriter_Args), MemberType = typeof(File_ReadWriteAllText))]
        public async Task OutputIsTheSameAsForStreamWriterAsync(string content, Encoding encoding)
        {
            string filePath = GetTestFilePath();
            await WriteAsync(filePath, content, encoding); // it uses System.File.IO APIs

            string swPath = GetTestFilePath();
            using (StreamWriter sw = new StreamWriter(swPath, IsAppend, encoding))
            {
                await sw.WriteAsync(content);
            }

            Assert.Equal(await File.ReadAllTextAsync(swPath, encoding), await File.ReadAllTextAsync(filePath, encoding));
            Assert.Equal(await File.ReadAllBytesAsync(swPath), await File.ReadAllBytesAsync(filePath)); // ensure Preamble was stored
        }

        [Theory]
        [MemberData(nameof(File_ReadWriteAllText.OutputIsTheSameAsForStreamWriter_Args), MemberType = typeof(File_ReadWriteAllText))]
        public async Task OutputIsTheSameAsForStreamWriter_OverwriteAsync(string content, Encoding encoding)
        {
            string filePath = GetTestFilePath();
            string swPath = GetTestFilePath();

            for (int i = 0; i < 2; i++)
            {
                await WriteAsync(filePath, content, encoding); // it uses System.File.IO APIs

                using (StreamWriter sw = new StreamWriter(swPath, IsAppend, encoding))
                {
                    await sw.WriteAsync(content);
                }
            }

            Assert.Equal(await File.ReadAllTextAsync(swPath, encoding), await File.ReadAllTextAsync(filePath, encoding));
            Assert.Equal(await File.ReadAllBytesAsync(swPath), await File.ReadAllBytesAsync(filePath)); // ensure Preamble was stored once
        }

        #endregion
    }

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
