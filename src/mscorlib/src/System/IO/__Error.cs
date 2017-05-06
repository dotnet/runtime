// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** 
** 
**
**
** Purpose: Centralized error methods for the IO package.  
** Mostly useful for translating Win32 HRESULTs into meaningful
** error strings & exceptions.
**
**
===========================================================*/

using System;
using System.Runtime.InteropServices;
using Win32Native = Microsoft.Win32.Win32Native;
using System.Text;
using System.Globalization;
using System.Security;
using System.Diagnostics.Contracts;

namespace System.IO
{
    [Pure]
    internal static class __Error
    {
        internal static void EndOfFile()
        {
            throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
        }

        internal static void FileNotOpen()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_FileClosed);
        }

        internal static void StreamIsClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_StreamClosed);
        }

        internal static void MemoryStreamNotExpandable()
        {
            throw new NotSupportedException(SR.NotSupported_MemStreamNotExpandable);
        }

        internal static void ReaderClosed()
        {
            throw new ObjectDisposedException(null, SR.ObjectDisposed_ReaderClosed);
        }

        internal static void ReadNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnreadableStream);
        }

        internal static void WrongAsyncResult()
        {
            throw new ArgumentException(SR.Arg_WrongAsyncResult);
        }

        internal static void EndReadCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maitain parity with Stream and FileStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndReadCalledMultiple);
        }

        internal static void EndWriteCalledTwice()
        {
            // Should ideally be InvalidOperationExc but we can't maintain parity with Stream and FileStream without some work
            throw new ArgumentException(SR.InvalidOperation_EndWriteCalledMultiple);
        }

        // Given a possible fully qualified path, ensure that we have path
        // discovery permission to that path.  If we do not, return just the 
        // file name.  If we know it is a directory, then don't return the 
        // directory name.
        internal static String GetDisplayablePath(String path, bool isInvalidPath)
        {
            if (String.IsNullOrEmpty(path))
                return String.Empty;

            // Is it a fully qualified path?
            bool isFullyQualified = false;
            if (path.Length < 2)
                return path;
            if (PathInternal.IsDirectorySeparator(path[0]) && PathInternal.IsDirectorySeparator(path[1]))
                isFullyQualified = true;
            else if (path[1] == Path.VolumeSeparatorChar)
            {
                isFullyQualified = true;
            }

            if (!isFullyQualified && !isInvalidPath)
                return path;

            bool safeToReturn = false;
            try
            {
                if (!isInvalidPath)
                {
                    safeToReturn = true;
                }
            }
            catch (SecurityException)
            {
            }
            catch (ArgumentException)
            {
                // ? and * characters cause ArgumentException to be thrown from HasIllegalCharacters
                // inside FileIOPermission.AddPathList
            }
            catch (NotSupportedException)
            {
                // paths like "!Bogus\\dir:with/junk_.in it" can cause NotSupportedException to be thrown
                // from Security.Util.StringExpressionSet.CanonicalizePath when ':' is found in the path
                // beyond string index position 1.  
            }

            if (!safeToReturn)
            {
                if (PathInternal.IsDirectorySeparator(path[path.Length - 1]))
                    path = SR.IO_NoPermissionToDirectoryName;
                else
                    path = Path.GetFileName(path);
            }

            return path;
        }

        internal static void WinIOError()
        {
            int errorCode = Marshal.GetLastWin32Error();
            WinIOError(errorCode, String.Empty);
        }

        // After calling GetLastWin32Error(), it clears the last error field,
        // so you must save the HResult and pass it to this method.  This method
        // will determine the appropriate exception to throw dependent on your 
        // error, and depending on the error, insert a string into the message 
        // gotten from the ResourceManager.
        internal static void WinIOError(int errorCode, String maybeFullPath)
        {
            // This doesn't have to be perfect, but is a perf optimization.
            bool isInvalidPath = errorCode == Win32Native.ERROR_INVALID_NAME || errorCode == Win32Native.ERROR_BAD_PATHNAME;
            String str = GetDisplayablePath(maybeFullPath, isInvalidPath);

            switch (errorCode)
            {
                case Win32Native.ERROR_FILE_NOT_FOUND:
                    if (str.Length == 0)
                        throw new FileNotFoundException(SR.IO_FileNotFound);
                    else
                        throw new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, str), str);

                case Win32Native.ERROR_PATH_NOT_FOUND:
                    if (str.Length == 0)
                        throw new DirectoryNotFoundException(SR.IO_PathNotFound_NoPathName);
                    else
                        throw new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, str));

                case Win32Native.ERROR_ACCESS_DENIED:
                    if (str.Length == 0)
                        throw new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);
                    else
                        throw new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, str));

                case Win32Native.ERROR_ALREADY_EXISTS:
                    if (str.Length == 0)
                        goto default;
                    throw new IOException(SR.Format(SR.IO_AlreadyExists_Name, str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);

                case Win32Native.ERROR_FILENAME_EXCED_RANGE:
                    throw new PathTooLongException(SR.IO_PathTooLong);

                case Win32Native.ERROR_INVALID_DRIVE:
                    throw new DriveNotFoundException(SR.Format(SR.IO_DriveNotFound_Drive, str));

                case Win32Native.ERROR_INVALID_PARAMETER:
                    throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);

                case Win32Native.ERROR_SHARING_VIOLATION:
                    if (str.Length == 0)
                        throw new IOException(SR.IO_SharingViolation_NoFileName, Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
                    else
                        throw new IOException(SR.Format(SR.IO_SharingViolation_File, str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);

                case Win32Native.ERROR_FILE_EXISTS:
                    if (str.Length == 0)
                        goto default;
                    throw new IOException(SR.Format(SR.IO_FileExists_Name, str), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);

                case Win32Native.ERROR_OPERATION_ABORTED:
                    throw new OperationCanceledException();

                default:
                    throw new IOException(Win32Native.GetMessage(errorCode), Win32Native.MakeHRFromErrorCode(errorCode), maybeFullPath);
            }
        }

        internal static void WriteNotSupported()
        {
            throw new NotSupportedException(SR.NotSupported_UnwritableStream);
        }
    }
}
