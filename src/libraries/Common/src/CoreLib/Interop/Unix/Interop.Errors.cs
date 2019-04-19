// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    /// <summary>Common Unix errno error codes.</summary>
    internal enum Error
    {
        // These values were defined in src/Native/System.Native/fxerrno.h
        //
        // They compare against values obtained via Interop.Sys.GetLastError() not Marshal.GetLastWin32Error()
        // which obtains the raw errno that varies between unixes. The strong typing as an enum is meant to
        // prevent confusing the two. Casting to or from int is suspect. Use GetLastErrorInfo() if you need to
        // correlate these to the underlying platform values or obtain the corresponding error message.
        // 

        SUCCESS          = 0,

        E2BIG            = 0x10001,           // Argument list too long.
        EACCES           = 0x10002,           // Permission denied.
        EADDRINUSE       = 0x10003,           // Address in use.
        EADDRNOTAVAIL    = 0x10004,           // Address not available.
        EAFNOSUPPORT     = 0x10005,           // Address family not supported.
        EAGAIN           = 0x10006,           // Resource unavailable, try again (same value as EWOULDBLOCK),
        EALREADY         = 0x10007,           // Connection already in progress.
        EBADF            = 0x10008,           // Bad file descriptor.
        EBADMSG          = 0x10009,           // Bad message.
        EBUSY            = 0x1000A,           // Device or resource busy.
        ECANCELED        = 0x1000B,           // Operation canceled.
        ECHILD           = 0x1000C,           // No child processes.
        ECONNABORTED     = 0x1000D,           // Connection aborted.
        ECONNREFUSED     = 0x1000E,           // Connection refused.
        ECONNRESET       = 0x1000F,           // Connection reset.
        EDEADLK          = 0x10010,           // Resource deadlock would occur.
        EDESTADDRREQ     = 0x10011,           // Destination address required.
        EDOM             = 0x10012,           // Mathematics argument out of domain of function.
        EDQUOT           = 0x10013,           // Reserved.
        EEXIST           = 0x10014,           // File exists.
        EFAULT           = 0x10015,           // Bad address.
        EFBIG            = 0x10016,           // File too large.
        EHOSTUNREACH     = 0x10017,           // Host is unreachable.
        EIDRM            = 0x10018,           // Identifier removed.
        EILSEQ           = 0x10019,           // Illegal byte sequence.
        EINPROGRESS      = 0x1001A,           // Operation in progress.
        EINTR            = 0x1001B,           // Interrupted function.
        EINVAL           = 0x1001C,           // Invalid argument.
        EIO              = 0x1001D,           // I/O error.
        EISCONN          = 0x1001E,           // Socket is connected.
        EISDIR           = 0x1001F,           // Is a directory.
        ELOOP            = 0x10020,           // Too many levels of symbolic links.
        EMFILE           = 0x10021,           // File descriptor value too large.
        EMLINK           = 0x10022,           // Too many links.
        EMSGSIZE         = 0x10023,           // Message too large.
        EMULTIHOP        = 0x10024,           // Reserved.
        ENAMETOOLONG     = 0x10025,           // Filename too long.
        ENETDOWN         = 0x10026,           // Network is down.
        ENETRESET        = 0x10027,           // Connection aborted by network.
        ENETUNREACH      = 0x10028,           // Network unreachable.
        ENFILE           = 0x10029,           // Too many files open in system.
        ENOBUFS          = 0x1002A,           // No buffer space available.
        ENODEV           = 0x1002C,           // No such device.
        ENOENT           = 0x1002D,           // No such file or directory.
        ENOEXEC          = 0x1002E,           // Executable file format error.
        ENOLCK           = 0x1002F,           // No locks available.
        ENOLINK          = 0x10030,           // Reserved.
        ENOMEM           = 0x10031,           // Not enough space.
        ENOMSG           = 0x10032,           // No message of the desired type.
        ENOPROTOOPT      = 0x10033,           // Protocol not available.
        ENOSPC           = 0x10034,           // No space left on device.
        ENOSYS           = 0x10037,           // Function not supported.
        ENOTCONN         = 0x10038,           // The socket is not connected.
        ENOTDIR          = 0x10039,           // Not a directory or a symbolic link to a directory.
        ENOTEMPTY        = 0x1003A,           // Directory not empty.
        ENOTRECOVERABLE  = 0x1003B,           // State not recoverable.
        ENOTSOCK         = 0x1003C,           // Not a socket.
        ENOTSUP          = 0x1003D,           // Not supported (same value as EOPNOTSUP).
        ENOTTY           = 0x1003E,           // Inappropriate I/O control operation.
        ENXIO            = 0x1003F,           // No such device or address.
        EOVERFLOW        = 0x10040,           // Value too large to be stored in data type.
        EOWNERDEAD       = 0x10041,           // Previous owner died.
        EPERM            = 0x10042,           // Operation not permitted.
        EPIPE            = 0x10043,           // Broken pipe.
        EPROTO           = 0x10044,           // Protocol error.
        EPROTONOSUPPORT  = 0x10045,           // Protocol not supported.
        EPROTOTYPE       = 0x10046,           // Protocol wrong type for socket.
        ERANGE           = 0x10047,           // Result too large.
        EROFS            = 0x10048,           // Read-only file system.
        ESPIPE           = 0x10049,           // Invalid seek.
        ESRCH            = 0x1004A,           // No such process.
        ESTALE           = 0x1004B,           // Reserved.
        ETIMEDOUT        = 0x1004D,           // Connection timed out.
        ETXTBSY          = 0x1004E,           // Text file busy.
        EXDEV            = 0x1004F,           // Cross-device link.
        ESOCKTNOSUPPORT  = 0x1005E,           // Socket type not supported.
        EPFNOSUPPORT     = 0x10060,           // Protocol family not supported.
        ESHUTDOWN        = 0x1006C,           // Socket shutdown.
        EHOSTDOWN        = 0x10070,           // Host is down.
        ENODATA          = 0x10071,           // No data available.

        // Custom Error codes to track errors beyond kernel interface.
        EHOSTNOTFOUND    = 0x20001,           // Name lookup failed

        // POSIX permits these to have the same value and we make them always equal so
        // that CoreFX cannot introduce a dependency on distinguishing between them that
        // would not work on all platforms.
        EOPNOTSUPP      = ENOTSUP,            // Operation not supported on socket.
        EWOULDBLOCK     = EAGAIN,             // Operation would block.
    }


    // Represents a platform-agnostic Error and underlying platform-specific errno
    internal struct ErrorInfo
    {
        private Error _error;
        private int _rawErrno;

        internal ErrorInfo(int errno)
        {
            _error = Interop.Sys.ConvertErrorPlatformToPal(errno);
            _rawErrno = errno;
        }

        internal ErrorInfo(Error error)
        {
            _error = error;
            _rawErrno = -1;
        }

        internal Error Error
        {
            get { return _error; }
        }

        internal int RawErrno
        {
            get { return _rawErrno == -1 ? (_rawErrno = Interop.Sys.ConvertErrorPalToPlatform(_error)) : _rawErrno; }
        }

        internal string GetErrorMessage()
        {
            return Interop.Sys.StrError(RawErrno);
        }

#pragma warning disable CS8609 // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/23268
        public override string ToString()
#pragma warning restore CS8609
        {
            return $"RawErrno: {RawErrno} Error: {Error} GetErrorMessage: {GetErrorMessage()}"; // No localization required; text is member names used for debugging purposes
        }
    }

    internal partial class Sys
    {
        internal static Error GetLastError()
        {
            return ConvertErrorPlatformToPal(Marshal.GetLastWin32Error());
        }

        internal static ErrorInfo GetLastErrorInfo()
        {
            return new ErrorInfo(Marshal.GetLastWin32Error());
        }

        internal static unsafe string StrError(int platformErrno)
        {
            int maxBufferLength = 1024; // should be long enough for most any UNIX error
            byte* buffer = stackalloc byte[maxBufferLength];
            byte* message = StrErrorR(platformErrno, buffer, maxBufferLength);

            if (message == null)
            {
                // This means the buffer was not large enough, but still contains
                // as much of the error message as possible and is guaranteed to
                // be null-terminated. We're not currently resizing/retrying because
                // maxBufferLength is large enough in practice, but we could do
                // so here in the future if necessary.
                message = buffer;
            }

            return Marshal.PtrToStringAnsi((IntPtr)message)!;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ConvertErrorPlatformToPal")]
        internal static extern Error ConvertErrorPlatformToPal(int platformErrno);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_ConvertErrorPalToPlatform")]
        internal static extern int ConvertErrorPalToPlatform(Error error);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_StrErrorR")]
        private static extern unsafe byte* StrErrorR(int platformErrno, byte* buffer, int bufferSize);
    }
}

// NOTE: extension method can't be nested inside Interop class.
internal static class InteropErrorExtensions
{
    // Intended usage is e.g. Interop.Error.EFAIL.Info() for brevity
    // vs. new Interop.ErrorInfo(Interop.Error.EFAIL) for synthesizing
    // errors. Errors originated from the system should be obtained
    // via GetLastErrorInfo(), not GetLastError().Info() as that will
    // convert twice, which is not only inefficient but also lossy if
    // we ever encounter a raw errno that no equivalent in the Error
    // enum.
    public static Interop.ErrorInfo Info(this Interop.Error error)
    {
        return new Interop.ErrorInfo(error);
    }
}
