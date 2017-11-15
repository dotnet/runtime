// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        ///     Converts, resetting it, the last Win32 error into a corresponding <see cref="Exception"/> object, optionally
        ///     including the specified path in the error message.
        /// </summary>
        internal static Exception GetExceptionForLastWin32Error(string path)
        {
            int errorCode = Marshal.GetLastWin32Error();
            return GetExceptionForWin32Error(errorCode, path);
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
                    return !string.IsNullOrEmpty(path) ?
                        new PathTooLongException(SR.Format(SR.IO_PathTooLong_Path, path)) :
                        new PathTooLongException(SR.IO_PathTooLong);

                case Interop.Errors.ERROR_INVALID_PARAMETER:
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));

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
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
            }
        }

        /// <summary>
        /// If not already an HRESULT, returns an HRESULT for the specified Win32 error code.
        /// </summary>
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            // Don't convert it if it is already an HRESULT
            if ((0xFFFF0000 & errorCode) != 0)
                return errorCode;

            return unchecked(((int)0x80070000) | errorCode);
        }

        /// <summary>
        ///     Returns a Win32 error code for the specified HRESULT if it came from FACILITY_WIN32
        ///     If not, returns the HRESULT unchanged
        /// </summary>
        internal static int TryMakeWin32ErrorCodeFromHR(int hr)
        {
            if ((0xFFFF0000 & hr) == 0x80070000)
            {
                // Win32 error, Win32Marshal.GetExceptionForWin32Error expects the Win32 format
                hr &= 0x0000FFFF;
            }

            return hr;
        }

        /// <summary>
        ///     Returns a string message for the specified Win32 error code.
        /// </summary>
        internal static string GetMessage(int errorCode)
        {
            return Interop.Kernel32.GetMessage(errorCode);
        }
    }
}
