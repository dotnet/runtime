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
    [MemberData(nameof(GetFormatBooleanData))]
    public async Task Add_Junction_As_SymbolicLink(TarEntryFormat format, bool async)
    {
        using TempDirectory root = new TempDirectory();
        string targetName = "TargetDirectory";
        string junctionName = "JunctionDirectory";
        string targetPath = Path.Join(root.Path, targetName);
        string junctionPath = Path.Join(root.Path, junctionName);

        Directory.CreateDirectory(targetPath);

        Assert.True(MountHelper.CreateJunction(junctionPath, targetPath));

        using MemoryStream archive = new MemoryStream();
        {
            await using TarWriterHolder writerHolder = CreateTarWriter(archive, async, format, leaveOpen: true);
            TarWriter writer = writerHolder;

            await WriteEntry(writer, junctionPath, junctionPath, async);
        }

        archive.Position = 0;
        {
            await using TarReaderHolder readerHolder = CreateTarReader(archive, async, leaveOpen: false);
            TarReader reader = readerHolder;

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
    }

    [ConditionalTheory]
    [MemberData(nameof(GetFormatBooleanData))]
    public async Task Add_Non_Symlink_ReparsePoint_Throws(TarEntryFormat format, bool async)
    {
        string? appExecLinkPath = MountHelper.GetAppExecLinkPath();
        if (appExecLinkPath is null)
        {
            throw new SkipTestException("Could not find an appexeclink in this machine.");
        }

        using MemoryStream archive = new MemoryStream();
        {
            await using TarWriterHolder writerHolder = CreateTarWriter(archive, async, format);
            TarWriter writer = writerHolder;

            await Assert.ThrowsAsync<IOException>(() => WriteEntry(writer, appExecLinkPath, "NonSymlinkReparsePoint", async));
        }
    }
}
