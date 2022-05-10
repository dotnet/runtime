// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected const string InitialEntryName = "InitialEntryName.ext";
        protected readonly string ModifiedEntryName = "ModifiedEntryName.ext";

        // Default values are what a TarEntry created with its constructor will set
        protected const TarFileMode DefaultMode = TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.GroupRead | TarFileMode.OtherRead; // 644 in octal, internally used as default
        protected const TarFileMode DefaultWindowsMode = TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.UserExecute | TarFileMode.GroupRead | TarFileMode.GroupWrite | TarFileMode.GroupExecute | TarFileMode.OtherRead | TarFileMode.OtherWrite | TarFileMode.UserExecute; // Creating archives in Windows always sets the mode to 777
        protected const int DefaultGid = 0;
        protected const int DefaultUid = 0;
        protected const int DefaultDeviceMajor = 0;
        protected const int DefaultDeviceMinor = 0;
        protected readonly string DefaultLinkName = string.Empty;
        protected readonly string DefaultGName = string.Empty;
        protected readonly string DefaultUName = string.Empty;

        // Values to which properties will be modified in tests
        protected const int TestGid = 1234;
        protected const int TestUid = 5678;
        protected const int TestBlockDeviceMajor = 61;
        protected const int TestBlockDeviceMinor = 65;
        protected const int TestCharacterDeviceMajor = 51;
        protected const int TestCharacterDeviceMinor = 42;
        protected readonly DateTimeOffset TestModificationTime = new DateTimeOffset(2003, 3, 3, 3, 33, 33, TimeSpan.Zero);
        protected readonly DateTimeOffset TestAccessTime = new DateTimeOffset(2022, 2, 2, 2, 22, 22, TimeSpan.Zero);
        protected readonly DateTimeOffset TestChangeTime = new DateTimeOffset(2011, 11, 11, 11, 11, 11, TimeSpan.Zero);
        protected readonly string TestLinkName = "TestLinkName";
        protected const TarFileMode TestMode = TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.GroupRead | TarFileMode.GroupWrite | TarFileMode.OtherRead | TarFileMode.OtherWrite;
        protected readonly DateTimeOffset TestTimestamp = DateTimeOffset.Now;
        protected const string TestGName = "group";
        protected const string TestUName = "user";

        // The metadata of the entries inside the asset archives are all set to these values
        protected const int AssetGid = 3579;
        protected const int AssetUid = 7913;
        protected const string AssetBlockDeviceFileName = "blockdev";
        protected const string AssetCharacterDeviceFileName = "chardev";
        protected const int AssetBlockDeviceMajor = 71;
        protected const int AssetBlockDeviceMinor = 53;
        protected const int AssetCharacterDeviceMajor = 49;
        protected const int AssetCharacterDeviceMinor = 86;
        protected const TarFileMode AssetMode = TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.UserExecute | TarFileMode.GroupRead | TarFileMode.OtherRead;
        protected const TarFileMode AssetSpecialFileMode = TarFileMode.UserRead | TarFileMode.UserWrite | TarFileMode.GroupRead | TarFileMode.OtherRead;
        protected const TarFileMode AssetSymbolicLinkMode = TarFileMode.OtherExecute | TarFileMode.OtherWrite | TarFileMode.OtherRead | TarFileMode.GroupExecute | TarFileMode.GroupWrite | TarFileMode.GroupRead | TarFileMode.UserExecute | TarFileMode.UserWrite | TarFileMode.UserRead;
        protected const string AssetGName = "devdiv";
        protected const string AssetUName = "dotnet";
        protected const string AssetPaxGeaKey = "globexthdr.MyGlobalExtendedAttribute";
        protected const string AssetPaxGeaValue = "hello";

        protected enum CompressionMethod
        {
            // Archiving only, no compression
            Uncompressed,
            // Archive compressed with Gzip
            GZip,
        }

        // Names match the testcase foldername
        public enum TestTarFormat
        {
            // V7 formatted files.
            v7,
            // UStar formatted files.
            ustar,
            // PAX formatted files.
            pax,
            // PAX formatted files that include a single Global Extended Attributes entry in the first position.
            pax_gea,
            // Old GNU formatted files. Format used by GNU tar of versions prior to 1.12.
            oldgnu,
            // GNU formatted files. Format used by GNU tar versions up to 1.13.25.
            gnu
        }

        protected static string GetTestCaseUnarchivedFolderPath(string testCaseName) =>
            Path.Join(Directory.GetCurrentDirectory(), "unarchived", testCaseName);

        protected static string GetTarFilePath(CompressionMethod compressionMethod, TestTarFormat format, string testCaseName)
        {
            (string compressionMethodFolder, string fileExtension) = compressionMethod switch
            {
                CompressionMethod.Uncompressed => ("tar", ".tar"),
                CompressionMethod.GZip => ("targz", ".tar.gz"),
                _ => throw new InvalidOperationException($"Unexpected compression method: {compressionMethod}"),
            };

            return Path.Join(Directory.GetCurrentDirectory(), compressionMethodFolder, format.ToString(), testCaseName + fileExtension);
        }

        // MemoryStream containing the copied contents of the specified file. Meant for reading and writing.
        protected static MemoryStream GetTarMemoryStream(CompressionMethod compressionMethod, TestTarFormat format, string testCaseName)
        {
            string path = GetTarFilePath(compressionMethod, format, testCaseName);
            FileStreamOptions options = new()
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read

            };
            MemoryStream ms = new();
            using (FileStream fs = new FileStream(path, options))
            {
                fs.CopyTo(ms);
            }
            ms.Seek(0, SeekOrigin.Begin);
            return ms;
        }

        protected void SetCommonRegularFile(TarEntry regularFile, bool isV7RegularFile = false)
        {
            Assert.NotNull(regularFile);
            TarEntryType entryType = isV7RegularFile ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;

            Assert.Equal(entryType, regularFile.EntryType);
            SetCommonProperties(regularFile);

            // Data stream
            Assert.Null(regularFile.DataStream);
        }

        protected void SetCommonDirectory(TarEntry directory)
        {
            Assert.NotNull(directory);
            Assert.Equal(TarEntryType.Directory, directory.EntryType);
            SetCommonProperties(directory);
        }

        protected void SetCommonHardLink(TarEntry hardLink)
        {
            Assert.NotNull(hardLink);
            Assert.Equal(TarEntryType.HardLink, hardLink.EntryType);
            SetCommonProperties(hardLink);

            // LinkName
            Assert.Equal(DefaultLinkName, hardLink.LinkName);
            hardLink.LinkName = TestLinkName;
        }

        protected void SetCommonSymbolicLink(TarEntry symbolicLink)
        {
            Assert.NotNull(symbolicLink);
            Assert.Equal(TarEntryType.SymbolicLink, symbolicLink.EntryType);
            SetCommonProperties(symbolicLink);

            // LinkName
            Assert.Equal(DefaultLinkName, symbolicLink.LinkName);
            symbolicLink.LinkName = TestLinkName;
        }

        protected void SetCommonProperties(TarEntry entry)
        {
            // Length (Data is checked outside this method)
            Assert.Equal(0, entry.Length);

            // Checksum
            Assert.Equal(0, entry.Checksum);

            // Gid
            Assert.Equal(DefaultGid, entry.Gid);
            entry.Gid = TestGid;

            // Mode
            Assert.Equal(DefaultMode, entry.Mode);
            entry.Mode = TestMode;

            // MTime: Verify the default value was approximately "now" by default
            DateTimeOffset approxNow = DateTimeOffset.Now.Subtract(TimeSpan.FromHours(6));
            Assert.True(entry.ModificationTime > approxNow);

            Assert.Throws<ArgumentOutOfRangeException>(() => entry.ModificationTime = DateTime.MinValue); // Minimum allowed is UnixEpoch, not MinValue
            entry.ModificationTime = TestModificationTime;

            // Name
            Assert.Equal(InitialEntryName, entry.Name);
            entry.Name = ModifiedEntryName;

            // Uid
            Assert.Equal(DefaultUid, entry.Uid);
            entry.Uid = TestUid;
        }

        protected void VerifyCommonRegularFile(TarEntry regularFile, bool isFromWriter, bool isV7RegularFile = false)
        {
            Assert.NotNull(regularFile);
            TarEntryType entryType = isV7RegularFile ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            Assert.Equal(entryType, regularFile.EntryType);
            VerifyCommonProperties(regularFile);
            VerifyUnsupportedLinkProperty(regularFile);
            VerifyDataStream(regularFile, isFromWriter);
        }

        protected void VerifyCommonDirectory(TarEntry directory)
        {
            Assert.NotNull(directory);
            Assert.Equal(TarEntryType.Directory, directory.EntryType);
            VerifyCommonProperties(directory);
            VerifyUnsupportedLinkProperty(directory);
            VerifyUnsupportedDataStream(directory);
        }

        protected void VerifyCommonHardLink(TarEntry hardLink)
        {
            Assert.NotNull(hardLink);
            Assert.Equal(TarEntryType.HardLink, hardLink.EntryType);
            VerifyCommonProperties(hardLink);
            VerifyUnsupportedDataStream(hardLink);
            Assert.Equal(TestLinkName, hardLink.LinkName);
        }

        protected void VerifyCommonSymbolicLink(TarEntry symbolicLink)
        {
            Assert.NotNull(symbolicLink);
            Assert.Equal(TarEntryType.SymbolicLink, symbolicLink.EntryType);
            VerifyCommonProperties(symbolicLink);
            VerifyUnsupportedDataStream(symbolicLink);
            Assert.Equal(TestLinkName, symbolicLink.LinkName);
        }

        protected void VerifyCommonProperties(TarEntry entry)
        {
            Assert.Equal(TestGid, entry.Gid);
            Assert.Equal(TestMode, entry.Mode);
            Assert.Equal(TestModificationTime, entry.ModificationTime);
            Assert.Equal(ModifiedEntryName, entry.Name);
            Assert.Equal(TestUid, entry.Uid);
        }

        protected void VerifyUnsupportedLinkProperty(TarEntry entry)
        {
            Assert.Equal(DefaultLinkName, entry.LinkName);
            Assert.Throws<InvalidOperationException>(() => entry.LinkName = "NotSupported");
            Assert.Equal(DefaultLinkName, entry.LinkName);
        }

        protected void VerifyUnsupportedDataStream(TarEntry entry)
        {
            Assert.Null(entry.DataStream);
            using (MemoryStream dataStream = new MemoryStream())
            {
                Assert.Throws<InvalidOperationException>(() => entry.DataStream = dataStream);
            }
        }

        protected void VerifyDataStream(TarEntry entry, bool isFromWriter)
        {
            if (isFromWriter)
            {
                Assert.Null(entry.DataStream);
                entry.DataStream = new MemoryStream();
                // Verify it is not modified or wrapped in any way
                Assert.True(entry.DataStream.CanRead);
                Assert.True(entry.DataStream.CanWrite);

                entry.DataStream.WriteByte(1);
                Assert.Equal(1, entry.DataStream.Length);
                Assert.Equal(1, entry.Length);
                entry.DataStream.Dispose();
                Assert.Throws<ObjectDisposedException>(() => entry.DataStream.WriteByte(1));

                entry.DataStream = new MemoryStream();
                Assert.Equal(0, entry.DataStream.Length);
                entry.DataStream.WriteByte(1);
                Assert.Equal(1, entry.Length);
                Assert.Equal(1, entry.DataStream.Length);
                entry.DataStream.Seek(0, SeekOrigin.Begin);
            }
            else
            {
                // Reader should always set it
                Assert.NotNull(entry.DataStream);
                Assert.True(entry.DataStream.CanRead);
                Assert.False(entry.DataStream.CanWrite);
            }
        }
    }
}
