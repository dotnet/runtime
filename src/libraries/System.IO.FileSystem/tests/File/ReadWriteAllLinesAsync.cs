// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/34583", TestPlatforms.Windows, TargetFrameworkMonikers.Netcoreapp, TestRuntimes.Mono)]
    public class File_ReadWriteAllLines_EnumerableAsync : FileSystemTest
    {
        #region Utilities

        protected virtual Task WriteAsync(string path, string[] content) =>
            File.WriteAllLinesAsync(path, content);

        protected virtual Task<string[]> ReadAsync(string path) => File.ReadAllLinesAsync(path);

        #endregion

        #region UniversalTests

        [Fact]
        public async Task InvalidPathAsync()
        {
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await WriteAsync(null, new string[] { "Text" }));
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await WriteAsync(string.Empty, new string[] { "Text" }));
            await Assert.ThrowsAsync<ArgumentNullException>("path", async () => await ReadAsync(null));
            await Assert.ThrowsAsync<ArgumentException>("path", async () => await ReadAsync(string.Empty));
        }

        [Fact]
        public async Task NullLinesAsync()
        {
            string path = GetTestFilePath();
            await Assert.ThrowsAsync<ArgumentNullException>("contents", async () => await WriteAsync(path, null));

            await WriteAsync(path, new string[] { null });
            Assert.Equal(new string[] { "" }, await ReadAsync(path));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public async Task EmptyStringCreatesFileAsync()
        {
            string path = GetTestFilePath();
            await WriteAsync(path, new string[] { });
            Assert.True(File.Exists(path));
            Assert.Empty(await ReadAsync(path));
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        [InlineData(0)]
        [InlineData(100)]
        public async Task ValidWriteAsync(int size)
        {
            string path = GetTestFilePath();
            string[] lines = { new string('c', size) };
            File.Create(path).Dispose();
            await WriteAsync(path, lines);
            Assert.Equal(lines, await ReadAsync(path));
        }

        [Fact]
        public virtual async Task OverwriteAsync()
        {
            string path = GetTestFilePath();
            string[] lines = { new string('c', 200) };
            string[] overwriteLines = { new string('b', 100) };
            await WriteAsync(path, lines);
            await WriteAsync(path, overwriteLines);
            Assert.Equal(overwriteLines, await ReadAsync(path));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/40065", TestPlatforms.Browser)]
        public async Task OpenFile_ThrowsIOExceptionAsync()
        {
            string path = GetTestFilePath();
            string[] lines = { new string('c', 200) };

            using (File.Create(path))
            {
                await Assert.ThrowsAsync<IOException>(async () => await WriteAsync(path, lines));
                await Assert.ThrowsAsync<IOException>(async () => await ReadAsync(path));
            }
        }

        [Fact]
        public Task Read_FileNotFound() =>
            Assert.ThrowsAsync<FileNotFoundException>(async () => await ReadAsync(GetTestFilePath()));

        /// <summary>
        /// On Unix, modifying a file that is ReadOnly will fail under normal permissions.
        /// If the test is being run under the superuser, however, modification of a ReadOnly
        /// file is allowed.
        /// </summary>
        [Fact]
        public async Task WriteToReadOnlyFile()
        {
            string path = GetTestFilePath();
            File.Create(path).Dispose();
            File.SetAttributes(path, FileAttributes.ReadOnly);
            try
            {
                // Operation succeeds when being run by the Unix superuser
                if (PlatformDetection.IsSuperUser)
                {
                    await WriteAsync(path, new string[] { "text" });
                    Assert.Equal(new string[] { "text" }, await ReadAsync(path));
                }
                else
                    await Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await WriteAsync(path, new[] { "text" }));
            }
            finally
            {
                File.SetAttributes(path, FileAttributes.Normal);
            }
        }

        [Fact]
        public async Task DisposingEnumeratorClosesFileAsync()
        {
            string path = GetTestFilePath();
            await WriteAsync(path, new[] { "line1", "line2", "line3" });

            IEnumerable<string> readLines = File.ReadLines(path);
            using (IEnumerator<string> e1 = readLines.GetEnumerator())
            using (IEnumerator<string> e2 = readLines.GetEnumerator())
            {
                Assert.Same(readLines, e1);
                Assert.NotSame(e1, e2);
            }

            // File should be closed deterministically; this shouldn't throw.
            File.OpenWrite(path).Dispose();
        }


        [Fact]
        public virtual Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.WriteAllLinesAsync(path, new[] { "" }, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.WriteAllLinesAsync(path, new[] { "" }, token));
        }

        #endregion
    }

    public class File_ReadWriteAllLines_Enumerable_EncodedAsync : File_ReadWriteAllLines_EnumerableAsync
    {
        protected override Task WriteAsync(string path, string[] content) =>
            File.WriteAllLinesAsync(path, (IEnumerable<string>)content, new UTF8Encoding(false));

        protected override Task<string[]> ReadAsync(string path) =>
            File.ReadAllLinesAsync(path, new UTF8Encoding(false));

        [Fact]
        public async Task NullEncodingAsync()
        {
            string path = GetTestFilePath();
            await Assert.ThrowsAsync<ArgumentNullException>("encoding", async () => await File.WriteAllLinesAsync(path, new string[] { "Text" }, null));
            await Assert.ThrowsAsync<ArgumentNullException>("encoding", async () => await File.ReadAllLinesAsync(path, null));
        }

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.WriteAllLinesAsync(path, new[] { "" }, Encoding.UTF8, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.WriteAllLinesAsync(path, new[] { "" }, Encoding.UTF8, token));
        }
    }
}
