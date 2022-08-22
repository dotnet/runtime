// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    // Tests specific to V7 format.
    public class TarWriter_WriteEntry_V7_Tests : TarWriter_WriteEntry_Base
    {
        [Fact]
        public void WriteEntry_Null_Throws() =>
            WriteEntry_Null_Throws_Internal(TarEntryFormat.V7);

        [Fact]
        public void WriteRegularFile()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
                SetRegularFile(oldRegularFile);
                VerifyRegularFile(oldRegularFile, isWritable: true);
                writer.WriteEntry(oldRegularFile);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry oldRegularFile = reader.GetNextEntry() as V7TarEntry;
                VerifyRegularFile(oldRegularFile, isWritable: false);
            }
        }

        [Fact]
        public void WriteHardLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
                SetHardLink(hardLink);
                VerifyHardLink(hardLink);
                writer.WriteEntry(hardLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry hardLink = reader.GetNextEntry() as V7TarEntry;
                VerifyHardLink(hardLink);
            }
        }

        [Fact]
        public void WriteSymbolicLink()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
                SetSymbolicLink(symbolicLink);
                VerifySymbolicLink(symbolicLink);
                writer.WriteEntry(symbolicLink);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry symbolicLink = reader.GetNextEntry() as V7TarEntry;
                VerifySymbolicLink(symbolicLink);
            }
        }

        [Fact]
        public void WriteDirectory()
        {
            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, TarEntryFormat.V7, leaveOpen: true))
            {
                V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
                SetDirectory(directory);
                VerifyDirectory(directory);
                writer.WriteEntry(directory);
            }

            archiveStream.Position = 0;
            using (TarReader reader = new TarReader(archiveStream))
            {
                V7TarEntry directory = reader.GetNextEntry() as V7TarEntry;
                VerifyDirectory(directory);
            }
        }
    }
}
