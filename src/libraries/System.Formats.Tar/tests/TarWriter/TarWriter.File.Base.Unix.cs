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
                Assert.True(Interop.Sys.TryGetGroupName(status.Gid, out string gname));
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
            Execute("groupadd", groupName);
            return GetGroupId(groupName);
        }

        protected int CreateUser(string userName)
        {
            Execute("useradd", userName);
            return GetUserId(userName);
        }

        protected int GetGroupId(string groupName)
        {
            string standardOutput = Execute("getent", $"group {groupName}");
            string[] values = standardOutput.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return int.Parse(values[^1]);
        }

        protected int GetUserId(string userName)
        {
            string standardOutput = Execute("id", $"-u {userName}");
            return int.Parse(standardOutput);
        }

        protected void SetGroupAsOwnerOfFile(string groupName, string filePath) =>
            Execute("chgrp", $"{groupName} {filePath}");

        protected void SetUserAsOwnerOfFile(string userName, string filePath) =>
            Execute("chown", $"{userName} {filePath}");

        protected void DeleteGroup(string groupName)
        {
            Execute("groupdel", groupName);
            Threading.Thread.Sleep(250);
            Assert.Throws<IOException>(() => GetGroupId(groupName));
        }

        protected void DeleteUser(string userName)
        {
            Execute("userdel", $"-f {userName}");
            Threading.Thread.Sleep(250);
            Assert.Throws<IOException>(() => GetUserId(userName));
        }

        private string Execute(string command, string arguments)
        {
            using Process p = new Process();

            p.StartInfo.FileName = command;
            p.StartInfo.Arguments = arguments;

            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;

            string standardError = string.Empty;
            p.ErrorDataReceived += new DataReceivedEventHandler((sender, e) => { standardError += e.Data; });

            string standardOutput = string.Empty;
            p.OutputDataReceived += new DataReceivedEventHandler((sender, e) => { standardOutput += e.Data; });

            p.Start();

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                throw new IOException($"Error '{p.ExitCode}' when executing '{command} {arguments}'. Message: {standardError}");
            }

            return standardOutput;
        }
    }
}
