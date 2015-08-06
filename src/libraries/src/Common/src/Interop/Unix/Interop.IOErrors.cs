// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    /// <summary>
    /// Validates the result of system call that returns greater than or equal to 0 on success
    /// and less than 0 on failure, with errno set to the error code.
    /// If the system call failed due to interruption (EINTR), true is returned and 
    /// the caller should (usually) retry. If the system call failed for any other reason, 
    /// an exception is thrown. Otherwise, the system call succeeded, and false is returned.
    /// </summary>
    /// <param name="result">The result of the system call.</param>
    /// <param name="path">The path with which this error is associated.  This may be null.</param>
    /// <param name="isDirectory">true if the <paramref name="path"/> is known to be a directory; otherwise, false.</param>
    /// <param name="errorRewriter">Optional function to change an error code prior to processing it.</param>
    /// <returns>
    /// true if the system call should be retried due to it being interrupted; otherwise, false.
    /// An exception will be thrown if the system call failed for any reason other than interruption.
    /// </returns>
    internal static bool CheckIo(long result, string path = null, bool isDirectory = false, Func<ErrorInfo, ErrorInfo> errorRewriter = null)
    {
        if (result < 0)
        {
            ErrorInfo errorInfo = Interop.Sys.GetLastErrorInfo();
            if (errorRewriter != null)
            {
                errorInfo = errorRewriter(errorInfo);
            }

            if (errorInfo.Error != Error.EINTR)
            {
                throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Validates the result of system call that returns a non-zero pointer on success
    /// and a zero pointer on failure.
    /// If the system call failed due to interruption (EINTR), true is returned and 
    /// the caller should (usually) retry. If the system call failed for any other reason, 
    /// an exception is thrown. Otherwise, the system call succeeded, and false is returned.
    /// </summary>
    internal static bool CheckIoPtr(IntPtr ptr, string path = null, bool isDirectory = false)
    {
        return CheckIo(ptr == IntPtr.Zero ? -1 : 0, path, isDirectory);
    }

    /// <summary>
    /// Gets an Exception to represent the supplied error info.
    /// </summary>
    /// <param name="error">The error info</param>
    /// <param name="path">The path with which this error is associated.  This may be null.</param>
    /// <param name="isDirectory">true if the <paramref name="path"/> is known to be a directory; otherwise, false.</param>
    /// <returns></returns>
    internal static Exception GetExceptionForIoErrno(ErrorInfo errorInfo, string path = null, bool isDirectory = false)
    {
        switch (errorInfo.Error)
        {
            case Error.ENOENT:
                if (isDirectory)
                {
                    return !string.IsNullOrEmpty(path) ?
                        new DirectoryNotFoundException(SR.Format(SR.IO_PathNotFound_Path, path)) :
                        new DirectoryNotFoundException(SR.IO_PathNotFound_NoPathName);
                }
                else
                {
                    return !string.IsNullOrEmpty(path) ?
                        new FileNotFoundException(SR.Format(SR.IO_FileNotFound_FileName, path), path) :
                        new FileNotFoundException(SR.IO_FileNotFound);
                }

            case Error.EACCES:
            case Error.EBADF:
            case Error.EPERM:
                return !string.IsNullOrEmpty(path) ?
                    new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path)) :
                    new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);

            case Error.ENAMETOOLONG:
                return new PathTooLongException(SR.IO_PathTooLong);

            case Error.EWOULDBLOCK:
                return !string.IsNullOrEmpty(path) ?
                    new IOException(SR.Format(SR.IO_SharingViolation_File, path), errorInfo.RawErrno) :
                    new IOException(SR.IO_SharingViolation_NoFileName, errorInfo.RawErrno);

            case Error.ECANCELED:
                return new OperationCanceledException();

            case Error.EFBIG:
                return new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_FileLengthTooBig);

            case Error.EEXIST:
                if (!string.IsNullOrEmpty(path))
                {
                    return new IOException(SR.Format(SR.IO_FileExists_Name, path), errorInfo.RawErrno);
                }
                goto default;

            default:
                return GetIOException(errorInfo);
        }
    }

    internal static Exception GetIOException(Interop.ErrorInfo errorInfo)
    {
        return new IOException(errorInfo.GetErrorMessage(), errorInfo.RawErrno);
    }
}
