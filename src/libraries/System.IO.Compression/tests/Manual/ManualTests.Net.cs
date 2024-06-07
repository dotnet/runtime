// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace System.IO.Compression.Tests;

public class ManualTests
{
    [Fact]
    public static void GenerateZip64File()
    {
        // This test has the purpose of generating a large zip file containing the Zip64 bug fix introduced in https://github.com/dotnet/runtime/pull/102053
        // The file can later be used in the VerifyZip64FixInNetFramework  test method in ManualTests.NetFramework.cs to confirm that it can read it correctly.

        const ushort Zip64Version = 45;
        const uint ZipLocalFileHeader_OffsetToVersionFromHeaderStart = 4;
        byte[] largeBuffer = GC.AllocateUninitializedArray<byte>(1_000_000_000); // 1 GB
        string zipArchivePath = Path.Combine(Path.GetTempPath(), "over4GB_WithoutBug.zip");

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

        // Create an archive using .NET with the fix, to later use it in .NET Framework
        using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true))
        {
            FieldInfo offsetOfLHField = typeof(ZipArchiveEntry).GetField("_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance);

            if (offsetOfLHField is null || offsetOfLHField.FieldType != typeof(long))
            {
                Assert.Fail("Cannot find the private field of _offsetOfLocalHeader in ZipArchiveEntry or the type is not long. Code may be changed after the test is written.");
            }

            ZipArchiveEntry entry = zip.Entries.First();

            long currentPosition = fs.Position;

            // Confirm it's set to the correct value
            using (BinaryReader reader = new(fs, Encoding.UTF8, leaveOpen: true))
            {
                fs.Position = (long)offsetOfLHField.GetValue(entry) + ZipLocalFileHeader_OffsetToVersionFromHeaderStart;
                ushort version = reader.ReadUInt16();
                Assert.Equal(Zip64Version, version);
            }
        }

        Console.WriteLine($"Zip file location: {zipArchivePath}");
    }
}