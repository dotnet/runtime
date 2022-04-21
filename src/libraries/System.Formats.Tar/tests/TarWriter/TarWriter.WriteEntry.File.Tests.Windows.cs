// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
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

                if (entry is PaxTarEntry pax)
                {
                    Assert.True(pax.ExtendedAttributes.Count >= 4);
                    Assert.Contains("path", pax.ExtendedAttributes);
                    Assert.Contains("mtime", pax.ExtendedAttributes);
                    Assert.Contains("atime", pax.ExtendedAttributes);
                    Assert.Contains("ctime", pax.ExtendedAttributes);

                    Assert.True(double.TryParse(pax.ExtendedAttributes["mtime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleMTime));
                    DateTimeOffset actualMTime = ConvertDoubleToDateTimeOffset(doubleMTime);
                    VerifyTimestamp(info.LastAccessTimeUtc, actualMTime);

                    Assert.True(double.TryParse(pax.ExtendedAttributes["atime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleATime));
                    DateTimeOffset actualATime = ConvertDoubleToDateTimeOffset(doubleATime);
                    VerifyTimestamp(info.LastAccessTimeUtc, actualATime);

                    Assert.True(double.TryParse(pax.ExtendedAttributes["ctime"], NumberStyles.Any, CultureInfo.InvariantCulture, out double doubleCTime));
                    DateTimeOffset actualCTime = ConvertDoubleToDateTimeOffset(doubleCTime);
                    VerifyTimestamp(info.LastAccessTimeUtc, actualCTime);
                }

                if (entry is GnuTarEntry gnu)
                {
                    VerifyTimestamp(info.LastAccessTimeUtc, gnu.AccessTime);
                    VerifyTimestamp(info.CreationTimeUtc, gnu.ChangeTime);
                }
            }
        }

        private void VerifyTimestamp(DateTime expected, DateTimeOffset actual)
        {
            // TODO: Find out best way to compare DateTime vs DateTimeOffset,
            // because DateTime seems to truncate the miliseconds https://github.com/dotnet/runtime/issues/68230
            Assert.Equal(expected.Date, actual.Date);
            Assert.Equal(expected.Hour, actual.Hour);
            Assert.Equal(expected.Minute, actual.Minute);
            Assert.Equal(expected.Second, actual.Second);
        }
    }
}
