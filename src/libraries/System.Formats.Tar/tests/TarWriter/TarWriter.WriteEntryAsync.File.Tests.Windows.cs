// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;
using Xunit.Sdk;

namespace System.Formats.Tar.Tests;

public partial class TarWriter_WriteEntryAsync_File_Tests : TarWriter_File_Base
{
    [Theory]
    [InlineData(TarEntryFormat.V7)]
    [InlineData(TarEntryFormat.Ustar)]
    [InlineData(TarEntryFormat.Pax)]
    [InlineData(TarEntryFormat.Gnu)]
    public async Task Add_Junction_As_SymbolicLink_Async(TarEntryFormat format)
    {
        using TempDirectory root = new TempDirectory();
        string targetName = "TargetDirectory";
        string junctionName = "JunctionDirectory";
        string targetPath = Path.Join(root.Path, targetName);
        string junctionPath = Path.Join(root.Path, junctionName);

        Directory.CreateDirectory(targetPath);

        Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));

        await using MemoryStream archive = new MemoryStream();
        await using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
        {
            await writer.WriteEntryAsync(fileName: junctionPath, entryName: junctionPath);
        }

        archive.Position = 0;
        await using (TarReader reader = new TarReader(archive))
        {
            TarEntry entry = await reader.GetNextEntryAsync();
            Assert.NotNull(entry);
            Assert.Equal(format, entry.Format);
            Assert.Equal(junctionPath, entry.Name);
            Assert.Equal(targetPath, entry.LinkName);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
            Assert.Null(entry.DataStream);

            VerifyPlatformSpecificMetadata(junctionPath, entry);

            Assert.Null(await reader.GetNextEntryAsync());
        }
    }

    [Theory]
    [InlineData(TarEntryFormat.V7)]
    [InlineData(TarEntryFormat.Ustar)]
    [InlineData(TarEntryFormat.Pax)]
    [InlineData(TarEntryFormat.Gnu)]
    public async Task Add_Non_Symlink_ReparsePoint_Throws_Async(TarEntryFormat format)
    {
        string? appExecLinkPath = MountHelper.GetAppExecLinkPath();
        if (appExecLinkPath is null)
        {
            throw SkipException.ForSkip("Could not find an appexeclink in this machine.");
        }

        await using MemoryStream archive = new MemoryStream();
        await using TarWriter writer = new TarWriter(archive, format);
        await Assert.ThrowsAsync<IOException>(async () => await writer.WriteEntryAsync(fileName: appExecLinkPath, "NonSymlinkReparsePoint"));
    }
}
