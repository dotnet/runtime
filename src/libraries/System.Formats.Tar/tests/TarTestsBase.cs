// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected static bool IsRemoteExecutorSupportedAndPrivilegedProcess => RemoteExecutor.IsSupported && PlatformDetection.IsPrivilegedProcess;

        protected const string InitialEntryName = "InitialEntryName.ext";
        protected readonly string ModifiedEntryName = "ModifiedEntryName.ext";

        // Default values are what a TarEntry created with its constructor will set
        protected const UnixFileMode DefaultFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead; // 644 in octal, internally used as default
        protected const UnixFileMode DefaultDirectoryMode = DefaultFileMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute; // 755 in octal, internally used as default

        protected readonly UnixFileMode CreateDirectoryDefaultMode; // Mode of directories created using Directory.CreateDirectory(string).
        protected readonly UnixFileMode UMask;

        // Mode assumed for files and directories on Windows.
        protected const UnixFileMode DefaultWindowsMode = DefaultFileMode | UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute; // 755 in octal, internally used as default

        // Permissions used by tests. User has all permissions to avoid permission errors.
        protected const UnixFileMode UserAll = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
        protected const UnixFileMode TestPermission1 = UserAll | UnixFileMode.GroupRead;
        protected const UnixFileMode TestPermission2 = UserAll | UnixFileMode.GroupExecute;
        protected const UnixFileMode TestPermission3 = UserAll | UnixFileMode.OtherRead;
        protected const UnixFileMode TestPermission4 = UserAll | UnixFileMode.OtherExecute;

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
        internal const char OneByteCharacter = 'a';
        internal const char TwoBytesCharacter = '\u00F6';
        internal const string FourBytesCharacter = "\uD83D\uDE12";
        internal const char Separator = '/';
        internal const int MaxPathComponent = 255;
        internal const long LegacyMaxFileSize = (1L << 33) - 1; // Max value of 11 octal digits = 2^33 - 1 or 8 Gb.

        private static readonly string[] V7TestCaseNames = new[]
        {
            "file",
            "file_hardlink",
            "file_symlink",
            "folder_file",
            "folder_file_utf8",
            "folder_subfolder_file",
            "foldersymlink_folder_subfolder_file",
            "many_small_files"
        };

        private static readonly string[] UstarTestCaseNames = new[]
        {
            "longpath_splitable_under255",
            "specialfiles" };

        private static readonly string[] PaxAndGnuTestCaseNames = new[]
        {
            "file_longsymlink",
            "longfilename_over100_under255",
            "longpath_over255"
        };

        private static readonly string[] GoLangTestCaseNames = new[]
        {
            "empty",
            "file-and-dir",
            "gnu-long-nul",
            "gnu-not-utf8",
            "gnu-utf8",
            "gnu",
            "hardlink",
            "nil-uid",
            "pax-bad-hdr-file",
            "pax-bad-mtime-file",
            "pax-global-records",
            "pax-nul-path",
            "pax-nul-xattrs",
            "pax-pos-size-file",
            "pax-records",
            "pax",
            "star",
            "trailing-slash",
            "ustar-file-devs",
            "ustar-file-reg",
            "ustar",
            "writer",
            "xattrs"
        };

        private static readonly string[] NodeTarTestCaseNames = new[]
        {
            "bad-cksum",
            "body-byte-counts",
            "dir",
            "emptypax",
            "file",
            "global-header",
            "links-invalid",
            "links-strip",
            "links",
            "long-paths",
            "long-pax",
            "next-file-has-long",
            "null-byte",
            "path-missing",
            "trailing-slash-corner-case",
            "utf8"
        };

        private static readonly string[] RsTarTestCaseNames = new[]
        {
            "7z_long_path",
            "directory",
            "duplicate_dirs",
            "empty_filename",
            "file_times",
            "link",
            "pax_size",
            "pax",
            "pax2",
            "reading_files",
            "simple_missing_last_header",
            "simple",
            "xattrs"
        };

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

        protected TarTestsBase()
        {
            CreateDirectoryDefaultMode = Directory.CreateDirectory(GetRandomDirPath()).UnixFileMode; // '0777 & ~umask'
            UMask = ~CreateDirectoryDefaultMode & (UnixFileMode)Convert.ToInt32("777",
            8);
        }

        protected static string GetTestCaseUnarchivedFolderPath(string testCaseName) =>
            Path.Join(Directory.GetCurrentDirectory(), "unarchived",
            testCaseName);

        protected static string GetTarFilePath(CompressionMethod compressionMethod, TestTarFormat format, string testCaseName)
            => GetTarFilePath(compressionMethod, format.ToString(), testCaseName);

        protected static string GetTarFilePath(CompressionMethod compressionMethod, string testFolderName, string testCaseName)
        {
            (string compressionMethodFolder, string fileExtension) = compressionMethod switch
            {
                CompressionMethod.Uncompressed => ("tar",
            ".tar"),
                CompressionMethod.GZip => ("targz",
            ".tar.gz"),
                _ => throw new InvalidOperationException($"Unexpected compression method: {compressionMethod}"),
            };

            return Path.Join(Directory.GetCurrentDirectory(), compressionMethodFolder, testFolderName, testCaseName + fileExtension);
        }

        // MemoryStream containing the copied contents of the specified file. Meant for reading and writing.
        protected static MemoryStream GetTarMemoryStream(CompressionMethod compressionMethod, TestTarFormat format, string testCaseName) =>
            GetTarMemoryStream(compressionMethod, format.ToString(), testCaseName);

        protected static MemoryStream GetTarMemoryStream(CompressionMethod compressionMethod, string testFolderName, string testCaseName) =>
            GetMemoryStream(GetTarFilePath(compressionMethod, testFolderName, testCaseName));

        protected static string GetStrangeTarFilePath(string testCaseName) =>
            Path.Join(Directory.GetCurrentDirectory(), "strange",
            testCaseName + ".tar");

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
            Assert.Throws<ArgumentNullException>(() => hardLink.LinkName = null);
            Assert.Throws<ArgumentException>(() => hardLink.LinkName = string.Empty);
            hardLink.LinkName = TestLinkName;
        }

        protected void SetCommonSymbolicLink(TarEntry symbolicLink)
        {
            Assert.NotNull(symbolicLink);
            Assert.Equal(TarEntryType.SymbolicLink, symbolicLink.EntryType);
            SetCommonProperties(symbolicLink);

            // LinkName
            Assert.Equal(DefaultLinkName, symbolicLink.LinkName);
            Assert.Throws<ArgumentNullException>(() => symbolicLink.LinkName = null);
            Assert.Throws<ArgumentException>(() => symbolicLink.LinkName = string.Empty);
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

                using (MemoryStream ms = new MemoryStream())
                using (WrappedStream ws = new WrappedStream(ms, canRead: false, canWrite: true, canSeek: true))
                {
                    Assert.Throws<ArgumentException>(() => entry.DataStream = ws);
                }

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
                _ => throw new InvalidDataException($"Unrecognized format: {expectedFormat}"),
            };
        }

        protected void CheckConversionType(TarEntry entry, TarEntryFormat expectedFormat)
        {
            Type expectedType = GetTypeForFormat(expectedFormat);
            Assert.Equal(expectedType, entry.GetType());
        }

        protected static TarEntryType GetTarEntryTypeForTarEntryFormat(TarEntryType entryType, TarEntryFormat format)
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

        protected static TarEntry InvokeTarEntryCreationConstructor(TarEntryFormat targetFormat, TarEntryType entryType, string entryName)
            => targetFormat switch
            {
                TarEntryFormat.V7 => new V7TarEntry(entryType, entryName),
                TarEntryFormat.Ustar => new UstarTarEntry(entryType, entryName),
                TarEntryFormat.Pax => new PaxTarEntry(entryType, entryName),
                TarEntryFormat.Gnu => new GnuTarEntry(entryType, entryName),
                _ => throw new InvalidDataException($"Unexpected format: {targetFormat}")
            };

        public static IEnumerable<object[]> GetTestTarFormats()
        {
            foreach (TestTarFormat testFormat in Enum.GetValues<TestTarFormat>())
            {
                yield return new object[] { testFormat };
            }
        }

        public static IEnumerable<object[]> GetFormatsAndLinks()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.V7, TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                yield return new object[] { format, TarEntryType.SymbolicLink };
                yield return new object[] { format, TarEntryType.HardLink };
            }
        }

        public static IEnumerable<object[]> GetFormatsAndFiles()
        {
            foreach (TarEntryType entryType in new[] { TarEntryType.V7RegularFile, TarEntryType.Directory })
            {
                yield return new object[] { TarEntryFormat.V7, entryType };
            }
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu })
            {
                foreach (TarEntryType entryType in new[] { TarEntryType.RegularFile, TarEntryType.Directory })
                {
                    yield return new object[] { format, entryType };
                }
            }
        }

        protected static void SetUnixFileMode(string path, UnixFileMode mode)
        {
            if (!PlatformDetection.IsWindows)
            {
                File.SetUnixFileMode(path, mode);
            }
        }

        protected static void AssertEntryModeFromFileSystemEquals(TarEntry entry, UnixFileMode fileMode)
        {
            if (PlatformDetection.IsWindows)
            {
                // Windows files don't have a mode. Set the expected value.
                fileMode = DefaultWindowsMode;
            }
            Assert.Equal(fileMode, entry.Mode);
        }

        protected void AssertFileModeEquals(string path, UnixFileMode archiveMode)
        {
            if (!PlatformDetection.IsWindows)
            {
                UnixFileMode expectedMode = archiveMode & ~UMask;

                UnixFileMode actualMode = File.GetUnixFileMode(path);
                // Ignore SetGroup: it may have been added to preserve group ownership.
                if ((expectedMode & UnixFileMode.SetGroup) == 0)
                {
                    actualMode &= ~UnixFileMode.SetGroup;
                }

                Assert.Equal(expectedMode, actualMode);
            }
        }

        protected (string, string, TarEntry) Prepare_Extract(TempDirectory root, TarEntryFormat format, TarEntryType entryType)
        {
            string entryName = entryType.ToString();
            string destination = Path.Join(root.Path, entryName);

            TarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryName);
            Assert.NotNull(entry);
            entry.Mode = TestPermission1;

            return (entryName, destination, entry);
        }

        protected void Verify_Extract(string destination, TarEntry entry, TarEntryType entryType)
        {
            if (entryType is TarEntryType.RegularFile or TarEntryType.V7RegularFile)
            {
                Assert.True(File.Exists(destination));
            }
            else if (entryType is TarEntryType.Directory)
            {
                Assert.True(Directory.Exists(destination));
            }
            else
            {
                Assert.Fail("Unchecked entry type.");
            }

            AssertFileModeEquals(destination, TestPermission1);
        }

        public static IEnumerable<object[]> GetNodeTarTestCaseNames()
        {
            foreach (string name in NodeTarTestCaseNames)
            {
                yield return new object[] { name };
            }
        }

        public static IEnumerable<object[]> GetGoLangTarTestCaseNames()
        {
            foreach (string name in GoLangTestCaseNames)
            {
                yield return new object[] { name };
            }
        }

        public static IEnumerable<object[]> GetRsTarTestCaseNames()
        {
            foreach (string name in RsTarTestCaseNames)
            {
                yield return new object[] { name };
            }
        }

        public static IEnumerable<object[]> GetV7TestCaseNames()
        {
            foreach (string name in V7TestCaseNames)
            {
                yield return new object[] { name };
            }
        }

        public static IEnumerable<object[]> GetUstarTestCaseNames()
        {
            foreach (string name in UstarTestCaseNames.Concat(V7TestCaseNames))
            {
                yield return new object[] { name };
            }
        }

        public static IEnumerable<object[]> GetPaxAndGnuTestCaseNames()
        {
            foreach (string name in UstarTestCaseNames.Concat(V7TestCaseNames).Concat(PaxAndGnuTestCaseNames))
            {
                yield return new object[] { name };
            }
        }

        private static List<string> GetPrefixes()
        {
            List<string> prefixes = new() { "", "/a/", "./", "../" };

            if (OperatingSystem.IsWindows())
                prefixes.Add("C:/");

            return prefixes;
        }

        internal static IEnumerable<string> GetNamesPrefixedTestData(NameCapabilities max)
        {
            Assert.True(Enum.IsDefined(max));
            List<string> prefixes = GetPrefixes();

            foreach (string prefix in prefixes)
            {
                // prefix + name of length 100
                int nameLength = 100 - prefix.Length;
                yield return prefix + Repeat(OneByteCharacter, nameLength);
                yield return prefix + Repeat(OneByteCharacter, nameLength - 2) + TwoBytesCharacter;
                yield return prefix + Repeat(OneByteCharacter, nameLength - 4) + FourBytesCharacter;

                // prefix alone
                if (prefix != string.Empty)
                    yield return prefix;
            }

            if (max == NameCapabilities.Name)
                yield break;

            // maxed out name.
            foreach (string prefix in prefixes)
            {
                yield return prefix + Repeat(OneByteCharacter, 100);
                yield return prefix + Repeat(OneByteCharacter, 100 - 2) + TwoBytesCharacter;
                yield return prefix + Repeat(OneByteCharacter, 100 - 4) + FourBytesCharacter;
            }

            // maxed out prefix and name.
            foreach (string prefix in prefixes)
            {
                int directoryLength = 155 - prefix.Length;
                yield return prefix + Repeat(OneByteCharacter, directoryLength) + Separator + Repeat(OneByteCharacter, 100);
                yield return prefix + Repeat(OneByteCharacter, directoryLength - 2) + TwoBytesCharacter + Separator + Repeat(OneByteCharacter, 100);
                yield return prefix + Repeat(OneByteCharacter, directoryLength - 4) + FourBytesCharacter + Separator + Repeat(OneByteCharacter, 100);
            }

            if (max == NameCapabilities.NameAndPrefix)
                yield break;

            foreach (string prefix in prefixes)
            {
                int directoryLength = MaxPathComponent - prefix.Length;
                yield return prefix + Repeat(OneByteCharacter, directoryLength) + Separator + Repeat(OneByteCharacter, MaxPathComponent);
                yield return prefix + Repeat(OneByteCharacter, directoryLength - 2) + TwoBytesCharacter + Separator + Repeat(OneByteCharacter, MaxPathComponent);
                yield return prefix + Repeat(OneByteCharacter, directoryLength - 4) + FourBytesCharacter + Separator + Repeat(OneByteCharacter, MaxPathComponent);
            }
        }

        internal static IEnumerable<string> GetNamesNonAsciiTestData(NameCapabilities max)
        {
            Assert.True(Enum.IsDefined(max));

            yield return Repeat(OneByteCharacter, 100);
            yield return Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(OneByteCharacter, 2) + Repeat(TwoBytesCharacter, 49);

            yield return Repeat(FourBytesCharacter, 100 / 4);
            yield return Repeat(OneByteCharacter, 4) + Repeat(FourBytesCharacter, 24);

            if (max == NameCapabilities.Name)
                yield break;

            // prefix + name
            // this is 256 but is supported because prefix is not required to end in separator.
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(OneByteCharacter, 100);

            // non-ascii prefix + name 
            yield return Repeat(TwoBytesCharacter, 155 / 2) + Separator + Repeat(OneByteCharacter, 100);
            yield return Repeat(FourBytesCharacter, 155 / 4) + Separator + Repeat(OneByteCharacter, 100);

            // prefix + non-ascii name
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(OneByteCharacter, 155) + Separator + Repeat(FourBytesCharacter, 100 / 4);

            // non-ascii prefix + non-ascii name
            yield return Repeat(TwoBytesCharacter, 155 / 2) + Separator + Repeat(TwoBytesCharacter, 100 / 2);
            yield return Repeat(FourBytesCharacter, 155 / 4) + Separator + Repeat(FourBytesCharacter, 100 / 4);

            if (max == NameCapabilities.NameAndPrefix)
                yield break;

            // Pax and Gnu support unlimited paths.
            yield return Repeat(OneByteCharacter, MaxPathComponent);
            yield return Repeat(TwoBytesCharacter, MaxPathComponent / 2);
            yield return Repeat(FourBytesCharacter, MaxPathComponent / 4);

            yield return Repeat(OneByteCharacter, MaxPathComponent) + Separator + Repeat(OneByteCharacter, MaxPathComponent);
            yield return Repeat(TwoBytesCharacter, MaxPathComponent / 2) + Separator + Repeat(TwoBytesCharacter, MaxPathComponent / 2);
            yield return Repeat(FourBytesCharacter, MaxPathComponent / 4) + Separator + Repeat(FourBytesCharacter, MaxPathComponent / 4);
        }

        internal static IEnumerable<string> GetTooLongNamesTestData(NameCapabilities max)
        {
            Assert.True(max is NameCapabilities.Name or NameCapabilities.NameAndPrefix);

            // root directory can't be saved as prefix
            yield return "/" + Repeat(OneByteCharacter, 100);

            List<string> prefixes = GetPrefixes();

            // 1. non-ascii last character doesn't fit in name.
            foreach (string prefix in prefixes)
            {
                // 1.1. last character doesn't fit fully.
                yield return prefix + Repeat(OneByteCharacter, 100 + 1);
                yield return prefix + Repeat(OneByteCharacter, 100 - 2) + Repeat(TwoBytesCharacter, 2);
                yield return prefix + Repeat(OneByteCharacter, 100 - 4) + Repeat(FourBytesCharacter, 2);

                // 1.2. last character doesn't fit by one byte.
                yield return prefix + Repeat(OneByteCharacter, 100 - 2 + 1) + Repeat(TwoBytesCharacter, 1);
                yield return prefix + Repeat(OneByteCharacter, 100 - 4 + 1) + Repeat(FourBytesCharacter, 1);
            }

            // 2. non-ascii last character doesn't fit in prefix.
            string maxedOutName = Repeat(OneByteCharacter, 100);

            // 2.1. last char doesn't fit fully.
            yield return Repeat(OneByteCharacter, 155 + 1) + Separator + maxedOutName;
            yield return Repeat(OneByteCharacter, 155 - 2) + Repeat(TwoBytesCharacter, 2) + Separator + maxedOutName;
            yield return Repeat(OneByteCharacter, 155 - 4) + Repeat(FourBytesCharacter, 2) + Separator + maxedOutName;

            // 2.2 last char doesn't fit by one byte.
            yield return Repeat(OneByteCharacter, 155 - 2 + 1) + TwoBytesCharacter + Separator + maxedOutName;
            yield return Repeat(OneByteCharacter, 155 - 4 + 1) + FourBytesCharacter + Separator + maxedOutName;

            if (max is NameCapabilities.NameAndPrefix)
                yield break;

            // Next cases only apply for V7 which only allows 100 length names.
            foreach (string prefix in prefixes)
            {
                if (prefix.Length == 0)
                    continue;

                yield return prefix + Repeat(OneByteCharacter, 100);
                yield return prefix + Repeat(TwoBytesCharacter, 100 / 2);
                yield return prefix + Repeat(FourBytesCharacter, 100 / 4);
            }
        }

        internal static string Repeat(char c, int count)
        {
            return new string(c, count);
        }

        internal static string Repeat(string c, int count)
        {
            return string.Concat(Enumerable.Repeat(c, count));
        }

        internal enum NameCapabilities
        {
            Name,
            NameAndPrefix,
            Unlimited
        }

        internal static void WriteTarArchiveWithOneEntry(Stream s, TarEntryFormat entryFormat, TarEntryType entryType)
        {
            using TarWriter writer = new(s, leaveOpen: true);

            TarEntry entry = InvokeTarEntryCreationConstructor(entryFormat, entryType, "foo");
            if (entryType == TarEntryType.SymbolicLink)
            {
                entry.LinkName = "bar";
            }

            writer.WriteEntry(entry);
        }
    }
}
