// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
// Ported from DirectoryInfo.cs and made extension methods to allow extending
// the class without System.IO.FileSystem needing to rely on System.Security.AccessControl

using Microsoft.Win32.SafeHandles;
using System.Diagnostics.Contracts;
using System.Security.AccessControl;
using System;

namespace System.IO
{
    public static class FileSystemAclExtensions
    {
        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo)
        {
            return new DirectorySecurity(directoryInfo.FullName, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static DirectorySecurity GetAccessControl(this DirectoryInfo directoryInfo, AccessControlSections includeSections)
        {
            return new DirectorySecurity(directoryInfo.FullName, includeSections);
        }

        public static void SetAccessControl(this DirectoryInfo directoryInfo, DirectorySecurity directorySecurity)
        {
            if (directorySecurity == null)
                throw new ArgumentNullException("directorySecurity");
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPath(directoryInfo.FullName);
            directorySecurity.Persist(fullPath);
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo)
        {
            return GetAccessControl(fileInfo, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        public static FileSecurity GetAccessControl(this FileInfo fileInfo, AccessControlSections includeSections)
        {
            return new FileSecurity(fileInfo.FullName, includeSections);
        }

        public static void SetAccessControl(this FileInfo fileInfo, FileSecurity fileSecurity)
        {
            if (fileSecurity == null)
                throw new ArgumentNullException("fileSecurity");
            Contract.EndContractBlock();

            String fullPath = Path.GetFullPath(fileInfo.FullName);
            // Appropriate security check should be done for us by FileSecurity.
            fileSecurity.Persist(fullPath);
        }

        public static FileSecurity GetAccessControl(this FileStream fileStream)
        {
            SafeFileHandle handle = fileStream.SafeFileHandle;
            if (handle.IsClosed)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
            }
            return new FileSecurity(handle, fileStream.Name, AccessControlSections.Access | AccessControlSections.Owner | AccessControlSections.Group);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public static void SetAccessControl(this FileStream fileStream, FileSecurity fileSecurity)
        {
            SafeFileHandle handle = fileStream.SafeFileHandle;

            if (fileSecurity == null)
                throw new ArgumentNullException("fileSecurity");
            Contract.EndContractBlock();

            if (handle.IsClosed)
            {
                throw new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
            }

            fileSecurity.Persist(handle, fileStream.Name);
        }
    }
}