// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_ExtractToFileAsync_Tests : TarTestsBase
    {
        [Fact]
        public async Task ExtractEntriesWithSlashDotPrefix_Async()
        {
            using (TempDirectory root = new TempDirectory())
            await using (MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry"))
            await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                string rootPath = Path.TrimEndingDirectorySeparator(root.Path);
                TarEntry entry;
                while ((entry = await reader.GetNextEntryAsync()) != null)
                {
                    Assert.NotNull(entry);
                    Assert.StartsWith("./", entry.Name);
                    // Normalize the path (remove redundant segments), remove trailing separators
                    // this is so the first entry can be skipped if it's the same as the root directory
                    string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(rootPath, entry.Name)));
                    if (entryPath != rootPath)
                    {
                        await entry.ExtractToFileAsync(entryPath, overwrite: true);
                        Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
                    }
                }
            }
        }
    }
}
