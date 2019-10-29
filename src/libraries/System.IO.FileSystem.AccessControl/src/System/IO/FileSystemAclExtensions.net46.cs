// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.AccessControl;

namespace System.IO
{
    public static class FileSystemAclExtensions
    {
        public static FileStream Create(this FileInfo fileInfo, FileMode mode, FileSystemRights rights, FileShare share, int bufferSize, FileOptions options, FileSecurity fileSecurity)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            if (fileSecurity == null)
                throw new ArgumentNullException(nameof(fileSecurity));

            return new FileStream(fileInfo.FullName, mode, rights, share, bufferSize, options, fileSecurity);
        }

        public static void Create(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

            if (directorySecurity == null)
                throw new ArgumentNullException(nameof(directorySecurity));

            directoryInfo.Create(directorySecurity);
        }

        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo)
        {
            return directoryInfo.GetAccessControl();
        }

        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo, AccessControlSections includeSections)
        {
            return directoryInfo.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
        {
            directoryInfo.SetAccessControl(directorySecurity);
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo)
        {
            return fileInfo.GetAccessControl();
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo, AccessControlSections includeSections)
        {
            return fileInfo.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this FileInfo fileInfo, FileSecurity fileSecurity)
        {
            fileInfo.SetAccessControl(fileSecurity);
        }

        public static FileSecurity GetAccessControl(this FileStream fileStream)
        {
            return fileStream.GetAccessControl();
        }

        public static void SetAccessControl(this FileStream fileStream, FileSecurity fileSecurity)
        {
            fileStream.SetAccessControl(fileSecurity);
        }
    }
}
