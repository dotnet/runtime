// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;

namespace System.Formats.Tar.Tests
{
    public partial class TarWriter_WriteEntry_File_Tests : TarTestsBase
    {
        partial void VerifyPlatformSpecificMetadata(string filePath, TarEntry entry)
        {
            FileSystemInfo info;
            if (entry.EntryType == TarEntryType.Directory)
            {
                info = new DirectoryInfo(filePath);
            }
            else
            {
                info = new FileInfo(filePath);
            }

            VerifyTimestamp(info.LastWriteTimeUtc, entry.ModificationTime);

            // Archives created in Windows always set mode to 777
            Assert.Equal(DefaultWindowsMode, entry.Mode);

            Assert.Equal(DefaultUid, entry.Uid);
            Assert.Equal(DefaultGid, entry.Gid);

            if (entry is PosixTarEntry posix)
            {
                Assert.Equal(DefaultGName, posix.GroupName);
                Assert.Equal(DefaultUName, posix.UserName);

                Assert.Equal(DefaultDeviceMajor, posix.DeviceMajor);
                Assert.Equal(DefaultDeviceMinor, posix.DeviceMinor);
            }

            if (entry is PaxTarEntry pax)
            {
                if (pax.ExtendedAttributes.ContainsKey("atime"))
                {
                    long longATime = long.Parse(pax.ExtendedAttributes["atime"]);
                    DateTimeOffset actualATime = DateTimeOffset.FromUnixTimeSeconds(longATime);

                    VerifyTimestamp(info.LastAccessTimeUtc, actualATime);
                }
                if (pax.ExtendedAttributes.ContainsKey("ctime"))
                {
                    long longCTime = long.Parse(pax.ExtendedAttributes["ctime"]);
                    DateTimeOffset actualCTime = DateTimeOffset.FromUnixTimeSeconds(longCTime);

                    VerifyTimestamp(info.CreationTimeUtc, actualCTime);// TODO: Verify if CreationTime is what we want to map to CTime on Windows
                }
            }

            if (entry is GnuTarEntry gnu)
            {
                VerifyTimestamp(info.LastAccessTimeUtc, gnu.AccessTime);
                VerifyTimestamp(info.CreationTimeUtc, gnu.ChangeTime);// TODO: Verify if CreationTime is what we want to map to CTime on Windows
            }
        }

        private void VerifyTimestamp(DateTime expected, DateTimeOffset actual)
        {
            // TODO: Find out best way to compare DateTime vs DateTimeOffset,
            // because DateTime seems to truncate the miliseconds
            Assert.Equal(expected.Date, actual.Date);
            Assert.Equal(expected.Hour, actual.Hour);
            Assert.Equal(expected.Minute, actual.Minute);
            Assert.Equal(expected.Second, actual.Second);
        }
    }
}
