// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarFile_CreateFromDirectory_Stream_Tests : TarTestsBase
    {
        [Fact]
        public async Task CreateFromDirectoryAsync_Cancel()
        {
            CancellationTokenSource cs = new CancellationTokenSource();
            cs.Cancel();

            using MemoryStream archiveStream = new MemoryStream();
            await Assert.ThrowsAsync<TaskCanceledException>(() => TarFile.CreateFromDirectoryAsync("directory", archiveStream, includeBaseDirectory: false, cs.Token));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task InvalidPath_Throws(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateFromDirectory(sourceDirectoryName: null, destination: archive, includeBaseDirectory: false, async));
            await Assert.ThrowsAsync<ArgumentException>(() => CreateFromDirectory(sourceDirectoryName: string.Empty, destination: archive, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NullStream_Throws(bool async)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => CreateFromDirectory(sourceDirectoryName: "path", destination: null, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task UnwritableStream_Throws(bool async)
        {
            using MemoryStream archive = new MemoryStream();
            using WrappedStream unwritable = new WrappedStream(archive, canRead: true, canWrite: false, canSeek: true);
            await Assert.ThrowsAsync<ArgumentException>(() => CreateFromDirectory(sourceDirectoryName: "path", destination: unwritable, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task NonExistentDirectory_Throws(bool async)
        {
            using TempDirectory root = new TempDirectory();
            string dirPath = Path.Join(root.Path, "dir");

            using MemoryStream archive = new MemoryStream();
            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => CreateFromDirectory(sourceDirectoryName: dirPath, destination: archive, includeBaseDirectory: false, async));
        }

        [Theory]
        [MemberData(nameof(GetTarEntryFormats))]
        public async Task CreateFromDirectory_WithFormat(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                string fileName = "file.txt";
                File.Create(Path.Join(source.Path, fileName)).Dispose();

                using MemoryStream archive = new MemoryStream();
                await CreateFromDirectory(source.Path, archive, includeBaseDirectory: false, format, async);

                archive.Position = 0;
                using TarReader reader = new TarReader(archive);

                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                Assert.Equal(format, entry.Format);
                Assert.Equal(fileName, entry.Name);

                Assert.Null(reader.GetNextEntry());
            }
        }

        [Theory]
        [MemberData(nameof(GetInvalidTarEntryFormats))]
        public async Task CreateFromDirectory_InvalidFormat_Throws(TarEntryFormat format)
        {
            foreach (bool async in Booleans)
            {
                using TempDirectory source = new TempDirectory();
                using MemoryStream archive = new MemoryStream();

                await Assert.ThrowsAsync<ArgumentOutOfRangeException>("format", () =>
                    CreateFromDirectory(source.Path, archive, includeBaseDirectory: false, format, async));
            }
        }

        [ConditionalTheory(typeof(MountHelper), nameof(MountHelper.CanCreateHardLinks))]
        [InlineData(true)]
        [InlineData(false)]
        public async Task CreateFromDirectory_UsesWriterOptions(bool toggle)
        {
            foreach (bool async in Booleans)
            {
                bool preserveLinks = toggle;

                using TempDirectory source = CreateSourceDirectoryForCreateFromDirectory_UsesWriterOptions();

                TarWriterOptions options = new TarWriterOptions()
                {
                    HardLinkMode = preserveLinks ? TarHardLinkMode.PreserveLink : TarHardLinkMode.CopyContents
                };

                using MemoryStream archive = new MemoryStream();
                await CreateFromDirectory(source.Path, archive, includeBaseDirectory: false, options, async);

                VerifyCreateFromDirectory_UsesWriterOptions(archive, preserveLinks);
            }
        }
    }
}
