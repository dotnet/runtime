// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class CompressedTar_Tests : TarTestsBase
    {
        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task TarGz_TarWriter_TarReader(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string archivePath = Path.Join(root.Path, "compressed.tar.gz");

            string fileName = "file.txt";
            string filePath = Path.Join(root.Path, fileName);
            File.Create(filePath).Dispose();

            FileStreamOptions createOptions = new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Options = async ? FileOptions.Asynchronous : FileOptions.None
            };

            using (FileStream streamToCompress = File.Open(archivePath, createOptions))
            using (GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress))
            {
                TarWriter writer = CreateTarWriter(compressorStream);
                try
                {
                    await WriteEntry(writer, filePath, fileName, async);
                }
                finally
                {
                    await DisposeTarWriter(writer, async);
                }
            }

            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            FileStreamOptions readOptions = new()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Options = async ? FileOptions.Asynchronous : FileOptions.None
            };

            using (FileStream streamToDecompress = File.Open(archivePath, readOptions))
            using (GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress))
            {
                TarReader reader = CreateTarReader(decompressorStream);
                try
                {
                    TarEntry entry = await GetNextEntry(reader, async: async);
                    Assert.NotNull(entry);
                    Assert.Equal(TarEntryFormat.Pax, entry.Format);
                    Assert.Equal(fileName, entry.Name);
                    Assert.Null(await GetNextEntry(reader, async: async));
                }
                finally
                {
                    await DisposeTarReader(reader, async);
                }
            }
        }

        [Theory]
        [MemberData(nameof(GetBooleanData))]
        public async Task TarGz_TarFile_CreateFromDir_ExtractToDir(bool async)
        {
            using TempDirectory root = new TempDirectory();

            string archivePath = Path.Join(root.Path, "compressed.tar.gz");

            string sourceDirectory = Path.Join(root.Path, "source");
            Directory.CreateDirectory(sourceDirectory);

            string destinationDirectory = Path.Join(root.Path, "destination");
            Directory.CreateDirectory(destinationDirectory);

            string fileName = "file.txt";
            string filePath = Path.Join(sourceDirectory, fileName);
            string extractedFilePath = Path.Join(destinationDirectory, fileName);
            File.Create(filePath).Dispose();

            FileStreamOptions createOptions = new()
            {
                Mode = FileMode.CreateNew,
                Access = FileAccess.Write,
                Options = async ? FileOptions.Asynchronous : FileOptions.None
            };

            using (FileStream streamToCompress = File.Open(archivePath, createOptions))
            using (GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress))
            {
                await CreateFromDirectory(sourceDirectory, compressorStream, includeBaseDirectory: false, async);
            }

            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            FileStreamOptions readOptions = new()
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Options = async ? FileOptions.Asynchronous : FileOptions.None
            };

            using (FileStream streamToDecompress = File.Open(archivePath, readOptions))
            using (GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress))
            {
                await ExtractToDirectory(decompressorStream, destinationDirectory, overwriteFiles: true, async);
            }

            Assert.True(File.Exists(extractedFilePath));
        }
    }
}
