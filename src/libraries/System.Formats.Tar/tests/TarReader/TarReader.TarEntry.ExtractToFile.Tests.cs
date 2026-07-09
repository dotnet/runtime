// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarReader_TarEntry_ExtractToFile_Tests : TarTestsBase
    {
        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task EntriesWithSlashDotPrefix(bool async)
        {
            using TempDirectory root = new TempDirectory();
            using MemoryStream archiveStream = GetStrangeTarMemoryStream("prefixDotSlashAndCurrentFolderEntry");
            TarReader reader = await CreateTarReader(archiveStream, async, leaveOpen: false);
            try
            {
                string rootPath = Path.TrimEndingDirectorySeparator(root.Path);
                TarEntry entry;
                while ((entry = await GetNextEntry(reader, async)) != null)
                {
                    Assert.NotNull(entry);
                    Assert.StartsWith("./", entry.Name);
                    string entryPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(Path.Join(rootPath, entry.Name)));
                    if (entryPath != rootPath)
                    {
                        await ExtractToFile(entry, entryPath, overwrite: true, async);
                        Assert.True(Path.Exists(entryPath), $"Entry was not extracted: {entryPath}");
                    }
                }
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }
    }
}
