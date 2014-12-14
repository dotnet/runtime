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
    /// <returns>
    /// true if the system call should be retried due to it being interrupted; otherwise, false.
    /// An exception will be thrown if the system call failed for any reason other than interruption.
    /// </returns>
    internal static bool CheckIo(long result, string path = null, bool isDirectory = false)
    {
        if (result < 0)
        {
            int errno = Marshal.GetLastWin32Error();
            if (errno != (int)Interop.Errors.EINTR)
            {
                throw Interop.GetExceptionForIoErrno(errno, path, isDirectory);
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

    /// <summary>Common Unix errno error codes.</summary>
    internal enum Errors
    {
        ENOENT = 2,
        EINTR = 4,
        EWOULDBLOCK = 11,
        EACCES = 13,
        EEXIST = 17,
        EXDEV = 18,
        EISDIR = 21,
        EINVAL = 22,
        EFBIG = 27,
        ENAMETOOLONG = 36,
        ECANCELED = 125,
    }

    /// <summary>
    /// Gets an Exception to represent the supplied errno error code.
    /// </summary>
    /// <param name="errno">The error code</param>
    /// <param name="path">The path with which this error is associated.  This may be null.</param>
    /// <param name="isDirectory">true if the <paramref name="path"/> is known to be a directory; otherwise, false.</param>
    /// <returns></returns>
    internal static Exception GetExceptionForIoErrno(int errno, string path = null, bool isDirectory = false)
    {
        switch ((Errors)errno)
        {
            case Errors.EINVAL:
                throw new ArgumentException();

            case Errors.EISDIR:
                isDirectory = true;
                goto case Errors.ENOENT;

            case Errors.ENOENT:
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

            case Errors.EACCES:
                return !string.IsNullOrEmpty(path) ?
                    new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path)) :
                    new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName);

            case Errors.ENAMETOOLONG:
                return new PathTooLongException(SR.IO_PathTooLong);

            case Errors.EWOULDBLOCK:
                return !string.IsNullOrEmpty(path) ?
                    new IOException(SR.Format(SR.IO_SharingViolation_File, path), errno) :
                    new IOException(SR.IO_SharingViolation_NoFileName, errno);

            case Errors.ECANCELED:
                return new OperationCanceledException();

            case Errors.EFBIG:
                return new ArgumentOutOfRangeException("value", SR.ArgumentOutOfRange_FileLengthTooBig);

            case Errors.EEXIST:
                if (!string.IsNullOrEmpty(path))
                {
                    return new IOException(SR.Format(SR.IO_FileExists_Name, path), errno);
                }
                goto default;
        
            default:
                return new IOException(strerror(errno), errno);
        }
    }
}