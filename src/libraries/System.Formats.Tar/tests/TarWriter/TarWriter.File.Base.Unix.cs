// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_File_Base : TarTestsBase
    {
        protected void VerifyPlatformSpecificMetadata(string filePath, TarEntry entry)
        {
            Interop.Sys.FileStatus status = default;
            status.Mode = default;
            status.Dev = default;
            Interop.CheckIo(Interop.Sys.LStat(filePath, out status));

            Assert.Equal((int)status.Uid, entry.Uid);
            Assert.Equal((int)status.Gid, entry.Gid);

            if (entry is PosixTarEntry posix)
            {
                string gname = Interop.Sys.GetGroupName(status.Gid);
                string uname = Interop.Sys.GetUserNameFromPasswd(status.Uid);

                Assert.Equal(gname, posix.GroupName);
                Assert.Equal(uname, posix.UserName);

                if (entry.EntryType is not TarEntryType.BlockDevice and not TarEntryType.CharacterDevice)
                {
                    Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                    Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
                }
            }

            if (entry.EntryType is not TarEntryType.Directory)
            {
                UnixFileMode expectedMode = (UnixFileMode)(status.Mode & 4095); // First 12 bits

                Assert.Equal(expectedMode, entry.Mode);
                Assert.True(entry.ModificationTime > DateTimeOffset.UnixEpoch);

                if (entry is PaxTarEntry pax)
                {
                    VerifyExtendedAttributeTimestamps(pax);
                }

                if (entry is GnuTarEntry gnu)
                {
                    VerifyGnuTimestamps(gnu);
                }
            }
        }
    }
}
