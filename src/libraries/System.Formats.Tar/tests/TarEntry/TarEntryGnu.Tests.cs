// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class GnuTarEntry_Tests : TarTestsBase
    {
        [Fact]
        public void Constructor_InvalidEntryName()
        {
            Assert.Throws<ArgumentNullException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: null));
            Assert.Throws<ArgumentException>(() => new GnuTarEntry(TarEntryType.RegularFile, entryName: string.Empty));
        }

        [Fact]
        public void Constructor_UnsupportedEntryTypes()
        {
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry((TarEntryType)byte.MaxValue, InitialEntryName));

            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.ExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.GlobalExtendedAttributes, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.V7RegularFile, InitialEntryName));

            // These are specific to GNU, but currently the user cannot create them manually
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.ContiguousFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.DirectoryList, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.MultiVolume, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.RenamedOrSymlinked, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.SparseFile, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.TapeVolume, InitialEntryName));

            // The user should not create these entries manually
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.LongLink, InitialEntryName));
            Assert.Throws<InvalidOperationException>(() => new GnuTarEntry(TarEntryType.LongPath, InitialEntryName));
        }

        [Fact]
        public void Constructor_ConversionFromV7()
        {
            V7TarEntry v7 = new V7TarEntry(TarEntryType.V7RegularFile, InitialEntryName);
            GnuTarEntry convertedV7 = new GnuTarEntry(other: v7);

            Assert.Equal(TarEntryType.RegularFile, convertedV7.EntryType);
            Assert.Equal(InitialEntryName, convertedV7.Name);
        }

        [Fact]
        public void Constructor_ConversionFromUstar()
        {
            UstarTarEntry ustar = new UstarTarEntry(TarEntryType.RegularFile, InitialEntryName);
            GnuTarEntry convertedUstar = new GnuTarEntry(other: ustar);

            Assert.Equal(TarEntryType.RegularFile, convertedUstar.EntryType);
            Assert.Equal(InitialEntryName, convertedUstar.Name);
        }

        [Fact]
        public void Constructor_ConversionFromPax()
        {
            PaxTarEntry pax = new PaxTarEntry(TarEntryType.RegularFile, InitialEntryName);
            GnuTarEntry convertedPax = new GnuTarEntry(other: pax);

            Assert.Equal(TarEntryType.RegularFile, convertedPax.EntryType);
            Assert.Equal(InitialEntryName, convertedPax.Name);
        }

        [Fact]
        public void Constructor_ConversionFromV7_From_UnseekableTarReader()
        {
            using MemoryStream source = GetTarMemoryStream(CompressionMethod.Uncompressed, TestTarFormat.v7, "file");
            using WrappedStream wrappedSource = new WrappedStream(source, canRead: true, canWrite: false, canSeek: false);

            using TarReader sourceReader = new TarReader(wrappedSource, leaveOpen: true);
            V7TarEntry v7Entry = sourceReader.GetNextEntry(copyData: false) as V7TarEntry;
            GnuTarEntry gnuEntry = new GnuTarEntry(other: v7Entry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
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
            GnuTarEntry gnuEntry = new GnuTarEntry(other: ustarEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
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
            GnuTarEntry gnuEntry = new GnuTarEntry(other: paxEntry); // Convert, and avoid advancing wrappedSource position

            using MemoryStream destination = new MemoryStream();
            using (TarWriter writer = new TarWriter(destination, TarEntryFormat.Gnu, leaveOpen: true))
            {
                writer.WriteEntry(gnuEntry); // Write DataStream exactly where the wrappedSource position was left
            }

            destination.Position = 0; // Rewind
            using (TarReader destinationReader = new TarReader(destination, leaveOpen: false))
            {
                GnuTarEntry resultEntry = destinationReader.GetNextEntry() as GnuTarEntry;
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
            DateTimeOffset firstNow = DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(10);
            // V7 does not support blockdev, so can't verify transfer of DeviceMajor/DeviceMinor fields
            GnuTarEntry firstEntry = new GnuTarEntry(TarEntryType.RegularFile, "file.txt")
            {
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            Assert.True(firstEntry.AccessTime > firstNow);
            Assert.True(firstEntry.ChangeTime > firstNow);

            DateTimeOffset secondNow = firstEntry.AccessTime;

            V7TarEntry otherEntry = new V7TarEntry(other: firstEntry);
            Assert.Equal(TarEntryType.V7RegularFile, otherEntry.EntryType);

            GnuTarEntry secondEntry = new GnuTarEntry(other: otherEntry);
            Assert.Equal(TarEntryType.RegularFile, secondEntry.EntryType);

            Assert.True(secondEntry.AccessTime > secondNow, "secondEntry.AccessTime is not greater than secondNow");
            Assert.True(secondEntry.ChangeTime > secondNow, "secondEntry.ChangeTime is not greater than secondNow");
            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(DefaultGName, secondEntry.GroupName);
            Assert.Equal(TestUid, secondEntry.Uid);
            Assert.Equal(DefaultUName, secondEntry.UserName);
        }

        [Fact]
        public void Constructor_ConversionUstar_BackAndForth()
        {
            DateTimeOffset firstNow = DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(10);
            GnuTarEntry firstEntry = new GnuTarEntry(TarEntryType.BlockDevice, "blockdev")
            {
                DeviceMajor = TestBlockDeviceMajor,
                DeviceMinor = TestBlockDeviceMinor,
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            Assert.True(firstEntry.AccessTime > firstNow);
            Assert.True(firstEntry.ChangeTime > firstNow);

            DateTimeOffset secondNow = firstEntry.AccessTime;

            UstarTarEntry otherEntry = new UstarTarEntry(other: firstEntry);

            GnuTarEntry secondEntry = new GnuTarEntry(other: otherEntry);

            Assert.True(secondEntry.AccessTime > secondNow, "secondEntry.AccessTime is not greater than secondNow");
            Assert.True(secondEntry.ChangeTime > secondNow, "secondEntry.ChangeTime is not greater than secondNow");
            Assert.Equal(TestBlockDeviceMajor, secondEntry.DeviceMajor);
            Assert.Equal(TestBlockDeviceMinor, secondEntry.DeviceMinor);
            Assert.Equal(TestGid, secondEntry.Gid);
            Assert.Equal(TestGName, secondEntry.GroupName);
            Assert.Equal(TestUid, secondEntry.Uid);
            Assert.Equal(TestUName, secondEntry.UserName);
        }

        [Fact]
        public void Constructor_ConversionPax_BackAndForth()
        {
            DateTimeOffset firstNow = DateTimeOffset.UtcNow - TimeSpan.FromMilliseconds(10);
            GnuTarEntry firstEntry = new GnuTarEntry(TarEntryType.BlockDevice, "blockdev")
            {
                DeviceMajor = TestBlockDeviceMajor,
                DeviceMinor = TestBlockDeviceMinor,
                Gid = TestGid,
                GroupName = TestGName,
                Uid = TestUid,
                UserName = TestUName,
            };

            Assert.True(firstEntry.AccessTime > firstNow);
            Assert.True(firstEntry.ChangeTime > firstNow);

            PaxTarEntry otherEntry = new PaxTarEntry(other: firstEntry);

            GnuTarEntry secondEntry = new GnuTarEntry(other: otherEntry);

            // atime and ctime should be transferred from gnu to pax and then back to gnu
            DateTimeOffset originalATime = firstEntry.AccessTime;
            DateTimeOffset originalCTime = firstEntry.ChangeTime;
            CompareDateTimeOffsets(originalATime, secondEntry.AccessTime);
            CompareDateTimeOffsets(originalCTime, secondEntry.ChangeTime);

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
            GnuTarEntry regularFile = new GnuTarEntry(TarEntryType.RegularFile, InitialEntryName);
            SetRegularFile(regularFile);
            VerifyRegularFile(regularFile, isWritable: true);
        }

        [Fact]
        public void SupportedEntryType_Directory()
        {
            GnuTarEntry directory = new GnuTarEntry(TarEntryType.Directory, InitialEntryName);
            SetDirectory(directory);
            VerifyDirectory(directory);
        }

        [Fact]
        public void SupportedEntryType_HardLink()
        {
            GnuTarEntry hardLink = new GnuTarEntry(TarEntryType.HardLink, InitialEntryName);
            SetHardLink(hardLink);
            VerifyHardLink(hardLink);
        }

        [Fact]
        public void SupportedEntryType_SymbolicLink()
        {
            GnuTarEntry symbolicLink = new GnuTarEntry(TarEntryType.SymbolicLink, InitialEntryName);
            SetSymbolicLink(symbolicLink);
            VerifySymbolicLink(symbolicLink);
        }

        [Fact]
        public void SupportedEntryType_BlockDevice()
        {
            GnuTarEntry blockDevice = new GnuTarEntry(TarEntryType.BlockDevice, InitialEntryName);
            SetBlockDevice(blockDevice);
            VerifyBlockDevice(blockDevice);
        }

        [Fact]
        public void SupportedEntryType_CharacterDevice()
        {
            GnuTarEntry characterDevice = new GnuTarEntry(TarEntryType.CharacterDevice, InitialEntryName);
            SetCharacterDevice(characterDevice);
            VerifyCharacterDevice(characterDevice);
        }

        [Fact]
        public void SupportedEntryType_Fifo()
        {
            GnuTarEntry fifo = new GnuTarEntry(TarEntryType.Fifo, InitialEntryName);
            SetFifo(fifo);
            VerifyFifo(fifo);
        }

        [Fact]
        public void Constructor_Name_FullPath_DestinationDirectory_Mismatch_Throws()
        {
            using TempDirectory root = new TempDirectory();

            string fullPath = Path.Join(Path.GetPathRoot(root.Path), "dir", "file.txt");

            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, fullPath);

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

            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, fullPath);

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

            GnuTarEntry entry = new GnuTarEntry(TarEntryType.RegularFile, fullPath);

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

            GnuTarEntry entry = new GnuTarEntry(entryType, fileName);
            entry.LinkName = linkTarget;

            Assert.Throws<InvalidOperationException>(() => entry.ExtractToFile(fileName, overwrite: false));

            Assert.Equal(0, Directory.GetFileSystemEntries(root.Path).Count());
        }
    }
}
