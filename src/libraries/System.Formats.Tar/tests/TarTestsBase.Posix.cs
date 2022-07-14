// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public abstract partial class TarTestsBase : FileCleanupTestBase
    {
        protected void SetPosixProperties(PosixTarEntry entry)
        {
            Assert.Equal(DefaultGName, entry.GroupName);
            entry.GroupName = TestGName;

            Assert.Equal(DefaultUName, entry.UserName);
            entry.UserName = TestUName;
        }

        private void SetBlockDeviceProperties(PosixTarEntry device)
        {
            Assert.NotNull(device);
            Assert.Equal(TarEntryType.BlockDevice, device.EntryType);
            SetCommonProperties(device);
            SetPosixProperties(device);

            // DeviceMajor
            Assert.Equal(DefaultDeviceMajor, device.DeviceMajor);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMajor = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMajor = 2097152);
            device.DeviceMajor = TestBlockDeviceMajor;

            // DeviceMinor
            Assert.Equal(DefaultDeviceMinor, device.DeviceMinor);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMinor = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMinor = 2097152);
            device.DeviceMinor = TestBlockDeviceMinor;
        }

        private void SetCharacterDeviceProperties(PosixTarEntry device)
        {
            Assert.NotNull(device);
            Assert.Equal(TarEntryType.CharacterDevice, device.EntryType);
            SetCommonProperties(device);
            SetPosixProperties(device);

            // DeviceMajor
            Assert.Equal(DefaultDeviceMajor, device.DeviceMajor);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMajor = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMajor = 2097152);
            device.DeviceMajor = TestCharacterDeviceMajor;

            // DeviceMinor
            Assert.Equal(DefaultDeviceMinor, device.DeviceMinor);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMinor = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => device.DeviceMinor = 2097152);
            device.DeviceMinor = TestCharacterDeviceMinor;
        }

        private void SetFifoProperties(PosixTarEntry fifo)
        {
            Assert.NotNull(fifo);
            Assert.Equal(TarEntryType.Fifo, fifo.EntryType);
            SetCommonProperties(fifo);
            SetPosixProperties(fifo);
        }

        protected void VerifyPosixProperties(PosixTarEntry entry)
        {
            entry.GroupName = TestGName;
            Assert.Equal(TestGName, entry.GroupName);

            entry.UserName = TestUName;
            Assert.Equal(TestUName, entry.UserName);
        }

        protected void VerifyPosixRegularFile(PosixTarEntry regularFile, bool isWritable)
        {
            VerifyCommonRegularFile(regularFile, isWritable);
            VerifyUnsupportedDeviceProperties(regularFile);
        }

        protected void VerifyPosixDirectory(PosixTarEntry directory)
        {
            VerifyCommonDirectory(directory);
            VerifyUnsupportedDeviceProperties(directory);
        }

        protected void VerifyPosixHardLink(PosixTarEntry hardLink)
        {
            VerifyCommonHardLink(hardLink);
            VerifyUnsupportedDeviceProperties(hardLink);
        }

        protected void VerifyPosixSymbolicLink(PosixTarEntry symbolicLink)
        {
            VerifyCommonSymbolicLink(symbolicLink);
            VerifyUnsupportedDeviceProperties(symbolicLink);
        }

        protected void VerifyPosixCharacterDevice(PosixTarEntry device)
        {
            Assert.NotNull(device);
            Assert.Equal(TarEntryType.CharacterDevice, device.EntryType);
            VerifyCommonProperties(device);
            VerifyUnsupportedLinkProperty(device);
            VerifyUnsupportedDataStream(device);

            Assert.Equal(TestCharacterDeviceMajor, device.DeviceMajor);
            Assert.Equal(TestCharacterDeviceMinor, device.DeviceMinor);
        }

        protected void VerifyPosixBlockDevice(PosixTarEntry device)
        {
            Assert.NotNull(device);
            Assert.Equal(TarEntryType.BlockDevice, device.EntryType);
            VerifyCommonProperties(device);
            VerifyUnsupportedLinkProperty(device);
            VerifyUnsupportedDataStream(device);

            Assert.Equal(TestBlockDeviceMajor, device.DeviceMajor);
            Assert.Equal(TestBlockDeviceMinor, device.DeviceMinor);
        }

        protected void VerifyPosixFifo(PosixTarEntry fifo)
        {
            Assert.NotNull(fifo);
            Assert.Equal(TarEntryType.Fifo, fifo.EntryType);
            VerifyCommonProperties(fifo);
            VerifyPosixProperties(fifo);
            VerifyUnsupportedDeviceProperties(fifo);
            VerifyUnsupportedLinkProperty(fifo);
            VerifyUnsupportedDataStream(fifo);
        }

        protected void VerifyUnsupportedDeviceProperties(PosixTarEntry entry)
        {
            Assert.True(entry.EntryType is not TarEntryType.CharacterDevice and not TarEntryType.BlockDevice);
            Assert.Equal(0, entry.DeviceMajor);
            Assert.Throws<InvalidOperationException>(() => entry.DeviceMajor = 5);
            Assert.Equal(0, entry.DeviceMajor); // No change

            Assert.Equal(0, entry.DeviceMinor);
            Assert.Throws<InvalidOperationException>(() => entry.DeviceMinor = 5);
            Assert.Equal(0, entry.DeviceMinor); // No change
        }
    }
}
