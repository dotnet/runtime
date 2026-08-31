// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class ZipFile_ZipArchiveEntry_Extract : ZipFileTestBase
    {
        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFileExtension(bool async)
        {
            ZipArchive archive = await CallZipFileOpen(async, zfile("normal.zip"), ZipArchiveMode.Read);

            string file = GetTestFilePath();
            ZipArchiveEntry e = archive.GetEntry("first.txt");

            await Assert.ThrowsAsync<ArgumentNullException>(() => CallExtractToFile(async, (ZipArchiveEntry)null, file));
            await Assert.ThrowsAsync<ArgumentNullException>(() => CallExtractToFile(async, e, null));

            //extract when there is nothing there
            await CallExtractToFile(async, e, file);

            using (Stream fs = File.Open(file, FileMode.Open))
            {
                Stream es = await OpenEntryStream(async, e);
                StreamsEqual(fs, es);
                await DisposeStream(async, es);
            }

            await Assert.ThrowsAsync<IOException>(() => CallExtractToFile(async, e, file, false));

            //truncate file
            using (Stream fs = File.Open(file, FileMode.Truncate)) { }

            //now use overwrite mode
            await CallExtractToFile(async, e, file, true);

            using (Stream fs = File.Open(file, FileMode.Open))
            {
                Stream es = await OpenEntryStream(async, e);
                StreamsEqual(fs, es);
                await DisposeStream(async, es);
            }

            await DisposeZipArchive(async, archive);
        }

        [Theory]
        [MemberData(nameof(Get_Booleans_Data))]
        public async Task ExtractToFile_OverwritePreservesOriginalFileOnExtractionFailure(bool async)
        {
            // Create a destination file with known content
            string destinationFile = GetTestFilePath();
            byte[] originalContent = "Original file content that should be preserved"u8.ToArray();
            File.WriteAllBytes(destinationFile, originalContent);

            // Verify the original file exists and has the correct content
            Assert.True(File.Exists(destinationFile));
            Assert.Equal(originalContent.Length, new FileInfo(destinationFile).Length);

            // Create an archive in memory with entry data that will be corrupted
            using MemoryStream archiveStream = new MemoryStream();
            using (ZipArchive createArchive = new ZipArchive(archiveStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                ZipArchiveEntry entry = createArchive.CreateEntry("test.txt");
                using (Stream entryStream = entry.Open())
                {
                    // Write some data
                    byte[] data = new byte[1024];
                    for (int i = 0; i < data.Length; i++)
                    {
                        data[i] = (byte)(i % 256);
                    }
                    entryStream.Write(data, 0, data.Length);
                }
            }

            // Corrupt the compressed data in the archive to trigger an exception during extraction
            // We'll modify bytes in the middle of the compressed data section
            byte[] archiveBytes = archiveStream.ToArray();
            // Find and corrupt the compressed data (after the local file header)
            // Local file headers are typically around byte 30-60, so corrupt bytes after that
            for (int i = 80; i < Math.Min(120, archiveBytes.Length); i++)
            {
                archiveBytes[i] = 0xFF;
            }

            // Create a new stream with the corrupted archive
            using MemoryStream corruptedStream = new MemoryStream(archiveBytes);
            using (ZipArchive readArchive = new ZipArchive(corruptedStream, ZipArchiveMode.Read, leaveOpen: true))
            {
                ZipArchiveEntry entry = readArchive.Entries[0];

                // Attempt to extract with overwrite=true, this should fail due to corrupted data
                // The corruption will cause InvalidDataException during decompression
                await Assert.ThrowsAsync<InvalidDataException>(async () =>
                {
                    await CallExtractToFile(async, entry, destinationFile, overwrite: true);
                });
            }

            // Verify the original file is preserved (not corrupted to 0 bytes)
            Assert.True(File.Exists(destinationFile));
            byte[] actualContent = File.ReadAllBytes(destinationFile);
            Assert.Equal(originalContent, actualContent);
        }
    }
}
