// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntryAsync_V7_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public Task WriteEntry_Null_Throws_Async() =>
            WriteEntry_Null_Throws_Async_Internal(TarEntryFormat.V7);

        [Fact]
        public async Task WriteRegularFile_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                await writer.WriteEntryAsync(oldRegularFile);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry oldRegularFile = await reader.GetNextEntryAsync() as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
            }
        }

        [Fact]
        public async Task WriteHardLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                await writer.WriteEntryAsync(hardLink);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry hardLink = await reader.GetNextEntryAsync() as V7TarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public async Task WriteSymbolicLink_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                await writer.WriteEntryAsync(symbolicLink);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry symbolicLink = await reader.GetNextEntryAsync() as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public async Task WriteDirectory_Async()
        {
            using MemoryStream archiveStream = new MemoryStream();
            await using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                await writer.WriteEntryAsync(directory);
            }

            archiveStream.Position = 0;
            await using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry directory = await reader.GetNextEntryAsync() as V7TarEntry;
                VerifyDirectory(directory);
            }
        }
    }
}
