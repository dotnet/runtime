// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.IO.Compression.Tests;

[Collection(nameof(DisableParallelization))]
public class zip_LargeFiles : ZipFileTestBase
{
    private const ushort Zip64Version = 45;
    private const uint ZipLocalFileHeader_OffsetToVersionFromHeaderStart = 4;

    private static void FillWithHardToCompressData(byte[] buffer) => Random.Shared.NextBytes(buffer);

    private static FieldInfo GetOffsetOfLHField()
    {
        FieldInfo offsetOfLHField = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

        if (offsetOfLHField is null || offsetOfLHField.FieldType != typeof(long))
        {
            Assert.Fail("Cannot find the private field of _offsetOfLocalHeader in ZipArchiveEntry or the type is not long. Code may be changed after the test is written.");
        }

        return offsetOfLHField;
    }

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
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

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
    [OuterLoop("It requires 5~6 GB of free disk space and a lot of CPU time for compressed tests")]
    [InlineData(false)]
    [InlineData(true)]
    public static void CheckZIP64VersionIsSet_ForSmallFilesAfterBigFiles(bool isCompressed)
    {
        // issue #94899

        CompressionLevel compressLevel = isCompressed ? CompressionLevel.Optimal : CompressionLevel.NoCompression;
        byte[] smallBuffer = GC.AllocateUninitializedArray<byte>(1000);
        byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // ~1 GB
        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");
        string LargeFileName = "largefile";
        string SmallFileName = "smallfile";

        try
        {
            using FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite);

            // Create
            using (ZipArchive archive = new(fs, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry file = archive.CreateEntry(LargeFileName, compressLevel);

                using (Stream stream = file.Open())
                {
                    // Write 5GB of data
                    for (var i = 0; i < 5; i++)
                    {
                        if (isCompressed)
                        {
                            FillWithHardToCompressData(largeBuffer);
                        }

                        stream.Write(largeBuffer);
                    }
                }

                file = archive.CreateEntry(SmallFileName, compressLevel);

                using (Stream stream = file.Open())
                {
                    stream.Write(smallBuffer);
                }
            }

            fs.Position = 0;

            // Validate
            using (ZipArchive archive = new(fs, ZipArchiveMode.Read))
            {
                using var reader = new BinaryReader(fs);

                FieldInfo offsetOfLHField = GetOffsetOfLHField();

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

    [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
    [OuterLoop("It requires around 6 GB of free disk space")]
    public static void CompatZip64BeforeAndAfterFix()
    {
        // This test has the purpose of confirming that ZipArchive can still process a zip file that was created
        // with these APIs before the Zip64 bug was fixed: https://github.com/dotnet/runtime/pull/102053

        ushort buggyZip64Version = 20;
        byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // 1 GB
        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");

        try
        {
            using FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite);

           // Create
            using (ZipArchive archive = new(fs, ZipArchiveMode.Create, true))
            {
                ZipArchiveEntry file = archive.CreateEntry("file.txt", CompressionLevel.NoCompression);

                using (Stream stream = file.Open())
                {
                    // Write 5GB of data
                    for (var i = 0; i < 5; i++)
                    {
                        stream.Write(largeBuffer);
                    }
                }
            }
            Assert.True(fs.Length > int.MaxValue, $"File size is not big enough to test the Zip64 fix: {fs.Length} vs {int.MaxValue}");

            fs.Position = 0;

            // Open archive to modify the bit as it used to look before fix
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true))
            {
                FieldInfo offsetOfLHField = GetOffsetOfLHField();

                ZipArchiveEntry entry = zip.Entries.First();

                long currentPosition = fs.Position;

                // Confirm it's initially set to the correct value
                using (BinaryReader reader = new(fs, Encoding.UTF8, leaveOpen: true))
                {
                    fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                    ushort version = reader.ReadUInt16();
                    Assert.Equal(Zip64Version, version);
                }

                fs.Position = currentPosition;

                // Change it to the value that a version of .NET previous to the fix would have written
                using (BinaryWriter writer = new(fs, Encoding.UTF8, leaveOpen: true))
                {
                    fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                    writer.Write(buggyZip64Version);
                }
            }

            fs.Position = 0;

            // Open archive to verify that we can still read an archive with the buggy version
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                FieldInfo offsetOfLHField = GetOffsetOfLHField();
                ZipArchiveEntry entry = zip.Entries.First();

                // Confirm it's still set to the old buggy value (to prove we could still read a malformed file)
                using BinaryReader reader = new(fs, Encoding.UTF8);
                fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                ushort version = reader.ReadUInt16();
                Assert.Equal(buggyZip64Version, version);
            }
        }
        finally
        {
            File.Delete(zipArchivePath);
        }
    }
}
