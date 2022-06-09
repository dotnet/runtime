// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class V7TarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new V7TarEntry(TarEntryType.V7RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.BlockDevice, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.CharacterDevice, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.Fifo, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.RegularFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedUstar = new V7TarEntry(other: ustar);

            Assert.Equal(TarEntryType.V7RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }

        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedPax = new V7TarEntry(other: pax);

            Assert.Equal(TarEntryType.V7RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }

        [Fact]
        public void Constructor_ConversionFromGnu()
        {
            GnuTarEntry gnu = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            V7TarEntry convertedGnu = new V7TarEntry(other: gnu);

            Assert.Equal(TarEntryType.V7RegularFile, convertedGnu.EntryType);
            Assert.Equal(InitialEntryName, convertedGnu.Name);
        }

        [Fact]
        public void Constructor_ConversionFromUstar_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            UstarTarEntry ustarEntry = sourceReader.GetNextEntry(copyData: false) as UstarTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionFromPax_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.pax, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            PaxTarEntry paxEntry = sourceReader.GetNextEntry(copyData: false) as PaxTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionFromGnu_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.gnu, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            GnuTarEntry gnuEntry = sourceReader.GetNextEntry(copyData: false) as GnuTarEntry;
            V7TarEntry v7Entry = new V7TarEntry(other: gnuEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.V7, leaveOpen: true))
            {
                writer.WriteEntry(v7Entry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                V7TarEntry resultEntry = destinationReader.GetNextEntry() as V7TarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth()
        {
            V7TarEntry firstEntry = new V7TarEntry(TarEntryType.V7RegularFile, "file.txt")
            {
                Gid = TestGid,
                Uid = TestUid
            };

            UstarTarEntry otherEntry = new UstarTarEntry(other: firstEntry);
            Assert.Equal(TarEntryType.RegularFile, otherEntry.EntryType);

            V7TarEntry secondEntry = new V7TarEntry(other: otherEntry);
            Assert.Equal(TarEntryType.V7RegularFile, secondEntry.EntryType);

            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestUid, secondEntry.Uid);
        }

        [Fact]
        public void Constructor_ConversionPax_BackAndForth()
        {
            V7TarEntry firstEntry = new V7TarEntry(TarEntryType.V7RegularFile, "file.txt")
            {
                Gid = TestGid,
                Uid = TestUid
            };

            PaxTarEntry otherEntry = new PaxTarEntry(other: firstEntry);
            Assert.Equal(TarEntryType.RegularFile, otherEntry.EntryType);

            V7TarEntry secondEntry = new V7TarEntry(other: otherEntry);
            Assert.Equal(TarEntryType.V7RegularFile, secondEntry.EntryType);

            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestUid, secondEntry.Uid);
        }

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth()
        {
            V7TarEntry firstEntry = new V7TarEntry(TarEntryType.V7RegularFile, "file.txt")
            {
                Gid = TestGid,
                Uid = TestUid
            };

            GnuTarEntry otherEntry = new GnuTarEntry(other: firstEntry);
            Assert.Equal(TarEntryType.RegularFile, otherEntry.EntryType);

            V7TarEntry secondEntry = new V7TarEntry(other: otherEntry);
            Assert.Equal(TarEntryType.V7RegularFile, secondEntry.EntryType);

            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestUid, secondEntry.Uid);
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Ustar()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new UstarTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Pax()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new PaxTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void Constructor_Conversion_UnsupportedEntryTypes_Gnu()
        {
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName)));
            Assert.Throws<InvalidOperationException>(() => new V7TarEntry(new GnuTarEntry(TarEntryType.Fifo, InitialEntryName)));
        }

        [Fact]
        public void SupportedEntryType_V7RegularFile()
        {
            V7TarEntry oldRegularFile = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            SetRegularFile(oldRegularFile);
            VerifyRegularFile(oldRegularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            V7TarEntry directory = new V7TarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            V7TarEntry hardLink = new V7TarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            V7TarEntry symbolicLink = new V7TarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Match_AdditionalSubdirectory_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "dir", "file.txt");

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            Assert.Throws<IOException>(() => entry.ExtractToFile(root.Path, overwrite: false));

            Assert.False(File.Exists(fullPath));
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Match()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(root.Path, "file.txt");

            V7TarEntry entry = new V7TarEntry(TarEntryType.V7RegularFile, fullPath);

            entry.DataStream = new MemoryStream();
            entry.DataStream.Write(new byte[] { 0x1 });
            entry.DataStream.Seek(0, SeekOrigin.Begin);

            entry.ExtractToFile(fullPath, overwrite: false);

            Assert.True(File.Exists(fullPath));
        }

        [Theory]
        [InlineData(TarEntryType.SymbolicLink)]
        [InlineData(TarEntryType.HardLink)]
        public void ExtractToFile_Link_Throws(TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();
            string fileName = "mylink";
            string fullPath = Path.Join(root.Path, fileName);

            string linkTarget = PlatformDetection.IsWindows ? @"C:\Windows\system32\notepad.exe" : "/usr/bin/nano";

            V7TarEntry entry = new V7TarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
