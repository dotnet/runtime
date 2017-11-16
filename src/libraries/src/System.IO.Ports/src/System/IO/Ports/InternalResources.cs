// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace System.IO.Ports
{
    internal static class InternalResources
    {
        // Beginning of static Error methods
        internal static void EndOfFile()
        {
            throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static String GetMessage(int errorCode)
        {
            return new Win32Exception(errorCode).Message;
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, SR.Port_not_open);
        }

        internal static void WrongAsyncResult()
        {
            throw new ArgumentException(SR.Arg_WrongAsyncResult);
        }

        internal static void EndReadCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and SerialStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndReadCalledMultiple);
        }

        internal static void EndWriteCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and SerialStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndWriteCalledMultiple);
        }

        internal static void WinIOError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, String.Empty);
        }

        internal static void WinIOError(string str)
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, str);
        }

        // After calling GetLastWin32Error(), it clears the last error field,
        // so you must save the HResult and pass it to this method.  This method
        // will determine the appropriate exception to throw dependent on your
        // error, and depending on the error, insert a string into the message
        // gotten from the ResourceManager.
        internal static void WinIOError(int errorCode, String str)
        {
            switch (errorCode)
            {
                case Interop.Errors.ERROR_FILE_NOT_FOUND:
                case Interop.Errors.ERROR_PATH_NOT_FOUND:
                    if (str.Length == 0)
                        throw new IOException(SR.IO_PortNotFound);
                    else
                        throw new IOException(string.Format(SR.IO_PortNotFoundFileName, str));

                case Interop.Errors.ERROR_ACCESS_DENIED:
                    if (str.Length == 0)
                        throw new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);
                    else
                        throw new UnauthorizedAccessException(string.Format(SR.UnauthorizedAccess_IODenied_Path, str));

                case Interop.Errors.ERROR_FILENAME_EXCED_RANGE:
                    if (string.IsNullOrEmpty(str))
                        throw new PathTooLongException(SR.IO_PathTooLong);
                    else
                        throw new PathTooLongException(SR.Format(SR.IO_PathTooLong_Path, str));

                case Interop.Errors.ERROR_SHARING_VIOLATION:
                    // error message.
                    if (str.Length == 0)
                        throw new IOException(SR.IO_SharingViolation_NoFileName);
                    else
                        throw new IOException(string.Format(SR.IO_SharingViolation_File, str));

                default:
                    throw new IOException(GetMessage(errorCode), MakeHRFromErrorCode(errorCode));
            }
        }

        // Use this to translate error codes like the above into HRESULTs like
        // 0x80070006 for ERROR_INVALID_HANDLE
        internal static int MakeHRFromErrorCode(int errorCode)
        {
            return unchecked(((int)0x80070000) | errorCode);
        }
    }
}

