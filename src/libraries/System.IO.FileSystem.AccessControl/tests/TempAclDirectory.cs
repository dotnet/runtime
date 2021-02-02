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
        protected override void DeleteDirectory()
        {
            try
            {
                var identity = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
                var accessRule = new FileSystemAccessRule(identity, FileSystemRights.FullControl, AccessControlType.Allow);

                foreach (string dirPath in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories))
                {
                    var dirInfo = new DirectoryInfo(dirPath);
                    var dirSecurity = new DirectorySecurity(dirPath, AccessControlSections.Access);
                    dirSecurity.AddAccessRule(accessRule);
                    dirInfo.SetAccessControl(dirSecurity);
                }

                foreach (string filePath in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(filePath);
                    var fileSecurity = new FileSecurity(filePath, AccessControlSections.Access);
                    fileSecurity.AddAccessRule(accessRule);
                    fileInfo.SetAccessControl(fileSecurity);
                }

                var rootDirInfo = new DirectoryInfo(Path);
                var rootSecurity = new DirectorySecurity(Path, AccessControlSections.Access);
                rootSecurity.AddAccessRule(accessRule);
                rootDirInfo.SetAccessControl(rootSecurity);
                rootDirInfo.Delete(recursive: true);
            }
            catch { /* Do not throw because we call this on finalize */ }
        }
    }
}
