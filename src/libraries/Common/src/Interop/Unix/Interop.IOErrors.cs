// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    private static void ThrowExceptionForIoErrno(ErrorInfo errorInfo, string? path, bool isDirectory)
    {
        Debug.Assert(errorInfo.Error != Error.SUCCESS);
        Debug.Assert(errorInfo.Error != Error.EINTR, "EINTR errors should be handled by the native shim and never bubble up to managed code");

        throw Interop.GetExceptionForIoErrno(errorInfo, path, isDirectory);
    }

    internal static void CheckIo(Error error, string? path = null, bool isDirectory = false)
    {
        if (error != Interop.Error.SUCCESS)
        {
            ThrowExceptionForIoErrno(error.Info(), path, isDirectory);
        }
    }

    /// <summary>
    /// Validates the result of system call that returns greater than or equal to 0 on success
    /// and less than 0 on failure, with errno set to the error code.
    /// If the system call failed for any reason, an exception is thrown. Otherwise, the system call succeeded.
    /// </summary>
    /// <param name="result">The result of the system call.</param>
    /// <param name="path">The path with which this error is associated.  This may be null.</param>
    /// <param name="isDirectory">true if the <paramref name="path"/> is known to be a directory; otherwise, false.</param>
    /// <returns>
    /// On success, returns the non-negative result long that was validated.
    /// </returns>
    internal static long CheckIo(long result, string? path = null, bool isDirectory = false)
    {
        if (result < 0)
        {
            ThrowExceptionForIoErrno(Sys.GetLastErrorInfo(), path, isDirectory);
        }

        return result;
    }

    /// <summary>
    /// Validates the result of system call that returns greater than or equal to 0 on success
    /// and less than 0 on failure, with errno set to the error code.
    /// If the system call failed for any reason, an exception is thrown. Otherwise, the system call succeeded.
    /// </summary>
    /// <returns>
    /// On success, returns the non-negative result int that was validated.
    /// </returns>
    internal static int CheckIo(int result, string? path = null, bool isDirectory = false)
    {
        CheckIo((long)result, path, isDirectory);

        return result;
    }

    /// <summary>
    /// Validates the result of system call that returns greater than or equal to 0 on success
    /// and less than 0 on failure, with errno set to the error code.
    /// If the system call failed for any reason, an exception is thrown. Otherwise, the system call succeeded.
    /// </summary>
    /// <returns>
    /// On success, returns the non-negative result IntPtr that was validated.
    /// </returns>
    internal static IntPtr CheckIo(IntPtr result, string? path = null, bool isDirectory = false)
    {
        CheckIo((long)result, path, isDirectory);

        return result;
    }

    /// <summary>
    /// Validates the result of system call that returns greater than or equal to 0 on success
    /// and less than 0 on failure, with errno set to the error code.
    /// If the system call failed for any reason, an exception is thrown. Otherwise, the system call succeeded.
    /// </summary>
    /// <returns>
    /// On success, returns the valid SafeFileHandle that was validated.
    /// </returns>
    internal static TSafeHandle CheckIo<TSafeHandle>(TSafeHandle handle, string? path = null, bool isDirectory = false)
        where TSafeHandle : SafeHandle
    {
        if (handle.IsInvalid)
        {
            Exception e = Interop.GetExceptionForIoErrno(Sys.GetLastErrorInfo(), path, isDirectory);
            handle.Dispose();
            throw e;
        }

        return handle;
    }

    /// <summary>
    /// Gets an Exception to represent the supplied error info.
    /// </summary>
    /// <param name="errorInfo">The error info</param>
    /// <param name="path">The path with which this error is associated.  This may be null.</param>
    /// <param name="isDirectory">true if the <paramref name="path"/> is known to be a directory; otherwise, false.</param>
    /// <returns></returns>
    internal static Exception GetExceptionForIoErrno(ErrorInfo errorInfo, string? path = null, bool isDirectory = false)
    {
        // Translate the errno into a known set of exception types.  For cases where multiple errnos map
        // to the same exception type, include an inner exception with the details.
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
                Exception inner = GetIOException(errorInfo);
                return !string.IsNullOrEmpty(path) ?
                    new UnauthorizedAccessException(SR.Format(SR.UnauthorizedAccess_IODenied_Path, path), inner) :
                    new UnauthorizedAccessException(SR.UnauthorizedAccess_IODenied_NoPathName, inner);

            case Error.ENAMETOOLONG:
                return !string.IsNullOrEmpty(path) ?
                    new PathTooLongException(SR.Format(SR.IO_PathTooLong_Path, path)) :
                    new PathTooLongException(SR.IO_PathTooLong);

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
                return GetIOException(errorInfo, path);
        }
    }

    internal static Exception GetIOException(Interop.ErrorInfo errorInfo, string? path = null)
    {
        string msg = errorInfo.GetErrorMessage();
        return new IOException(
            string.IsNullOrEmpty(path) ? msg : $"{msg} : '{path}'", errorInfo.RawErrno);
    }
}
