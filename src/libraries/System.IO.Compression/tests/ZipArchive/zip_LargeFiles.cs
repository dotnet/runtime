// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_LargeFiles : ZipFileTestBase
    {
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsSpeedOptimized))] // don't run it on slower runtimes
        [OuterLoop("It takes more than 10 minutes")]
        public static void UnzipOver4GBZipFile()
        {
            Random random = new (12345); // use const seed for reproducible results
            byte[] buffer = new byte[1_000_000]; // 1 MB
            Dictionary<string, byte[]> entiresContent = new();

            string zipArchivePath = Path.Combine("ZipTestData", "over4GB.zip");
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine("ZipTestData", "over4GB"));

            try
            {
                for (int i = 0; i < 4_500; i++)
                {
                    random.NextBytes(buffer);

                    string entryName = $"{i}.test";
                    File.WriteAllBytes(Path.Combine(tempDir.FullName, entryName), buffer);
                    entiresContent.Add(entryName, buffer.ToArray());
                }

                ZipFile.CreateFromDirectory(tempDir.FullName, zipArchivePath);

                using ZipArchive zipArchive = ZipFile.OpenRead(zipArchivePath);
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    using Stream entryStream = entry.Open();
                    Assert.True(entryStream.CanRead);
                    entryStream.ReadExactly(buffer);
                    Assert.Equal(entiresContent[entry.Name], buffer);
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
