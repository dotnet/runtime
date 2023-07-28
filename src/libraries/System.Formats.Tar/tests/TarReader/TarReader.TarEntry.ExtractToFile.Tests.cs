// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [Fact]
        public void EntriesWithSlashDotPrefix()
        {
            using TempDirectory root = new TempDirectory();

            using MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry");
            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                string rootPath = Path.TrimEndingDirectorySeparator(root.Path);
                TarEntry entry;
                while ((entry = reader.GetNextEntry()) != null)
                {
                    Assert.NotNull(entry);
                    Assert.StartsWith("./", entry.Name);
                    // Normalize the path (remove redundant segments), remove trailing separators
                    // this is so the first entry can be skipped if it's the same as the root directory
                    string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(rootPath, entry.Name)));
                    if (entryPath != rootPath)
                    {
                        entry.ExtractToFile(entryPath, overwrite: true);
                        Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
                    }
                }
            }
        }
    }
}
