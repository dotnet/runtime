// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        public static DirectoryInfo CreateDirectory(this DirectorySecurity directorySecurity, string path)
        {
            if (directorySecurity == null)
                throw new ArgumentNullException(nameof(directorySecurity));

            if (path == null)
                throw new ArgumentNullException(nameof(path));

            if (path.Length == 0)
                throw new ArgumentException(SR.Arg_PathEmpty);

            return Directory.CreateDirectory(path, directorySecurity);
        }

        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

            return directoryInfo.GetAccessControl();
        }

        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo, AccessControlSections includeSections)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

            return directoryInfo.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
        {
            if (directoryInfo == null)
                throw new ArgumentNullException(nameof(directoryInfo));

            if (directorySecurity == null)
                throw new ArgumentNullException(nameof(directorySecurity));

            directoryInfo.SetAccessControl(directorySecurity);
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            return fileInfo.GetAccessControl();
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo, AccessControlSections includeSections)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            return fileInfo.GetAccessControl(includeSections);
        }

        public static void SetAccessControl(this FileInfo fileInfo, FileSecurity fileSecurity)
        {
            if (fileInfo == null)
                throw new ArgumentNullException(nameof(fileInfo));

            if (fileSecurity == null)
                throw new ArgumentNullException(nameof(fileSecurity));

            fileInfo.SetAccessControl(fileSecurity);
        }

        public static FileSecurity GetAccessControl(this FileStream fileStream)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            return fileStream.GetAccessControl();
        }

        public static void SetAccessControl(this FileStream fileStream, FileSecurity fileSecurity)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (fileSecurity == null)
                throw new ArgumentNullException(nameof(fileSecurity));

            fileStream.SetAccessControl(fileSecurity);
        }
    }
}
