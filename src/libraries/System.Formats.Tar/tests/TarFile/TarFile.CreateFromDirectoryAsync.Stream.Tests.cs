// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectoryAsync_Stream_Tests : TarTestsBase
    {
        [Fact]
        public async Task InvalidPath_Throws_Async()
        {
            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: null,destination: archive, includeBaseDirectory: false));
            await Assert.ThrowsAsync<ArgumentException>(async () => await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: string.Empty,destination: archive, includeBaseDirectory: false));
        }

        [Fact]
        public async Task NullStream_Throws_Async()
        {
            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path",destination: null, includeBaseDirectory: false));
        }

        [Fact]
        public async Task UnwritableStream_Throws_Async()
        {
            using MemoryStream archive = new MemoryStream();
            using WrappedStream unwritable = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: true);
            await Assert.ThrowsAsync<IOException>(async () => await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: "path",destination: unwritable, includeBaseDirectory: false));
        }

        [Fact]
        public async Task NonExistentDirectory_Throws_Async()
        {
            using TempDirectory root = new TempDirectory();
            string dirPath = Path.Join(root.Path, "dir");

            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<DirectoryNotFoundException>(async () => await TarFile.CreateFromDirectoryAsync(sourceDirectoryName: dirPath, destination: archive, includeBaseDirectory: false));
        }
    }
}
