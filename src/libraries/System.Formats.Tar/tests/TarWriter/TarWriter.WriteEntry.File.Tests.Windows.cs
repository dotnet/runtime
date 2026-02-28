// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
    public void Add_Junction_As_SymbolicLink(TarEntryFormat format)
    {
        using TempDirectory root = new TempDirectory();
        string targetName = "TargetDirectory";
        string junctionName = "JunctionDirectory";
        string targetPath = Path.Join(root.Path, targetName);
        string junctionPath = Path.Join(root.Path, junctionName);

        Directory.CreateDirectory(targetPath);

        Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));

        using MemoryStream archive = new MemoryStream();
        using (TarWriter writer = new TarWriter(archive, format, leaveOpen: true))
        {
            writer.WriteEntry(fileName: junctionPath, entryName: junctionPath);
        }

        archive.Position = 0;
        using (TarReader reader = new TarReader(archive))
        {
            TarEntry entry = reader.GetNextEntry();
            Assert.NotNull(entry);
            Assert.Equal(format, entry.Format);
            Assert.Equal(junctionPath, entry.Name);
            Assert.Equal(targetPath, entry.LinkName);
            Assert.Equal(TarEntryType.SymbolicLink, entry.EntryType);
            Assert.Null(entry.DataStream);

            VerifyPlatformSpecificMetadata(junctionPath, entry);

            Assert.Null(reader.GetNextEntry());
        }
    }

    [Theory]
    [InlineData(TarEntryFormat.V7)]
    [InlineData(TarEntryFormat.Ustar)]
    [InlineData(TarEntryFormat.Pax)]
    [InlineData(TarEntryFormat.Gnu)]
    public void Add_Non_Symlink_ReparsePoint_Throws(TarEntryFormat format)
    {
        string? appExecLinkPath = MountHelper.GetAppExecLinkPath();
        if (appExecLinkPath is null)
        {
            throw new SkipTestException("Could not find an appexeclink in this machine.");
        }

        using MemoryStream archive = new MemoryStream();
        using TarWriter writer = new TarWriter(archive, format);
        Assert.Throws<IOException>(() => writer.WriteEntry(fileName: appExecLinkPath, "NonSymlinkReparsePoint"));
    }
}
