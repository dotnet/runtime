// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class UstarTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new UstarTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.LongPath, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.V7RegularFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new UstarTarEntry(TarEntryType.TapeVolume, InitialEntryName));
        }

        [Fact]
        public void Constructor_ConversionFromV7()
        {
            V7TarEntry v7 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            UstarTarEntry convertedV7 = new UstarTarEntry(other: v7);

            Assert.Equal(TarEntryType.RegularFile, convertedV7.EntryType);
            Assert.Equal(InitialEntryName, convertedV7.Name);
        }

        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            UstarTarEntry convertedPax = new UstarTarEntry(other: pax);

            Assert.Equal(TarEntryType.RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }

        [Fact]
        public void Constructor_ConversionFromGnu()
        {
            GnuTarEntry gnu = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            UstarTarEntry convertedGnu = new UstarTarEntry(other: gnu);

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
            UstarTarEntry ustarEntry = new UstarTarEntry(other: v7Entry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
            {
                writer.WriteEntry(ustarEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                UstarTarEntry resultEntry = destinationReader.GetNextEntry() as UstarTarEntry;
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
            UstarTarEntry ustarEntry = new UstarTarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
            {
                writer.WriteEntry(ustarEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                UstarTarEntry resultEntry = destinationReader.GetNextEntry() as UstarTarEntry;
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
            UstarTarEntry ustarEntry = new UstarTarEntry(other: gnuEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Ustar, leaveOpen: true))
            {
                writer.WriteEntry(ustarEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                UstarTarEntry resultEntry = destinationReader.GetNextEntry() as UstarTarEntry;
                Assert.NotNull(resultEntry);
                using (StreamReader streamReader = new StreamReader(resultEntry.DataStream))
                {
                    Assert.Equal("Hello file", streamReader.ReadToEnd());
                }
            }
        }

        [Fact]
        public void Constructor_ConversionV7_BackAndForth()
        {
            // V7 does not support blockdev, so can't verify transfer of DeviceMajor/DeviceMinor fields
            UstarTarEntry firstEntry = new UstarTarEntry(TarEntryType.RegularFile, "file.txt")
            {
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            V7TarEntry otherEntry = new V7TarEntry(other: firstEntry);
            Assert.Equal(TarEntryType.V7RegularFile, otherEntry.EntryType);

            UstarTarEntry secondEntry = new UstarTarEntry(other: otherEntry);
            Assert.Equal(TarEntryType.RegularFile, secondEntry.EntryType);

            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(DefaultGName, secondEntry.GroupName);
            Assert.Equal(TestUid, secondEntry.Uid);
            Assert.Equal(DefaultUName, secondEntry.UserName);
        }

        [Fact]
        public void Constructor_ConversionPax_BackAndForth()
        {
            UstarTarEntry firstEntry = new UstarTarEntry(TarEntryType.BlockDevice, "blockdev")
            {
                DeviceMajor = TestBlockDeviceMajor,
                DeviceMinor = TestBlockDeviceMinor,
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            PaxTarEntry otherEntry = new PaxTarEntry(other: firstEntry);

            UstarTarEntry secondEntry = new UstarTarEntry(other: otherEntry);

            Assert.Equal(TestBlockDeviceMajor, secondEntry.DeviceMajor);
            Assert.Equal(TestBlockDeviceMinor, secondEntry.DeviceMinor);
            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestGName, secondEntry.GroupName);
            Assert.Equal(TestUid, secondEntry.Uid);
            Assert.Equal(TestUName, secondEntry.UserName);
        }

        [Fact]
        public void Constructor_ConversionGnu_BackAndForth()
        {
            UstarTarEntry firstEntry = new UstarTarEntry(TarEntryType.BlockDevice, "blockdev")
            {
                DeviceMajor = TestBlockDeviceMajor,
                DeviceMinor = TestBlockDeviceMinor,
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            GnuTarEntry otherEntry = new GnuTarEntry(other: firstEntry);

            UstarTarEntry secondEntry = new UstarTarEntry(other: otherEntry);

            Assert.Equal(TestBlockDeviceMajor, secondEntry.DeviceMajor);
            Assert.Equal(TestBlockDeviceMinor, secondEntry.DeviceMinor);
            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestGName, secondEntry.GroupName);
            Assert.Equal(TestUid, secondEntry.Uid);
            Assert.Equal(TestUName, secondEntry.UserName);
        }

        [Fact]
        public void SupportedEntryType_RegularFile()
        {
            UstarTarEntry regularFile = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            UstarTarEntry directory = new UstarTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            UstarTarEntry hardLink = new UstarTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            UstarTarEntry symbolicLink = new UstarTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            UstarTarEntry blockDevice = new UstarTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            UstarTarEntry characterDevice = new UstarTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            UstarTarEntry fifo = new UstarTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

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

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

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

            UstarTarEntry entry = new UstarTarEntry(TarEntryType.RegularFile, fullPath);

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

            UstarTarEntry entry = new UstarTarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
