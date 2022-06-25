// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.IO;
using Xunit;
using System.Threading.Tasks;

namespace System.Formats.Tar.Tests
{
    public class CompressedTar_Async_Tests : TarTestsBase
    {
        [Fact]
        public async Task TarGz_TarWriter_TarReader_Async()
        {
            using TempDirectory root = new TempDirectory();

            string archivePath = Path.Join(root.Path, "compressed.tar.gz");

            string fileName = "file.txt";
            string filePath = Path.Join(root.Path, fileName);
            File.Create(filePath).Dispose();

            // Create tar.gz archive
            FileStreamOptions createOptions = new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Options = FileOptions.Asynchronous
            };
            FileStream streamToCompress = File.Open(archivePath, createOptions);
            await using (streamToCompress)
            {
                GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress);
                await using (compressorStream)
                {
                    TarWriter writer = new TarWriter(compressorStream);
                    await using (writer)
                    {
                        await writer.WriteEntryAsync(fileName: filePath, entryName: fileName);
                    }
                }
            }
            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            // Verify tar.gz archive contents
            FileStreamOptions readOptions = new()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Options = FileOptions.Asynchronous
            };
            FileStream streamToDecompress = File.Open(archivePath, readOptions);
            await using (streamToDecompress)
            {
                GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress);
                await using (decompressorStream)
                {
                    TarReader reader = new TarReader(decompressorStream);
                    await using (reader)
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        Assert.Equal(TarEntryFormat.Pax, entry.Format);
                        Assert.Equal(fileName, entry.Name);
                        Assert.Null(await reader.GetNextEntryAsync());
                    }
                }
            }
        }

        [Fact]
        public async Task TarGz_TarFile_CreateFromDir_ExtractToDir_Async()
        {
            using TempDirectory root = new TempDirectory();

            string archivePath = Path.Join(root.Path, "compressed.tar.gz");

            string sourceDirectory = Path.Join(root.Path, "source");
            Directory.CreateDirectory(sourceDirectory);

            string destinationDirectory = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destinationDirectory);

            string fileName = "file.txt";
            string filePath = Path.Join(sourceDirectory, fileName);
            File.Create(filePath).Dispose();

            FileStreamOptions createOptions = new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Options = FileOptions.Asynchronous
            };
            FileStream streamToCompress = File.Open(archivePath, createOptions);
            await using (streamToCompress)
            {
                GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress);
                await using (compressorStream)
                {
                    await TarFile.CreateFromDirectoryAsync(sourceDirectory, compressorStream, includeBaseDirectory: false);
                }
            }
            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            FileStreamOptions readOptions = new()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Options = FileOptions.Asynchronous
            };
            FileStream streamToDecompress = File.Open(archivePath, readOptions);
            await using (streamToDecompress)
            {
                GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress);
                await using (decompressorStream)
                {
                    await TarFile.ExtractToDirectoryAsync(decompressorStream, destinationDirectory, overwriteFiles: true);
                    Assert.True(File.Exists(filePath));
                }
            }
        }
    }
}
