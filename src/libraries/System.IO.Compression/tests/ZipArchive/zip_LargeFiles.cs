// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.IO.Compression.Tests
{
    [Collection(nameof(DisableParallelization))]
    public class zip_LargeFiles : ZipFileTestBase
    {
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

        private static void FillWithHardToCompressData(byte[] buffer)
        {
            Random.Shared.NextBytes(buffer);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
        [OuterLoop("It requires 5~6 GB of free disk space and a lot of CPU time for compressed tests")]
        [InlineData(false)]
        [InlineData(true)]
        public static void CheckZIP64VersionIsSet_ForSmallFilesAfterBigFiles(bool isCompressed)
        {
            // issue #94899

            byte[] smallBuffer = GC.AllocateUninitializedArray<byte>(1000);
            byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // ~1 GB

            string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");

            try
            {
                using FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite);
                const string LargeFileName = "largefile";
                const string SmallFileName = "smallfile";
                const uint ZipLocalFileHeader_OffsetToVersionFromHeaderStart = 4;
                const ushort Zip64Version = 45;

                {
                    // Create

                    var compressLevel = isCompressed ? CompressionLevel.Optimal : CompressionLevel.NoCompression;

                    using var archive = new ZipArchive(fs, ZipArchiveMode.Create, true);
                    ZipArchiveEntry file = archive.CreateEntry(LargeFileName, compressLevel);

                    using (Stream stream = file.Open())
                    {
                        // Write 5GB of data

                        const int HOW_MANY_GB_TO_WRITE = 5;

                        for (var i = 0; i < HOW_MANY_GB_TO_WRITE; i++)
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

                {
                    // Validate

                    using var reader = new BinaryReader(fs);
                    using var archive = new ZipArchive(fs, ZipArchiveMode.Read);
                    FieldInfo offsetOfLHField = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

                    if (offsetOfLHField is null || offsetOfLHField.FieldType != typeof(long))
                    {
                        Assert.Fail("Cannot find the private field of _offsetOfLocalHeader in ZipArchiveEntry or the type is not long. Code may be changed after the test is written.");
                    }

                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                        ushort versionNeeded = reader.ReadUInt16();

                        Assert.True(versionNeeded == Zip64Version, "Version is not ZIP64 for files with Local Header at >4GB offset.");
                    }
                }
            }
            finally
            {
                File.Delete(zipArchivePath);
            }
        }
    }
}
