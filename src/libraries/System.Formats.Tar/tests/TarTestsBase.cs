// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected const string InitialEntryName = "InitialEntryName.ext";
        protected readonly string ModifiedEntryName = "ModifiedEntryName.ext";

        // Default values are what a TarEntry created with its constructor will set
        protected const UnixFileMode DefaultFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead; // 644 in octal, internally used as default
        private const UnixFileMode DefaultDirectoryMode = DefaultFileMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute; // 755 in octal, internally used as default
        protected const UnixFileMode DefaultWindowsMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.UserExecute; // Creating archives in Windows always sets the mode to 777
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

        protected readonly DateTimeOffset MinimumTime = new(2022, 1, 1, 1, 1, 1, TimeSpan.Zero);
        protected readonly DateTimeOffset TestModificationTime = new DateTimeOffset(2022, 2, 2, 2, 2, 2, TimeSpan.Zero);
        protected readonly DateTimeOffset TestAccessTime = new DateTimeOffset(2022, 3, 3, 3, 3, 3, TimeSpan.Zero);
        protected readonly DateTimeOffset TestChangeTime = new DateTimeOffset(2022, 4, 4, 4, 4, 4, TimeSpan.Zero);

        protected readonly string TestLinkName = "TestLinkName";
        protected const UnixFileMode TestMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.OtherRead | UnixFileMode.OtherWrite;

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
        protected const UnixFileMode AssetMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        protected const UnixFileMode AssetSpecialFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        protected const UnixFileMode AssetSymbolicLinkMode = UnixFileMode.OtherExecute | UnixFileMode.OtherWrite | UnixFileMode.OtherRead | UnixFileMode.GroupExecute | UnixFileMode.GroupWrite | UnixFileMode.GroupRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite | UnixFileMode.UserRead;
        protected const string AssetGName = "devdiv";
        protected const string AssetUName = "dotnet";
        protected const string AssetPaxGeaKey = "globexthdr.MyGlobalExtendedAttribute";
        protected const string AssetPaxGeaValue = "hello";

        protected const string PaxEaName = "path";
        protected const string PaxEaLinkName = "linkpath";
        protected const string PaxEaMode = "mode";
        protected const string PaxEaGName = "gname";
        protected const string PaxEaUName = "uname";
        protected const string PaxEaGid = "gid";
        protected const string PaxEaUid = "uid";
        protected const string PaxEaATime = "atime";
        protected const string PaxEaCTime = "ctime";
        protected const string PaxEaMTime = "mtime";
        protected const string PaxEaSize = "size";
        protected const string PaxEaDevMajor = "devmajor";
        protected const string PaxEaDevMinor = "devminor";

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
        protected static bool IsRemoteExecutorSupportedAndOnUnixAndSuperUser => RemoteExecutor.IsSupported && PlatformDetection.IsUnixAndSuperUser;

        protected static bool IsUnixButNotSuperUser => !PlatformDetection.IsWindows && !PlatformDetection.IsSuperUser;

        protected static bool IsNotLinuxBionic => !PlatformDetection.IsLinuxBionic;

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
        protected static MemoryStream GetTarMemoryStream(CompressionMethod compressionMethod, TestTarFormat format, string testCaseName) =>
            GetMemoryStream(GetTarFilePath(compressionMethod, format, testCaseName));

        protected static string GetStrangeTarFilePath(string testCaseName) =>
            Path.Join(Directory.GetCurrentDirectory(), "strange", testCaseName + ".tar");

        protected static MemoryStream GetStrangeTarMemoryStream(string testCaseName) =>
            GetMemoryStream(GetStrangeTarFilePath(testCaseName));

        private static MemoryStream GetMemoryStream(string path)
        {
            MemoryStream ms = new();
            using (FileStream fs = File.OpenRead(path))
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
            SetCommonProperties(directory, isDirectory: true);
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

        protected void SetCommonProperties(TarEntry entry, bool isDirectory = false)
        {
            // Length (Data is checked outside this method)
            Assert.Equal(0, entry.Length);

            // Checksum
            Assert.Equal(0, entry.Checksum);

            // Gid
            Assert.Equal(DefaultGid, entry.Gid);
            entry.Gid = TestGid;

            // Mode
            Assert.Equal(isDirectory ? DefaultDirectoryMode : DefaultFileMode, entry.Mode);
            entry.Mode = TestMode;

            // MTime: Verify the default value was approximately "now" by default
            DateTimeOffset approxNow = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromHours(6));
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

        protected Type GetTypeForFormat(TarEntryFormat expectedFormat)
        {
            return expectedFormat switch
            {
                TarEntryFormat.V7 => typeof(V7TarEntry),
                TarEntryFormat.Ustar => typeof(UstarTarEntry),
                TarEntryFormat.Pax => typeof(PaxTarEntry),
                TarEntryFormat.Gnu => typeof(GnuTarEntry),
                _ => throw new FormatException($"Unrecognized format: {expectedFormat}"),
            };
        }

        protected void CheckConversionType(TarEntry entry, TarEntryFormat expectedFormat)
        {
            Type expectedType = GetTypeForFormat(expectedFormat);
            Assert.Equal(expectedType, entry.GetType());
        }

        protected TarEntryType GetTarEntryTypeForTarEntryFormat(TarEntryType entryType, TarEntryFormat format)
        {
            if (format is TarEntryFormat.V7)
            {
                if (entryType is TarEntryType.RegularFile)
                {
                    return TarEntryType.V7RegularFile;
                }
            }
            else
            {
                if (entryType is TarEntryType.V7RegularFile)
                {
                    return TarEntryType.RegularFile;
                }
            }
            return entryType;
        }

        protected TarEntry InvokeTarEntryCreationConstructor(TarEntryFormat targetFormat, TarEntryType entryType, string entryName)
            => targetFormat switch
            {
                TarEntryFormat.V7 => new V7TarEntry(entryType, entryName),
                TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new FormatException($"Unexpected format: {targetFormat}")
            };

        public static IEnumerable<object[]> GetFormatsAndLinks()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                yield return new object[] { format, TarEntryType.SymbolicLink };
                yield return new object[] { format, TarEntryType.HardLink };
            }
        }
    }
}
