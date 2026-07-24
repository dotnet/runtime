// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Formats.Tar.Tests;

public partial class TarWriter_WriteEntry_File_Tests : TarWriter_File_Base
{
    [Theory]
    [InlineData(TarEntryFormat.V7)]
    [InlineData(TarEntryFormat.Ustar)]
    [InlineData(TarEntryFormat.Pax)]
    [InlineData(TarEntryFormat.Gnu)]
    public async Task Add_Junction_As_SymbolicLink(TarEntryFormat format)
    {
        foreach (bool async in Booleans)
        {
            using TempDirectory root = new TempDirectory();
            string targetName = "TargetDirectory";
            string junctionName = "JunctionDirectory";
            string targetPath = Path.Join(root.Path, targetName);
            string junctionPath = Path.Join(root.Path, junctionName);

            Directory.CreateDirectory(targetPath);

            Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));

            using MemoryStream archive = new MemoryStream();
            TarWriter writer = CreateTarWriter(archive, format, leaveOpen: true);
            try
            {
                await WriteEntry(writer, junctionPath, junctionPath, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archive.Position = 0;
            TarReader reader = CreateTarReader(archive);
            try
            {
                TarEntry entry = await GetNextEntry(reader, async: async);
                Assert.NotNull(entry);
                Assert.Equal(format, entry.Format);
                Assert.Equal(junctionPath, entry.Name);
                Assert.Equal(targetPath, entry.LinkName);
                Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
                Assert.Null(entry.DataStream);

                VerifyPlatformSpecificMetadata(junctionPath, entry);

                Assert.Null(await GetNextEntry(reader, async: async));
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }
    }

    [ConditionalTheory]
    [InlineData(TarEntryFormat.V7)]
    [InlineData(TarEntryFormat.Ustar)]
    [InlineData(TarEntryFormat.Pax)]
    [InlineData(TarEntryFormat.Gnu)]
    public async Task Add_Non_Symlink_ReparsePoint_Throws(TarEntryFormat format)
    {
        string? appExecLinkPath = MountHelper.GetAppExecLinkPath();
        if (appExecLinkPath is null)
        {
            throw new SkipTestException("Could not find an appexeclink in this machine.");
        }

        foreach (bool async in Booleans)
        {
            using MemoryStream archive = new MemoryStream();
            TarWriter writer = CreateTarWriter(archive, format);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<IOException>(() => writer.WriteEntryAsync(fileName: appExecLinkPath, "NonSymlinkReparsePoint"));
                }
                else
                {
                    Assert.Throws<IOException>(() => writer.WriteEntry(fileName: appExecLinkPath, "NonSymlinkReparsePoint"));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }
    }
}
