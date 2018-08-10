// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.IO
{
    internal static partial class FileSystem
    {
        public static void Encrypt(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!Interop.Kernel32.EncryptFile(fullPath))
            {
                ThrowException(fullPath);
            }
        }

        public static void Decrypt(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!Interop.Kernel32.DecryptFile(fullPath))
            {
                ThrowException(fullPath);
            }
        }

        private static void ThrowException(string fullPath)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
            {
                // Check to see if the file system is not NTFS. If so,
                // throw a different exception.
                DriveInfo di = new DriveInfo(Path.GetPathRoot(fullPath));
                if (!string.Equals("NTFS", di.DriveFormat))
                    throw new NotSupportedException(SR.NotSupported_EncryptionNeedsNTFS);
            }
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
        }
    }
}
