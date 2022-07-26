// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Async_Tests : TarReader_File_Tests_Async_Base
    {
        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_HardLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_HardLink_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_SymbolicLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_SymbolicLink_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_File_Utf8_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_File_Utf8_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Folder_Subfolder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Folder_Subfolder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_FolderSymbolicLink_Folder_Subfolder_File_Async_Internal(format, testFormat);

        [Theory]
        [InlineData(TarEntryFormat.V7, TestTarFormat.v7)]
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_Many_Small_Files_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_Many_Small_Files_Async_Internal(format, testFormat);

        [Theory]
        // V7 does not support longer filenames
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongPath_Splitable_Under255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Splitable_Under255_Async_Internal(format, testFormat);

        [Theory]
        // V7 does not support block devices, character devices or fifos
        [InlineData(TarEntryFormat.Ustar, TestTarFormat.ustar)]
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_SpecialFiles_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_SpecialFiles_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle links with long target filenames
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_File_LongSymbolicLink_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_File_LongSymbolicLink_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle a path that does not have separators that can be split under 100 bytes
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongFileName_Over100_Under255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongFileName_Over100_Under255_Async_Internal(format, testFormat);

        [Theory]
        // Neither V7 not Ustar can handle path lengths waaaay beyond name+prefix length
        [InlineData(TarEntryFormat.Pax, TestTarFormat.pax)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.gnu)]
        [InlineData(TarEntryFormat.Gnu, TestTarFormat.oldgnu)]
        public Task Read_Archive_LongPath_Over255_Async(TarEntryFormat format, TestTarFormat testFormat) =>
            Read_Archive_LongPath_Over255_Async_Internal(format, testFormat);
    }
}
