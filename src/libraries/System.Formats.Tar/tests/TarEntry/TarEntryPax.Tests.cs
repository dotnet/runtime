// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class PaxTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new PaxTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not be creating these entries manually in pax
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new PaxTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
        }

        [Fact]
        public void Constructor_ConversionFromV7()
        {
            V7TarEntry v7 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            PaxTarEntry convertedV7 = new PaxTarEntry(other: v7);

            Assert.Equal(TarEntryType.RegularFile, convertedV7.EntryType);
            Assert.Equal(InitialEntryName, convertedV7.Name);
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            PaxTarEntry convertedUstar = new PaxTarEntry(other: ustar);

            Assert.Equal(TarEntryType.RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }

        [Fact]
        public void Constructor_ConversionFromGnu()
        {
            GnuTarEntry gnu = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            PaxTarEntry convertedGnu = new PaxTarEntry(other: gnu);

            Assert.Equal(TarEntryType.RegularFile, convertedGnu.EntryType);
            Assert.Equal(InitialEntryName, convertedGnu.Name);
        }

        [Fact]
        public void Constructor_ConversionFromV7_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.v7, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            V7TarEntry v7Entry = sourceReader.GetNextEntry(copyData: false) as V7TarEntry;
            PaxTarEntry paxEntry = new PaxTarEntry(other: v7Entry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen: true))
            {
                writer.WriteEntry(paxEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                PaxTarEntry resultEntry = destinationReader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionFromUstar_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.ustar, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            UstarTarEntry ustarEntry = sourceReader.GetNextEntry(copyData: false) as UstarTarEntry;
            PaxTarEntry paxEntry = new PaxTarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen: true))
            {
                writer.WriteEntry(paxEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                PaxTarEntry resultEntry = destinationReader.GetNextEntry() as PaxTarEntry;
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
            PaxTarEntry paxEntry = new PaxTarEntry(other: gnuEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Pax, leaveOpen: true))
            {
                writer.WriteEntry(paxEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                PaxTarEntry resultEntry = destinationReader.GetNextEntry() as PaxTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            PaxTarEntry regularFile = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            PaxTarEntry directory = new PaxTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            PaxTarEntry hardLink = new PaxTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            PaxTarEntry symbolicLink = new PaxTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            PaxTarEntry blockDevice = new PaxTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            PaxTarEntry characterDevice = new PaxTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            PaxTarEntry fifo = new PaxTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, fullPath);

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

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, fullPath);

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

            PaxTarEntry entry = new PaxTarEntry(TarEntryType.RegularFile, fullPath);

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

            PaxTarEntry entry = new PaxTarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
