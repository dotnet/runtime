// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarEntry_ExtractToFile_Tests_Unix : TarTestsBase
    {
        public static IEnumerable<object[]> GetFormatsAndSpecialFiles()
        {
            foreach (TarEntryFormat format in new[] { TarEntryFormat.Ustar, TarEntryFormat.Pax, TarEntryFormat.Gnu }) // V7 does not support special files
            {
                foreach (TarEntryType entryType in new[] { TarEntryType.BlockDevice, TarEntryType.CharacterDevice, TarEntryType.Fifo })
                {
                    yield return new object[] { format, entryType };
                }
            }
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [MemberData(nameof(GetFormatsAndSpecialFiles))]
        public void Extract_SpecialFiles(TarEntryFormat format, TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();

            (string entryName, string destination, PosixTarEntry entry) = Prepare_Extract_SpecialFiles(root, format, entryType);

            entry.ExtractToFile(destination, overwrite: true);

            Verify_Extract_SpecialFiles(destination, entry, entryType);
        }

        [ConditionalTheory(nameof(IsRemoteExecutorSupportedAndPrivilegedProcess))]
        [MemberData(nameof(GetFormatsAndSpecialFiles))]
        public async Task Extract_SpecialFiles_Async(TarEntryFormat format, TarEntryType entryType)
        {
            using TempDirectory root = new TempDirectory();

            (string entryName, string destination, PosixTarEntry entry) = Prepare_Extract_SpecialFiles(root, format, entryType);

            await entry.ExtractToFileAsync(destination, overwrite: true);

            Verify_Extract_SpecialFiles(destination, entry, entryType);
        }

        private (string, string, PosixTarEntry) Prepare_Extract_SpecialFiles(TempDirectory root, TarEntryFormat format, TarEntryType entryType)
        {
            string entryName = entryType.ToString();
            string destination = Path.Join(root.Path, entryName);

            PosixTarEntry entry = InvokeTarEntryCreationConstructor(format, entryType, entryName) as PosixTarEntry;
            Assert.NotNull(entry);

            if (entryType is TarEntryType.BlockDevice)
            {
                entry.DeviceMajor = TestBlockDeviceMajor;
                entry.DeviceMinor = TestBlockDeviceMinor;
            }
            else if (entryType is TarEntryType.CharacterDevice)
            {
                entry.DeviceMajor = TestCharacterDeviceMajor;
                entry.DeviceMinor = TestCharacterDeviceMinor;
            }
            entry.Mode = TestPermission1;

            return (entryName, destination, entry);
        }

        private void Verify_Extract_SpecialFiles(string destination, PosixTarEntry entry, TarEntryType entryType)
        {
            Assert.True(File.Exists(destination));

            Interop.Sys.FileStatus status = default;
            status.Mode = default;
            status.Dev = default;
            Interop.CheckIo(Interop.Sys.LStat(destination, out status));
            int fileType = status.Mode & Interop.Sys.FileTypes.S_IFMT;

            if (entryType is TarEntryType.BlockDevice)
            {
                Assert.Equal(Interop.Sys.FileTypes.S_IFBLK, fileType);
            }
            else if (entryType is TarEntryType.CharacterDevice)
            {
                Assert.Equal(Interop.Sys.FileTypes.S_IFCHR, fileType);
            }
            else if (entryType is TarEntryType.Fifo)
            {
                Assert.Equal(Interop.Sys.FileTypes.S_IFIFO, fileType);
            }

            if (entryType is TarEntryType.BlockDevice or TarEntryType.CharacterDevice)
            {
                uint major;
                uint minor;
                unsafe
                {
                    Interop.Sys.GetDeviceIdentifiers((ulong)status.RDev, &major, &minor);
                }

                Assert.Equal((int)major, entry.DeviceMajor);
                Assert.Equal((int)minor, entry.DeviceMinor);
            }

            AssertFileModeEquals(destination, TestPermission1);
        }

        [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows))]
        [InlineData(TarEntryFormat.V7)]
        [InlineData(TarEntryFormat.Ustar)]
        [InlineData(TarEntryFormat.Pax)]
        [InlineData(TarEntryFormat.Gnu)]
        public void Archive_And_Extract_Executable_PreservesExecutableBit(TarEntryFormat format)
        {
            using TempDirectory root = new TempDirectory();

            string executableFileName = "testexecutable.sh";
            string executableFilePath = Path.Join(root.Path, executableFileName);

            File.WriteAllText(executableFilePath, "#!/bin/bash\necho 'test'\n");

            UnixFileMode executableMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                          UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                                          UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            File.SetUnixFileMode(executableFilePath, executableMode);

            using MemoryStream archiveStream = new MemoryStream();
            using (TarWriter writer = new TarWriter(archiveStream, format, leaveOpen: true))
            {
                writer.WriteEntry(executableFilePath, executableFileName);
            }

            string extractedFileName = "extracted_executable.sh";
            string extractedFilePath = Path.Join(root.Path, extractedFileName);

            archiveStream.Seek(0, SeekOrigin.Begin);
            using (TarReader reader = new TarReader(archiveStream))
            {
                TarEntry entry = reader.GetNextEntry();
                Assert.NotNull(entry);
                entry.ExtractToFile(extractedFilePath, overwrite: false);
            }

            UnixFileMode extractedMode = File.GetUnixFileMode(extractedFilePath);
            UnixFileMode executeBitsMask = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;
            UnixFileMode expectedExecutableBits = executableMode & ~UMask & executeBitsMask;
            UnixFileMode actualExecutableBits = extractedMode & executeBitsMask;
            Assert.Equal(expectedExecutableBits, actualExecutableBits);
        }
    }
}
