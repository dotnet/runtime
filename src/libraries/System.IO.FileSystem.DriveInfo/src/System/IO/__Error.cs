// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace System.IO
{
    [Pure]
    internal static class __Error
    {
        // An alternative to Win32Marshal with friendlier messages for drives
        [System.Security.SecuritySafeCritical]  // auto-generated
        internal static Exception GetExceptionForLastWin32DriveError(String driveName)
        {
            int errorCode = Marshal.GetLastWin32Error();
            return GetExceptionForWin32DriveError(errorCode, driveName);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static Exception GetExceptionForWin32DriveError(int errorCode, String driveName)
        {
            switch (errorCode)
            {
                case Interop.mincore.Errors.ERROR_PATH_NOT_FOUND:
                case Interop.mincore.Errors.ERROR_INVALID_DRIVE:
                    return new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, driveName));

                default:
                    return Win32Marshal.GetExceptionForWin32Error(errorCode, driveName);
            }
        }
    }
}
