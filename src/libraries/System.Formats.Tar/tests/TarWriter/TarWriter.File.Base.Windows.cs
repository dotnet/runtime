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
            Assert.True(entry.ModificationTime > DateTimeOffset.UnixEpoch);

            UnixFileMode expectedMode = DefaultWindowsMode;
            Assert.Equal(expectedMode, entry.Mode);

            Assert.Equal(DefaultUid, entry.Uid);
            Assert.Equal(DefaultGid, entry.Gid);

            if (entry is PosixTarEntry posix)
            {
                Assert.Equal(DefaultGName, posix.GroupName);
                Assert.Equal(DefaultUName, posix.UserName);

                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);

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
