// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Compression;
using System.Net.Mime;
using System.Reflection;
using Xunit;

namespace System.IO.Packaging.Tests;

public partial class LargeFilesTests
{
    private static void FillWithHardToCompressData(byte[] buffer)
    {
        Random.Shared.NextBytes(buffer);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
    [InlineData(false)]
    [InlineData(true)]
    [OuterLoop("It requires 5~6 GB of free disk space and a lot of CPU time for compressed tests")]
    public static void CheckZIP64VersionIsSet_ForSmallFilesAfterBigFiles(bool isCompressed)
    {
        // issue #94899

        CompressionOption compressionOption = isCompressed ? CompressionOption.Normal : CompressionOption.NotCompressed;
        byte[] smallBuffer = GC.AllocateUninitializedArray<byte>(1000);
        byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // ~1 GB
        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB.zip");
        Uri largePartUri = PackUriHelper.CreatePartUri(new Uri("large.bin", UriKind.Relative));
        Uri smallPartUri = PackUriHelper.CreatePartUri(new Uri("small.bin", UriKind.Relative));
        uint ZipLocalFileHeader_OffsetToVersionFromHeaderStart = 4;
        ushort Zip64Version = 45;

        try
        {
            using FileStream fs = File.Open(zipArchivePath, FileMode.Create, FileAccess.ReadWrite);

            // Create
            using (Package package = Package.Open(fs, FileMode.Create, FileAccess.Write))
            {
                PackagePart partLarge = package.CreatePart(largePartUri, MediaTypeNames.Application.Octet, compressionOption);

                using (Stream streamLarge = partLarge.GetStream())
                {
                    // Write 5GB of data

                    for (var i = 0; i < 5; i++)
                    {
                        if (isCompressed)
                        {
                            FillWithHardToCompressData(largeBuffer);
                        }

                        streamLarge.Write(largeBuffer);
                    }
                }

                PackagePart partSmall = package.CreatePart(smallPartUri, MediaTypeNames.Application.Octet, compressionOption);

                using (Stream streamSmall = partSmall.GetStream())
                {
                    streamSmall.Write(smallBuffer);
                }
            }


            fs.Position = 0;

            // Validate
            using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
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
}
