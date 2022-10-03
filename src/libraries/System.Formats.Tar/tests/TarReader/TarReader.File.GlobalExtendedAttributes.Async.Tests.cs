// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_GlobalExtendedAttributes_Async_Tests : TarReader_File_Tests_Async_Base
    {
        [Fact]
        public Task Read_Archive_File_Async() =>
            Read_Archive_File_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_File_HardLink_Async() =>
            Read_Archive_File_HardLink_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_File_SymbolicLink_Async() =>
            Read_Archive_File_SymbolicLink_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_Folder_File_Async() =>
            Read_Archive_Folder_File_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_Folder_File_Utf8_Async() =>
            Read_Archive_Folder_File_Utf8_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_Folder_Subfolder_File_Async() =>
            Read_Archive_Folder_Subfolder_File_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async() =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_Many_Small_Files_Async() =>
            Read_Archive_Many_Small_Files_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_LongPath_Splitable_Under255_Async() =>
            Read_Archive_LongPath_Splitable_Under255_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_SpecialFiles_Async() =>
            Read_Archive_SpecialFiles_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_File_LongSymbolicLink_Async() =>
            Read_Archive_File_LongSymbolicLink_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_LongFileName_Over100_Under255_Async() =>
            Read_Archive_LongFileName_Over100_Under255_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public Task Read_Archive_LongPath_Over255_Async() =>
            Read_Archive_LongPath_Over255_Async_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public async Task ExtractGlobalExtendedAttributesEntry_Throws_Async()
        {
            using (TempDirectory root = new TempDirectory())
            {
                await using (MemoryStream archiveStream = new MemoryStream())
                {
                    await using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
                    {
                        PaxGlobalExtendedAttributesTarEntry gea = new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                        await writer.WriteEntryAsync(gea);
                    }

                    archiveStream.Position = 0;

                    await using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
                    {
                        TarEntry entry = await reader.GetNextEntryAsync();
                        await Assert.ThrowsAsync<InvalidOperationException>(() => entry.ExtractToFileAsync(Path.Join(root.Path, "file"), overwrite: true));
                    }
                }
            }
        }
    }
}
