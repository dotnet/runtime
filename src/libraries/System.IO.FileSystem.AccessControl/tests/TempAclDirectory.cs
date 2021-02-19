// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;

namespace System.IO
{
    /// <summary>
    /// Represents a temporary directory.
    /// Disposing will recurse all files and directories inside it, ensure the
    /// appropriate access control is set, then delete all of them.
    /// </summary>
    public sealed class TempAclDirectory : TempDirectory
    {
        internal readonly List<DirectoryInfo> CreatedSubdirectories = new();
        internal readonly List<FileInfo> CreatedSubfiles = new();
        protected override void DeleteDirectory()
        {
            try
            {
                foreach (DirectoryInfo subdir in CreatedSubdirectories)
                {
                    ResetFullControlToDirectory(subdir);
                }

                foreach (FileInfo subfile in CreatedSubfiles)
                {
                    ResetFullControlToFile(subfile);
                }

                var rootDirInfo = new DirectoryInfo(Path);
                ResetFullControlToDirectory(rootDirInfo);
                rootDirInfo.Delete(recursive: true);
            }
            catch { /* Do not throw because we call this on finalize */ }
        }

        private void ResetFullControlToDirectory(DirectoryInfo dirInfo)
        {
            try
            {
                var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var accessRule = new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow);
                var security = new DirectorySecurity(dirInfo.FullName, AccessControlSections.Access);
                security.AddAccessRule(accessRule);
                dirInfo.SetAccessControl(security);
            }
            catch { /* Skip silently if dir does not exist */ }
        }

        private void ResetFullControlToFile(FileInfo fileInfo)
        {
            try
            {
                var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var accessRule = new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow);
                var security = new FileSecurity(fileInfo.FullName, AccessControlSections.Access);
                security.AddAccessRule(accessRule);
                fileInfo.SetAccessControl(security);
            }
            catch { /* Skip silently if file does not exist */ }
        }
    }
}
