// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectoryAsync_Stream_Tests : TarTestsBase
    {
        [Fact]
        public async Task CreateFromDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.CreateFromDirectoryAsync("directory", archiveStream, includeBaseDirectory: false, cs.Token));
            }
        }

        [Fact]
        public async Task InvalidPath_Throws_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: null, destination: archiveStream, includeBaseDirectory: false));
                await Assert.ThrowsAsync<ArgumentException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: string.Empty, destination: archiveStream, includeBaseDirectory: false));
            }
        }

        [Fact]
        public async Task NullStream_Throws_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await Assert.ThrowsAsync<ArgumentNullException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path", destination: null, includeBaseDirectory: false));
            }
        }

        [Fact]
        public async Task UnwritableStream_Throws_Async()
        {
            await using (MemoryStream archiveStream = new MemoryStream())
            {
                await using (WrappedStream unwritable = new WrappedStream(archiveStream, canRead: true, canWrite: false, canSeek: true))
                {
                    await Assert.ThrowsAsync<ArgumentException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path", destination: unwritable, includeBaseDirectory: false));
                }
            }
        }

        [Fact]
        public async Task NonExistentDirectory_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                string dirPath = Path.Join(root.Path, "dir");

                await using (MemoryStream archive = new MemoryStream())
                {
                    await Assert.ThrowsAsync<DirectoryNotFoundException>(() => TarFile.CreateFromDirectoryAsync(sourceDirectoryName: dirPath, destination: archive, includeBaseDirectory: false));
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetTarEntryFormats))]
        public async Task CreateFromDirectoryAsync_WithFormat(TarEntryFormat format)
        {
            using (TempDirectory source = new TempDirectory())
            {
                string fileName = "file.txt";
                File.Create(Path.Join(source.Path, fileName)).Dispose();

                await using (MemoryStream archive = new MemoryStream())
                {
                    await TarFile.CreateFromDirectoryAsync(source.Path, archive, includeBaseDirectory: false, format);

                    archive.Position = 0;
                    await using (TarReader reader = new TarReader(archive))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.NotNull(entry);
                        Assert.Equal(format, entry.Format);
                        Assert.Equal(fileName, entry.Name);

                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidTarEntryFormats))]
        public async Task CreateFromDirectoryAsync_InvalidFormat_Throws(TarEntryFormat format)
        {
            using (TempDirectory source = new TempDirectory())
            {
                await using (MemoryStream archive = new MemoryStream())
                {
                    await Assert.ThrowsAsync<ArgumentOutOfRangeException>("format", () =>
                        TarFile.CreateFromDirectoryAsync(source.Path, archive, includeBaseDirectory: false, format));
                }
            }
        }
    }
}
