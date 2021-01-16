// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

                foreach (string file in Directory.EnumerateFiles(Path, "*", SearchOption.AllDirectories))
                {
                    var fileSecurity = new FileSecurity(file, AccessControlSections.Access);
                    var fileInfo = new FileInfo(file);
                    fileInfo.SetAccessControl(fileSecurity);
                }

                foreach (string directory in Directory.EnumerateDirectories(Path, "*", SearchOption.AllDirectories))
                {
                    var directorySecurity = new DirectorySecurity(directory, AccessControlSections.Access);
                    var directoryInfo = new DirectoryInfo(directory);
                    directoryInfo.SetAccessControl(directorySecurity);
                }

                Directory.Delete(Path, recursive: true);
            }
            catch () { /* Do not throw because we call this on finalize */ }
        }
    }
}
