// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public class TarReader_File_Tests_Base : TarTestsBase
    {
        protected void VerifyRegularFileEntry(TarEntry file, TarFormat format, string expectedFileName, string expectedContents)
        {
            Assert.NotNull(file);
            Assert.Equal(format, file.Format);

            Assert.True(file.Checksum > 0);
            Assert.NotNull(file.DataStream);
            Assert.True(file.DataStream.Length > 0);
            Assert.True(file.DataStream.CanRead);
            Assert.True(file.DataStream.CanSeek);
            file.DataStream.Seek(0, SeekOrigin.Begin);
            using (StreamReader reader = new StreamReader(file.DataStream, leaveOpen: true))
            {
                string contents = reader.ReadLine();
                Assert.Equal(expectedContents, contents);
            }

            TarEntryType expectedEntryType = format == TarFormat.V7 ? TarEntryType.V7RegularFile : TarEntryType.RegularFile;
            Assert.Equal(expectedEntryType, file.EntryType);

            Assert.Equal(AssetGid, file.Gid);
            Assert.Equal(file.Length, file.DataStream.Length);
            Assert.Equal(DefaultLinkName, file.LinkName);
            Assert.Equal(AssetMode, file.Mode);
            Assert.True(file.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, file.Name);
            Assert.Equal(AssetUid, file.Uid);

            if (file is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);

                if (posix is PaxTarEntry pax)
                {
                    VerifyExtendedAttributes(pax);
                }
                else if (posix is GnuTarEntry gnu)
                {
                    VerifyGnuFields(gnu);
                }
            }
        }

        protected void VerifySymbolicLinkEntry(TarEntry symbolicLink, TarFormat format, string expectedFileName, string expectedTargetName)
        {
            Assert.NotNull(symbolicLink);
            Assert.Equal(format, symbolicLink.Format);

            Assert.True(symbolicLink.Checksum > 0);
            Assert.Null(symbolicLink.DataStream);

            Assert.Equal(TarEntryType.SymbolicLink, symbolicLink.EntryType);

            Assert.Equal(AssetGid, symbolicLink.Gid);
            Assert.Equal(0, symbolicLink.Length);
            Assert.Equal(expectedTargetName, symbolicLink.LinkName);
            Assert.Equal(AssetSymbolicLinkMode, symbolicLink.Mode);
            Assert.True(symbolicLink.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, symbolicLink.Name);
            Assert.Equal(AssetUid, symbolicLink.Uid);

            if (symbolicLink is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);
            }

            if (symbolicLink is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (symbolicLink is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyDirectoryEntry(TarEntry directory, TarFormat format, string expectedFileName)
        {
            Assert.NotNull(directory);
            Assert.Equal(format, directory.Format);

            Assert.True(directory.Checksum > 0);
            Assert.Null(directory.DataStream);

            Assert.Equal(TarEntryType.Directory, directory.EntryType);

            Assert.Equal(AssetGid, directory.Gid);
            Assert.Equal(0, directory.Length);
            Assert.Equal(DefaultLinkName, directory.LinkName);
            Assert.Equal(AssetMode, directory.Mode);
            Assert.True(directory.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, directory.Name);
            Assert.Equal(AssetUid, directory.Uid);

            if (directory is PosixTarEntry posix)
            {
                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                Assert.Equal(AssetGName, posix.GroupName);
                Assert.Equal(AssetUName, posix.UserName);
            }

            if (directory is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (directory is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyBlockDeviceEntry(PosixTarEntry blockDevice, TarFormat format, string expectedFileName)
        {
            Assert.NotNull(blockDevice);
            Assert.Equal(TarEntryType.BlockDevice, blockDevice.EntryType);
            Assert.Equal(format, blockDevice.Format);

            Assert.True(blockDevice.Checksum > 0);
            Assert.Null(blockDevice.DataStream);

            Assert.Equal(AssetGid, blockDevice.Gid);
            Assert.Equal(0, blockDevice.Length);
            Assert.Equal(DefaultLinkName, blockDevice.LinkName);
            Assert.Equal(AssetSpecialFileMode, blockDevice.Mode);
            Assert.True(blockDevice.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, blockDevice.Name);
            Assert.Equal(AssetUid, blockDevice.Uid);

            // TODO: Figure out why the numbers don't match https://github.com/dotnet/runtime/issues/68230
            // Assert.Equal(AssetBlockDeviceMajor, blockDevice.DeviceMajor);
            // Assert.Equal(AssetBlockDeviceMinor, blockDevice.DeviceMinor);
            // Remove these two temporary checks when the above is fixed
            Assert.True(blockDevice.DeviceMajor > 0);
            Assert.True(blockDevice.DeviceMinor > 0);
            Assert.Equal(AssetGName, blockDevice.GroupName);
            Assert.Equal(AssetUName, blockDevice.UserName);

            if (blockDevice is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (blockDevice is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyCharacterDeviceEntry(PosixTarEntry characterDevice, TarFormat format, string expectedFileName)
        {
            Assert.NotNull(characterDevice);
            Assert.Equal(TarEntryType.CharacterDevice, characterDevice.EntryType);
            Assert.Equal(format, characterDevice.Format);

            Assert.True(characterDevice.Checksum > 0);
            Assert.Null(characterDevice.DataStream);

            Assert.Equal(AssetGid, characterDevice.Gid);
            Assert.Equal(0, characterDevice.Length);
            Assert.Equal(DefaultLinkName, characterDevice.LinkName);
            Assert.Equal(AssetSpecialFileMode, characterDevice.Mode);
            Assert.True(characterDevice.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, characterDevice.Name);
            Assert.Equal(AssetUid, characterDevice.Uid);

            // TODO: Figure out why the numbers don't match https://github.com/dotnet/runtime/issues/68230
            //Assert.Equal(AssetBlockDeviceMajor, characterDevice.DeviceMajor);
            //Assert.Equal(AssetBlockDeviceMinor, characterDevice.DeviceMinor);
            // Remove these two temporary checks when the above is fixed
            Assert.True(characterDevice.DeviceMajor > 0);
            Assert.True(characterDevice.DeviceMinor > 0);
            Assert.Equal(AssetGName, characterDevice.GroupName);
            Assert.Equal(AssetUName, characterDevice.UserName);

            if (characterDevice is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (characterDevice is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        protected void VerifyFifoEntry(PosixTarEntry fifo, TarFormat format, string expectedFileName)
        {
            Assert.NotNull(fifo);
            Assert.Equal(format, fifo.Format);

            Assert.True(fifo.Checksum > 0);
            Assert.Null(fifo.DataStream);

            Assert.Equal(TarEntryType.Fifo, fifo.EntryType);

            Assert.Equal(AssetGid, fifo.Gid);
            Assert.Equal(0, fifo.Length);
            Assert.Equal(DefaultLinkName, fifo.LinkName);
            Assert.Equal(AssetSpecialFileMode, fifo.Mode);
            Assert.True(fifo.ModificationTime > DateTimeOffset.UnixEpoch);
            Assert.Equal(expectedFileName, fifo.Name);
            Assert.Equal(AssetUid, fifo.Uid);

            Assert.Equal(DefaultDeviceMajor, fifo.DeviceMajor);
            Assert.Equal(DefaultDeviceMinor, fifo.DeviceMinor);
            Assert.Equal(AssetGName, fifo.GroupName);
            Assert.Equal(AssetUName, fifo.UserName);

            if (fifo is PaxTarEntry pax)
            {
                VerifyExtendedAttributes(pax);
            }
            else if (fifo is GnuTarEntry gnu)
            {
                VerifyGnuFields(gnu);
            }
        }

        private void VerifyExtendedAttributes(PaxTarEntry pax)
        {
            Assert.NotNull(pax.ExtendedAttributes);
            Assert.Equal(TarFormat.Pax, pax.Format);
            Assert.True(pax.ExtendedAttributes.Count() >= 3); // Expect to at least collect mtime, ctime and atime

            Assert.Contains("mtime", pax.ExtendedAttributes);
            Assert.Contains("atime", pax.ExtendedAttributes);
            Assert.Contains("ctime", pax.ExtendedAttributes);

            Assert.True(double.TryParse(pax.ExtendedAttributes["mtime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double mtimeSecondsSinceEpoch));
            Assert.True(mtimeSecondsSinceEpoch > 0);

            Assert.True(double.TryParse(pax.ExtendedAttributes["atime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double atimeSecondsSinceEpoch));
            Assert.True(atimeSecondsSinceEpoch > 0);

            Assert.True(double.TryParse(pax.ExtendedAttributes["ctime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double ctimeSecondsSinceEpoch));
            Assert.True(ctimeSecondsSinceEpoch > 0);
        }

        private void VerifyGnuFields(GnuTarEntry gnu)
        {
            Assert.Equal(TarFormat.Gnu, gnu.Format);
            Assert.True(gnu.AccessTime >= DateTimeOffset.UnixEpoch);
            Assert.True(gnu.ChangeTime >= DateTimeOffset.UnixEpoch);
        }
    }
}