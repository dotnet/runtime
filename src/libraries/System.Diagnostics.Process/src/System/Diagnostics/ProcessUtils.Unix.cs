// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessUtils
    {
        private static bool IsExecutable(string fullPath)
        {
            Interop.Sys.FileStatus fileinfo;

            if (Interop.Sys.Stat(fullPath, out fileinfo) < 0)
            {
                return false;
            }

            // Check if the path is a directory.
            if ((fileinfo.Mode & Interop.Sys.FileTypes.S_IFMT) == Interop.Sys.FileTypes.S_IFDIR)
            {
                return false;
            }

            const UnixFileMode AllExecute = UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute;

            UnixFileMode permissions = ((UnixFileMode)fileinfo.Mode) & AllExecute;

            // Avoid checking user/group when permission.
            if (permissions == AllExecute)
            {
                return true;
            }
            else if (permissions == 0)
            {
                return false;
            }

            uint euid = Interop.Sys.GetEUid();

            if (euid == 0)
            {
                return true; // We're root.
            }

            if (euid == fileinfo.Uid)
            {
                // We own the file.
                return (permissions & UnixFileMode.UserExecute) != 0;
            }

            bool groupCanExecute = (permissions & UnixFileMode.GroupExecute) != 0;
            bool otherCanExecute = (permissions & UnixFileMode.OtherExecute) != 0;

            // Avoid group check when group and other have same permissions.
            if (groupCanExecute == otherCanExecute)
            {
                return groupCanExecute;
            }

            if (Interop.Sys.IsMemberOfGroup(fileinfo.Gid))
            {
                return groupCanExecute;
            }
            else
            {
                return otherCanExecute;
            }
        }

    }
}
