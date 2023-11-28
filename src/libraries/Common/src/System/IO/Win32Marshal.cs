// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    /// Provides static methods for converting from Win32 errors codes to exceptions, HRESULTS and error messages.
    /// </summary>
    internal static class Win32Marshal
    {
        /// <summary>
        /// Converts, resetting it, the last Win32 error into a corresponding <see cref="Exception"/> object, optionally
        /// including the specified path in the error message.
        /// </summary>
        internal static Exception GetExceptionForLastWin32Error(string? path = "")
            => GetExceptionForWin32Error(Marshal.GetLastPInvokeError(), path);

        /// <summary>
        /// Converts the specified Win32 error into a corresponding <see cref="Exception"/> object, optionally
        /// including the specified path in the error message.
        /// </summary>
        internal static Exception GetExceptionForWin32Error(int errorCode, string? path = "", string? errorDetails = null)
        {
            // ERROR_SUCCESS gets thrown when another unexpected interop call was made before checking GetLastWin32Error().
            // Errors have to get retrieved as soon as possible after P/Invoking to avoid this.
            Debug.Assert(errorCode != Interop.Errors.ERROR_SUCCESS);

            switch (errorCode)
            {
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                    return new FileNotFoundException(
                        string.IsNullOrEmpty(path) ? SR.IO_FileNotFound : SR.Format(SR.IO_FileNotFound_FileName, path), path);
                case Interop.Errors.ERROR_PATH_NOT_FOUND:
                    return new DirectoryNotFoundException(
                        string.IsNullOrEmpty(path) ? SR.IO_PathNotFound_NoPathName : SR.Format(SR.IO_PathNotFound_Path, path));
                case Interop.Errors.ERROR_ACCESS_DENIED:
                    return new UnauthorizedAccessException(
                        string.IsNullOrEmpty(path) ? SR.UnauthorizedAccess_IODenied_NoPathName : SR.Format(SR.UnauthorizedAccess_IODenied_Path, path));
                case Interop.Errors.ERROR_ALREADY_EXISTS:
                    if (string.IsNullOrEmpty(path))
                        goto default;
                    return new IOException(SR.Format(SR.IO_AlreadyExists_Name, path), MakeHRFromErrorCode(errorCode));
                case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                    return new PathTooLongException(
                        string.IsNullOrEmpty(path) ? SR.IO_PathTooLong : SR.Format(SR.IO_PathTooLong_Path, path));
                case Interop.Errors.ERROR_SHARING_VIOLATION:
                    return new IOException(
                        string.IsNullOrEmpty(path) ? SR.IO_SharingViolation_NoFileName : SR.Format(SR.IO_SharingViolation_File, path),
                        MakeHRFromErrorCode(errorCode));
                case Interop.Errors.ERROR_FILE_EXISTS:
                    if (string.IsNullOrEmpty(path))
                        goto default;
                    return new IOException(SR.Format(SR.IO_FileExists_Name, path), MakeHRFromErrorCode(errorCode));
                case Interop.Errors.ERROR_OPERATION_ABORTED:
                    return new OperationCanceledException();
                case Interop.Errors.ERROR_INVALID_PARAMETER:

                default:
                    string msg = GetPInvokeErrorMessage(errorCode);
                    if (!string.IsNullOrEmpty(path))
                    {
                        msg += $" : '{path}'.";
                    }
                    if (!string.IsNullOrEmpty(errorDetails))
                    {
                        msg += $" {errorDetails}";
                    }

                    return new IOException(msg, MakeHRFromErrorCode(errorCode));
            }

            static string GetPInvokeErrorMessage(int errorCode)
            {
                // Call Kernel32.GetMessage directly in CoreLib. It eliminates one level of indirection and it is necessary to
                // produce correct error messages for CoreCLR Win32 PAL.
#if NET7_0_OR_GREATER && !SYSTEM_PRIVATE_CORELIB
                return Marshal.GetPInvokeErrorMessage(errorCode);
#else
                return Interop.Kernel32.GetMessage(errorCode);
#endif
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
        /// Returns a Win32 error code for the specified HRESULT if it came from FACILITY_WIN32
        /// If not, returns the HRESULT unchanged
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
    }
}
