// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.IO.Compression.Tests
{
    public class zip_LargeFiles : ZipFileTestBase
    {
        [Fact]
        public static void ZipUnzip4GBFile()
        {
            Random random = new (12345); // use const seed for reproducible results
            byte[] buffer = new byte[1_000_000]; // 1 MB

            string filePath = Path.Combine("ZipTestData", "large.zip");
            DirectoryInfo tempDir = Directory.CreateDirectory(Path.Combine("ZipTestData", "large"));

            try
            {
                for (int i = 0; i < 4_500; i++)
                {
                    random.NextBytes(buffer);
                    File.WriteAllBytes(Path.Combine(tempDir.FullName, $"{i}.test"), buffer);
                }

                ZipFile.CreateFromDirectory(tempDir.FullName, filePath);

                using ZipArchive zipArchive = ZipFile.OpenRead(filePath);
                foreach (ZipArchiveEntry entry in zipArchive.Entries)
                {
                    using Stream entryStream = entry.Open();
                    Assert.True(entryStream.CanRead);
                }
            }
            finally
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                tempDir.Delete(recursive: true);
            }
        }
    }
}
