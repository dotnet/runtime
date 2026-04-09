// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.IO.Compression.Tests;

[Collection(nameof(DisableParallelization))]
public class zip_LargeFiles : ZipFileTestBase
{
    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
    [OuterLoop("It requires almost 12 GB of free disk space")]
    public static void UnzipOver4GBZipFile()
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // 1 GB

        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");
        DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "over4GB"));

        try
        {
            for (byte i = 0; i < 6; i++)
            {
                File.WriteAllBytes(Path.Combine(tempDir.FullName, $"{i}.test"), buffer);
            }

            ZipFile.CreateFromDirectory(tempDir.FullName, zipArchivePath, CompressionLevel.NoCompression, includeBaseDirectory: false);

            using ZipArchive zipArchive = ZipFile.OpenRead(zipArchivePath);
            foreach (ZipArchiveEntry entry in zipArchive.Entries)
            {
                using Stream entryStream = entry.Open();

                Assert.True(entryStream.CanRead);
                Assert.Equal(buffer.Length, entryStream.Length);
            }
        }
        finally
        {
            File.Delete(zipArchivePath);

            tempDir.Delete(recursive: true);
        }
    }

    private static void FillWithHardToCompressData(byte[] buffer)
    {
        Random.Shared.NextBytes(buffer);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
    [OuterLoop("It requires 5~6 GB of free disk space and a lot of CPU time for compressed tests")]
    [MemberData(nameof(Get_Booleans_Data))]
    public static async Task CheckZIP64VersionIsSet_ForSmallFilesAfterBigFiles_Async(bool isCompressed)
    {
        // issue #94899

        CompressionLevel compressLevel = isCompressed ? CompressionLevel.Optimal : CompressionLevel.NoCompression;
        byte[] smallBuffer = GC.AllocateUninitializedArray<byte>(1000);
        byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // ~1 GB
        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");
        string LargeFileName = "largefile";
        string SmallFileName = "smallfile";
        uint ZipLocalFileHeader_OffsetToVersionFromHeaderStart = 4;
        ushort Zip64Version = 45;

        try
        {
            using FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite);

            // Create
            await using (ZipArchive archive = await ZipArchive.CreateAsync(fs, ZipArchiveMode.Create, true, entryNameEncoding: null))
            {
                ZipArchiveEntry file = archive.CreateEntry(LargeFileName, compressLevel);

                await using (Stream stream = await file.OpenAsync())
                {
                    // Write 5GB of data
                    for (var i = 0; i < 5; i++)
                    {
                        if (isCompressed)
                        {
                            FillWithHardToCompressData(largeBuffer);
                        }

                        await stream.WriteAsync(largeBuffer);
                    }
                }

                file = archive.CreateEntry(SmallFileName, compressLevel);

                await using (Stream stream = await file.OpenAsync())
                {
                    await stream.WriteAsync(smallBuffer);
                }
            }

            fs.Position = 0;

            // Validate
            await using (ZipArchive archive = await ZipArchive.CreateAsync(fs, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null))
            {
                using var reader = new BinaryReader(fs);

                FieldInfo offsetOfLHField = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

                if (offsetOfLHField is null || offsetOfLHField.FieldType != typeof(long))
                {
                    Assert.Fail("Cannot find the private field of _offsetOfLocalHeader in ZipArchiveEntry or the type is not long. Code may be changed after the test is written.");
                }

                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                    ushort versionNeeded = reader.ReadUInt16();

                    // Version is not ZIP64 for files with Local Header at >4GB offset.
                    Assert.Equal(Zip64Version, versionNeeded);
                }
            }
        }
        finally
        {
            File.Delete(zipArchivePath);
        }
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotMobile), nameof(PlatformDetection.Is64BitProcess))]
    [OuterLoop("It requires ~11 GB of free disk space")]
    public static async Task LargeFile_At_LargeOffset_ZIP64_HeaderPreservation()
    {
        // When a large file (>4GB) is placed at an offset >4GB, both the sizes
        // and offset need ZIP64 extra field entries in the central directory.
        // Previously, the offset handling would overwrite the sizes, causing corruption.

        byte[] buffer;
        try
        {
            buffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // 1 GB
        }
        catch (OutOfMemoryException)
        {
            throw new SkipTestException("Insufficient memory to run test");
        }

        string zipArchivePath = Path.Combine(Path.GetTempPath(), "largeFileAtLargeOffset.zip");

        try
        {
            using (FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite))
            await using (ZipArchive archive = await ZipArchive.CreateAsync(fs, ZipArchiveMode.Create, leaveOpen: true, entryNameEncoding: null))
            {
                // First, write small files totaling >4GB to push the offset past 4GB
                // Write 5 x 1GB files = 5GB of data to ensure offset > 4GB
                for (int i = 0; i < 5; i++)
                {
                    ZipArchiveEntry smallEntry = archive.CreateEntry($"prefix_{i}.bin", CompressionLevel.NoCompression);
                    await using (Stream stream = await smallEntry.OpenAsync())
                    {
                        await stream.WriteAsync(buffer);
                    }
                }

                // Now write a large file (>4GB) at an offset that is also >4GB
                // This triggers both AreSizesTooLarge and IsOffsetTooLarge conditions
                ZipArchiveEntry largeEntry = archive.CreateEntry("largefile.bin", CompressionLevel.NoCompression);
                await using (Stream stream = await largeEntry.OpenAsync())
                {
                    // Write 5GB of data (5 x 1GB chunks)
                    for (int i = 0; i < 5; i++)
                    {
                        await stream.WriteAsync(buffer);
                    }
                }
            }

            // Verify the archive can be read back without corruption
            await using (FileStream fs = File.OpenRead(zipArchivePath))
            await using (ZipArchive archive = await ZipArchive.CreateAsync(fs, ZipArchiveMode.Read, leaveOpen: false, entryNameEncoding: null))
            {
                Assert.Equal(6, archive.Entries.Count);

                // Verify each entry can be opened and has correct length
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    await using Stream entryStream = await entry.OpenAsync();

                    if (entry.Name.StartsWith("prefix_"))
                    {
                        Assert.Equal(buffer.Length, entry.Length);
                    }
                    else if (entry.Name == "largefile.bin")
                    {
                        Assert.Equal(5L * buffer.Length, entry.Length);
                    }

                    // Verify we can read from the stream (this would throw if header is corrupt)
                    byte[] readBuffer = new byte[1024];
                    int bytesRead = await entryStream.ReadAsync(readBuffer);
                    Assert.True(bytesRead > 0);
                }
            }
        }
        finally
        {
            File.Delete(zipArchivePath);
        }
    }
}
