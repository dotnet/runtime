// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntry_V7_Tests : TarWriter_WriteEntry_Base
    {
        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteEntry_Null_Throws(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: false, async: async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentNullException>(() => writer.WriteEntryAsync(null));
                }
                else
                {
                    Assert.Throws<ArgumentNullException>(() => writer.WriteEntry(null));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteRegularFile(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true, async: async);
            try
            {
                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                await WriteEntry(writer, oldRegularFile, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = await CreateTarReader(archiveStream, async: async);
            try
            {
                V7TarEntry oldRegularFile = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteHardLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true, async: async);
            try
            {
                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await WriteEntry(writer, hardLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = await CreateTarReader(archiveStream, async: async);
            try
            {
                V7TarEntry hardLink = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyHardLink(hardLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteSymbolicLink(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true, async: async);
            try
            {
                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await WriteEntry(writer, symbolicLink, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = await CreateTarReader(archiveStream, async: async);
            try
            {
                V7TarEntry symbolicLink = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [MemberData(nameof(Get_Boolean_Data))]
        public async Task WriteDirectory(bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true, async: async);
            try
            {
                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await WriteEntry(writer, directory, async);
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }

            archiveStream.Position = 0;
            TarReader reader = await CreateTarReader(archiveStream, async: async);
            try
            {
                V7TarEntry directory = await GetNextEntry(reader, async: async) as V7TarEntry;
                VerifyDirectory(directory);
            }
            finally
            {
                await DisposeTarReader(reader, async);
            }
        }

        [Theory]
        [InlineData(TarEntryType.HardLink, false)]
        [InlineData(TarEntryType.HardLink, true)]
        [InlineData(TarEntryType.SymbolicLink, false)]
        [InlineData(TarEntryType.SymbolicLink, true)]
        public async Task Write_LinkEntry_EmptyLinkName_Throws(TarEntryType entryType, bool async)
        {
            using MemoryStream archiveStream = new MemoryStream();
            TarWriter writer = await CreateTarWriter(archiveStream, leaveOpen: false, async: async);
            try
            {
                if (async)
                {
                    await Assert.ThrowsAsync<ArgumentException>("entry", () => writer.WriteEntryAsync(new V7TarEntry(entryType, "link")));
                }
                else
                {
                    Assert.Throws<ArgumentException>("entry", () => writer.WriteEntry(new V7TarEntry(entryType, "link")));
                }
            }
            finally
            {
                await DisposeTarWriter(writer, async);
            }
        }
    }
}