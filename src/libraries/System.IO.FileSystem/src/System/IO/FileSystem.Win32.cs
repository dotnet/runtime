// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

#if MS_IO_REDIST
using System.IO;

namespace Microsoft.IO
#else
namespace System.IO
#endif
{
    internal static partial class FileSystem
    {
        public static void Encrypt(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!Interop.Kernel32.EncryptFile(fullPath))
            {
                ThrowExceptionEncryptDecryptFail(fullPath);
            }
        }

        public static void Decrypt(string path)
        {
            string fullPath = Path.GetFullPath(path);

            if (!Interop.Kernel32.DecryptFile(fullPath))
            {
                ThrowExceptionEncryptDecryptFail(fullPath);
            }
        }

        private static unsafe void ThrowExceptionEncryptDecryptFail(string fullPath)
        {
            int errorCode = Marshal.GetLastWin32Error();
            if (errorCode == Interop.Errors.ERROR_ACCESS_DENIED)
            {
                // Check to see if the file system support the Encrypted File System (EFS)
                string name = DriveInfoInternal.NormalizeDriveName(Path.GetPathRoot(fullPath));

                using (DisableMediaInsertionPrompt.Create())
                {
                    if (!Interop.Kernel32.GetVolumeInformation(name, null, 0, null, null, out int fileSystemFlags, null, 0))
                    {
                        errorCode = Marshal.GetLastWin32Error();
                        throw Win32Marshal.GetExceptionForWin32Error(errorCode, name);
                    }

                    if ((fileSystemFlags & Interop.Kernel32.FILE_SUPPORTS_ENCRYPTION) == 0)
                    {
                        throw new NotSupportedException(SR.PlatformNotSupported_FileEncryption);
                    }
                }
            }
            throw Win32Marshal.GetExceptionForWin32Error(errorCode, fullPath);
        }
    }
}
