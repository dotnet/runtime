// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class CompressedTar_Tests : TarTestsBase
    {
        [Fact]
        public void TarGz_TarWriter_TarReader()
        {
            using TempDirectory root = new TempDirectory();

            string archivePath = Path.Join(root.Path, "compressed.tar.gz");

            string fileName = "file.txt";
            string filePath = Path.Join(root.Path, fileName);
            File.Create(filePath).Dispose();

            // Create tar.gz archive
            using (FileStream streamToCompress = File.Create(archivePath))
            {
                using GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress);
                using TarWriter writer = new TarWriter(compressorStream);
                writer.WriteEntry(fileName: filePath, entryName: fileName);
            }
            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            // Verify tar.gz archive contents
            using (FileStream streamToDecompress = File.OpenRead(archivePath))
            {
                using GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress);
                using TarReader reader = new TarReader(decompressorStream);
                TarEntry entry = reader.GetNextEntry();
                Assert.Equal(TarEntryFormat.Pax, reader.Format);
                Assert.Equal(fileName, entry.Name);
                Assert.Null(reader.GetNextEntry());
            }
        }

        [Fact]
        public void TarGz_TarFile_CreateFromDir_ExtractToDir()
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

            using (FileStream streamToCompress = File.Create(archivePath))
            {
                using GZipStream compressorStream = new GZipStream(streamToCompress, CompressionMode.Compress);
                TarFile.CreateFromDirectory(sourceDirectory, compressorStream, includeBaseDirectory: false);
            }
            FileInfo fileInfo = new FileInfo(archivePath);
            Assert.True(fileInfo.Exists);
            Assert.True(fileInfo.Length > 0);

            using (FileStream streamToDecompress = File.OpenRead(archivePath))
            {
                using GZipStream decompressorStream = new GZipStream(streamToDecompress, CompressionMode.Decompress);
                TarFile.ExtractToDirectory(decompressorStream, destinationDirectory, overwriteFiles: true);
                Assert.True(File.Exists(filePath));
            }
        }
    }
}
