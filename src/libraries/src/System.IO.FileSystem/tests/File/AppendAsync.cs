// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class File_AppendAllTextAsync : File_ReadWriteAllTextAsync
    {
        protected override Task WriteAsync(string path, string content) => File.AppendAllTextAsync(path, content);

        [Fact]
        public override async Task OverwriteAsync()
        {
            string path = GetTestFilePath();
            string lines = new string('c', 200);
            string appendLines = new string('b', 100);
            await WriteAsync(path, lines);
            await WriteAsync(path, appendLines);
            Assert.Equal(lines + appendLines, await ReadAsync(path));
        }

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.AppendAllTextAsync(path, "", token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(async () => await File.AppendAllTextAsync(path, "", token));
        }
    }

    public class File_AppendAllTextAsync_Encoded : File_AppendAllTextAsync
    {
        protected override Task WriteAsync(string path, string content) =>
            File.AppendAllTextAsync(path, content, new UTF8Encoding(false));

        [Fact]
        public Task NullEncodingAsync() => Assert.ThrowsAsync<ArgumentNullException>(
            "encoding",
            async () => await File.AppendAllTextAsync(GetTestFilePath(), "Text", null));

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.AppendAllTextAsync(path, "", Encoding.UTF8, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.AppendAllTextAsync(path, "", Encoding.UTF8, token));
        }
    }

    public class File_AppendAllLinesAsync : File_ReadWriteAllLines_EnumerableAsync
    {
        protected override Task WriteAsync(string path, string[] content) => File.AppendAllLinesAsync(path, content);

        [Fact]
        public override async Task OverwriteAsync()
        {
            string path = GetTestFilePath();
            string[] lines = new string[] { new string('c', 200) };
            string[] appendLines = new string[] { new string('b', 100) };
            await WriteAsync(path, lines);
            await WriteAsync(path, appendLines);
            Assert.Equal(new string[] { lines[0], appendLines[0] }, await ReadAsync(path));
        }

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.AppendAllLinesAsync(path, new[] { "" }, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.AppendAllLinesAsync(path, new[] { "" }, token));
        }
    }

    public class File_AppendAllLinesAsync_Encoded : File_AppendAllLinesAsync
    {
        protected override Task WriteAsync(string path, string[] content) =>
            File.AppendAllLinesAsync(path, content, new UTF8Encoding(false));

        [Fact]
        public Task NullEncodingAsync() => Assert.ThrowsAsync<ArgumentNullException>(
            "encoding",
            async () => await File.AppendAllLinesAsync(GetTestFilePath(), new string[] { "Text" }, null));

        [Fact]
        public override Task TaskAlreadyCanceledAsync()
        {
            string path = GetTestFilePath();
            CancellationTokenSource source = new CancellationTokenSource();
            CancellationToken token = source.Token;
            source.Cancel();
            Assert.True(File.AppendAllLinesAsync(path, new[] { "" }, Encoding.UTF8, token).IsCanceled);
            return Assert.ThrowsAsync<TaskCanceledException>(
                async () => await File.AppendAllLinesAsync(path, new[] { "" }, Encoding.UTF8, token));
        }
    }
}
