// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_LargeFiles : ZipFileTestBase
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized), nameof(PlatformDetection.Is64BitProcess))] // don't run it on slower runtimes
        [OuterLoop("It requires almost 12 GB of free disk space")]
        public static void UnzipOver4GBZipFile()
        {
            byte[] buffer = new byte[1_000_000_000]; // 1 GB

            string zipArchivePath = Path.Combine("ZipTestData", "over4GB.zip");
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine("ZipTestData", "over4GB"));

            try
            {
                for (byte i = 0; i < 6; i++)
                {
                    buffer.AsSpan().Fill(i);

                    string entryName = $"{i}.test";
                    File.WriteAllBytes(Path.Combine(tempDir.FullName, entryName), buffer);
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
                if (File.Exists(zipArchivePath))
                {
                    File.Delete(zipArchivePath);
                }

                tempDir.Delete(recursive: true);
            }
        }
    }
}
