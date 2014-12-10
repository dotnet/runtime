// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

// BUG: 1001383 Proposal: Move DirectoryNotFoundException, PathTooLongException -> System.IO
// When #if is removed, please also remove the appropriate ones around the tree in FxCore.
#if WIN32MARSHAL_PATH_BASED_API
        /// <summary>
        ///     Converts, resetting it, the last Win32 error into a corresponding <see cref="Exception"/> object.
        /// </summary>
        internal static Exception GetExceptionForLastWin32Error()
        {
            int errorCode = Marshal.GetLastWin32Error();
            return GetExceptionForWin32Error(errorCode, String.Empty);
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
                case Interop.ERROR_FILE_NOT_FOUND:
                    if (path.Length == 0)
                        return new FileNotFoundException(SR.IO_FileNotFound);
                    else
                        return new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, path), path);

                case Interop.ERROR_PATH_NOT_FOUND:
                    if (path.Length == 0)
                        return new DirectoryNotFoundException(SR.IO_PathNotFound_NoPathName);
                    else
                        return new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, path));

                case Interop.ERROR_ACCESS_DENIED:
                    if (path.Length == 0)
                        return new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);
                    else
                        return new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path));

                case Interop.ERROR_ALREADY_EXISTS:
                    if (path.Length == 0)
                        goto default;

                    return new IOException(SR.Format(SR.IO_AlreadyExists_Name, path), MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_FILENAME_EXCED_RANGE:
                    return new PathTooLongException(SR.IO_PathTooLong);

                case Interop.ERROR_INVALID_PARAMETER:
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_SHARING_VIOLATION:
                    if (path.Length == 0)
                        return new IOException(SR.IO_SharingViolation_NoFileName, MakeHRFromErrorCode(errorCode));
                    else
                        return new IOException(SR.Format(SR.IO_SharingViolation_File, path), MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_FILE_EXISTS:
                    if (path.Length == 0)
                        goto default;

                    return new IOException(SR.Format(SR.IO_FileExists_Name, path), MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_OPERATION_ABORTED:
                    return new OperationCanceledException();

                default:
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
            }
        }
#else 
        internal static Exception GetExceptionForWin32Error(int errorCode)
        {
            switch (errorCode)
            {
                case Interop.ERROR_FILE_NOT_FOUND:
                    return new FileNotFoundException(SR.IO_FileNotFound);

                case Interop.ERROR_ACCESS_DENIED:
                    return new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);

                case Interop.ERROR_INVALID_PARAMETER:
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_SHARING_VIOLATION:
                    return new IOException(SR.IO_SharingViolation_NoFileName, MakeHRFromErrorCode(errorCode));

                case Interop.ERROR_OPERATION_ABORTED:
                    return new OperationCanceledException();

                default:
                    return new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
            }
        }
#endif

        /// <summary>
        ///     Returns a HRESULT for the specified Win32 error code.
        /// </summary>
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            Debug.Assert((0xFFFF0000 & errorCode) == 0, "This is an HRESULT, not an error code!");

            return unchecked(((int)0x80070000) | errorCode);
        }

        /// <summary>
        ///     Returns a Win32 error code for the specified HRESULT if it came from FACILITY_WIN32
        ///     If not, returns the HRESULT unchanged
        /// </summary>
        internal static int TryMakeWin32ErrorCodeFromHR(int hr) {
            if ((0xFFFF0000 & hr) == 0x80070000) {
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
            const int FORMAT_MESSAGE_IGNORE_INSERTS = 0x00000200;
            const int FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;
            const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 0x00002000;

            char[] buffer = new char[512];
            uint result = Interop.mincore.FormatMessage(FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, (uint)errorCode, 0, buffer, (uint)buffer.Length, IntPtr.Zero);
            if (result != 0)
            {
                // result is the # of characters copied to the StringBuilder on NT,
                // but on Win9x, it appears to be the number of MBCS buffer.
                // Just give up and return the String as-is...
                return new string(buffer, 0, (int)result);
            }
            else
            {
                return SR.Format(SR.UnknownError_Num, errorCode);
            }
        }
    }
}