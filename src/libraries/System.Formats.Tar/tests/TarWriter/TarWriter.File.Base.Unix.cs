// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
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
                Interop.Sys.TryGetGroupName(status.Gid, out string gname);
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

        protected int CreateGroup(string groupName)
        {
            int exitCode = Execute("groupadd", groupName, out string standardOutput, out string standardError);
            if (exitCode != 0)
            {
                ThrowOnError(exitCode, "groupadd", groupName, standardError);
            }
            return GetGroupId(groupName);
        }

        protected int GetGroupId(string groupName)
        {
            int exitCode = Execute("getent", $"group {groupName}", out string standardOutput, out string standardError);
            if (exitCode != 0)
            {
                ThrowOnError(exitCode, "getent", "group", standardError);
            }

            string[] values = standardOutput.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            return int.Parse(values[^1]);
        }
        
        protected void SetGroupAsOwnerOfFile(string groupName, string filePath)
        {
            int exitCode = Execute("chgrp", $"{groupName} {filePath}", out string standardOutput, out string standardError);
            if (exitCode != 0)
            {
                ThrowOnError(exitCode, "chgroup", $"{groupName} {filePath}", standardError);
            }
        }

        protected void DeleteGroup(string groupName)
        {
            int exitCode = Execute("groupdel", groupName, out string standardOutput, out string standardError);
            if (exitCode != 0)
            {
                ThrowOnError(exitCode, "groupdel", groupName, standardError);
            }
        }

        private int Execute(string command, string arguments, out string standardOutput, out string standardError)
        {
            using Process p = new Process();

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            p.WaitForExit();

            standardOutput = p.StandardOutput.ReadToEnd();
            standardError = p.StandardError.ReadToEnd();
            return p.ExitCode;
        }

        private void ThrowOnError(int code, string command, string arguments, string message)
        {
            throw new IOException($"Error '{code}' when executing '{command} {arguments}'. Message: {message}");
        }
    }
}
