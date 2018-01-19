// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    ///     Provides static methods for converting from Win32 errors codes to exceptions, HRESULTS and error messages.
    /// </summary>
    internal static class Win32Marshal
    {
        /// <summary>
        ///     Converts, resetting it, the last Win32 error into a corresponding <see cref="Exception"/> object.
        /// </summary>
        internal static Exception GetExceptionForLastWin32Error()
        {
            int errorCode = Marshal.GetLastWin32Error();
            return GetExceptionForWin32Error(errorCode, string.Empty);
        }

        /// <summary>
        ///     Converts the specified Win32 error into a corresponding <see cref="Exception"/> object.
        /// </summary>
        internal static Exception GetExceptionForWin32Error(int errorCode)
        {
            return GetExceptionForWin32Error(errorCode, string.Empty);
        }

        /// <summary>
        ///     Converts the specified Win32 error into a corresponding <see cref="Exception"/> object, optionally 
        ///     including the specified path in the error message.
        /// </summary>
        internal static Exception GetExceptionForWin32Error(int errorCode, string path)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    if (path.Length == 0)
                        return new FileNotFoundException(SR.IO_FileNotFound);
                    else
                        return new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, path), path);

                case Interop.Errors.ERROR_PATH_NOT_FOUND:
                    if (path.Length == 0)
                        return new DirectoryNotFoundException(SR.IO_PathNotFound_NoPathName);
                    else
                        return new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, path));

                case Interop.Errors.ERROR_ACCESS_DENIED:
                    if (path.Length == 0)
                        return new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);
                    else
                        return new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path));

                case Interop.Errors.ERROR_ALREADY_EXISTS:
                    if (path.Length == 0)
                        goto default;

                    return new IOException(SR.Format(SR.IO_AlreadyExists_Name, path), MakeHRFromErrorCode(errorCode));

                case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                    if (path.Length == 0)
                        return new PathTooLongException(SR.IO_PathTooLong);
                    else
                        return new PathTooLongException(SR.Format(SR.IO_PathTooLong_Path, path));

                case Interop.Errors.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, path));                    

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                    return new IOException(Interop.Kernel32.GetMessage(errorCode), MakeHRFromErrorCode(errorCode));

                case Interop.Errors.ERROR_SHARING_VIOLATION:
                    if (path.Length == 0)
                        return new IOException(SR.IO_SharingViolation_NoFileName, MakeHRFromErrorCode(errorCode));
                    else
                        return new IOException(SR.Format(SR.IO_SharingViolation_File, path), MakeHRFromErrorCode(errorCode));

                case Interop.Errors.ERROR_FILE_EXISTS:
                    if (path.Length == 0)
                        goto default;

                    return new IOException(SR.Format(SR.IO_FileExists_Name, path), MakeHRFromErrorCode(errorCode));

                case Interop.Errors.ERROR_OPERATION_ABORTED:
                    return new OperationCanceledException();

                default:
                    return new IOException(Interop.Kernel32.GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
            }
        }

        /// <summary>
        ///     Returns a HRESULT for the specified Win32 error code.
        /// </summary>
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            Debug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");

            return unchecked(((int)0x80070000) | errorCode);
        }
    }
}
