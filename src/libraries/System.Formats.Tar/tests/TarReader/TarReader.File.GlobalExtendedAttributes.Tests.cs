// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_GlobalExtendedAttributes_Tests : TarReader_File_Tests_Base
    {
        [Fact]
        public void Read_Archive_File() =>
            Read_Archive_File_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_File_HardLink() =>
            Read_Archive_File_HardLink_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_File_SymbolicLink() =>
            Read_Archive_File_SymbolicLink_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_Folder_File() =>
            Read_Archive_Folder_File_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_Folder_File_Utf8() =>
            Read_Archive_Folder_File_Utf8_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_Folder_Subfolder_File() =>
            Read_Archive_Folder_Subfolder_File_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_FolderSymbolicLink_Folder_Subfolder_File() =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_Many_Small_Files() =>
            Read_Archive_Many_Small_Files_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_LongPath_Splitable_Under255() =>
            Read_Archive_LongPath_Splitable_Under255_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_SpecialFiles() =>
            Read_Archive_SpecialFiles_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_File_LongSymbolicLink() =>
            Read_Archive_File_LongSymbolicLink_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_LongFileName_Over100_Under255() =>
            Read_Archive_LongFileName_Over100_Under255_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void Read_Archive_LongPath_Over255() =>
            Read_Archive_LongPath_Over255_Internal(TarEntryFormat.Pax, TestTarFormat.pax_gea);

        [Fact]
        public void ExtractGlobalExtendedAttributesEntry_Throws()
        {
            using TempDirectory root = new TempDirectory();

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, leaveOpen: true))
            {
                PaxGlobalExtendedAttributesTarEntry gea = new PaxGlobalExtendedAttributesTarEntry(new Dictionary<string, string>());
                writer.WriteEntry(gea);
            }

            archiveStream.Position = 0;

            using (TarReader reader = new TarReader(archiveStream, leaveOpen: false))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(Path.Join(root.Path, "file"), overwrite: true));
            }
        }
    }
}
