// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma once

#include "pal_types.h"

/**
 * Error codes returned via ConvertErrno.
 *
 * Only the names (without the PAL_ prefix) are specified by POSIX.
 *
 * The values chosen below are simply assigned arbitrarily (originally
 * in alphabetical order they appear in the spec, but they can't change so
 * add new values to the end!).
 *
 * Also, the values chosen are deliberately outside the range of
 * typical UNIX errnos (small numbers), HRESULTs (negative for errors)
 * and Win32 errors (0x0000 - 0xFFFF). This isn't required for
 * correctness, but may help debug a caller that is interpreting a raw
 * int incorrectly.
 *
 * Wherever the spec says "x may be the same value as y", we do use
 * the same value so that callers cannot not take a dependency on
 * being able to distinguish between them.
 */
enum Error : int32_t
{
    PAL_SUCCESS = 0,

    PAL_E2BIG = 0x10001,           // Argument list too long.
    PAL_EACCES = 0x10002,          // Permission denied.
    PAL_EADDRINUSE = 0x10003,      // Address in use.
    PAL_EADDRNOTAVAIL = 0x10004,   // Address not available.
    PAL_EAFNOSUPPORT = 0x10005,    // Address family not supported.
    PAL_EAGAIN = 0x10006,          // Resource unavailable, try again (same value as EWOULDBLOCK),
    PAL_EALREADY = 0x10007,        // Connection already in progress.
    PAL_EBADF = 0x10008,           // Bad file descriptor.
    PAL_EBADMSG = 0x10009,         // Bad message.
    PAL_EBUSY = 0x1000A,           // Device or resource busy.
    PAL_ECANCELED = 0x1000B,       // Operation canceled.
    PAL_ECHILD = 0x1000C,          // No child processes.
    PAL_ECONNABORTED = 0x1000D,    // Connection aborted.
    PAL_ECONNREFUSED = 0x1000E,    // Connection refused.
    PAL_ECONNRESET = 0x1000F,      // Connection reset.
    PAL_EDEADLK = 0x10010,         // Resource deadlock would occur.
    PAL_EDESTADDRREQ = 0x10011,    // Destination address required.
    PAL_EDOM = 0x10012,            // Mathematics argument out of domain of function.
    PAL_EDQUOT = 0x10013,          // Reserved.
    PAL_EEXIST = 0x10014,          // File exists.
    PAL_EFAULT = 0x10015,          // Bad address.
    PAL_EFBIG = 0x10016,           // File too large.
    PAL_EHOSTUNREACH = 0x10017,    // Host is unreachable.
    PAL_EIDRM = 0x10018,           // Identifier removed.
    PAL_EILSEQ = 0x10019,          // Illegal byte sequence.
    PAL_EINPROGRESS = 0x1001A,     // Operation in progress.
    PAL_EINTR = 0x1001B,           // Interrupted function.
    PAL_EINVAL = 0x1001C,          // Invalid argument.
    PAL_EIO = 0x1001D,             // I/O error.
    PAL_EISCONN = 0x1001E,         // Socket is connected.
    PAL_EISDIR = 0x1001F,          // Is a directory.
    PAL_ELOOP = 0x10020,           // Too many levels of symbolic links.
    PAL_EMFILE = 0x10021,          // File descriptor value too large.
    PAL_EMLINK = 0x10022,          // Too many links.
    PAL_EMSGSIZE = 0x10023,        // Message too large.
    PAL_EMULTIHOP = 0x10024,       // Reserved.
    PAL_ENAMETOOLONG = 0x10025,    // Filename too long.
    PAL_ENETDOWN = 0x10026,        // Network is down.
    PAL_ENETRESET = 0x10027,       // Connection aborted by network.
    PAL_ENETUNREACH = 0x10028,     // Network unreachable.
    PAL_ENFILE = 0x10029,          // Too many files open in system.
    PAL_ENOBUFS = 0x1002A,         // No buffer space available.
    PAL_ENODEV = 0x1002C,          // No such device.
    PAL_ENOENT = 0x1002D,          // No such file or directory.
    PAL_ENOEXEC = 0x1002E,         // Executable file format error.
    PAL_ENOLCK = 0x1002F,          // No locks available.
    PAL_ENOLINK = 0x10030,         // Reserved.
    PAL_ENOMEM = 0x10031,          // Not enough space.
    PAL_ENOMSG = 0x10032,          // No message of the desired type.
    PAL_ENOPROTOOPT = 0x10033,     // Protocol not available.
    PAL_ENOSPC = 0x10034,          // No space left on device.
    PAL_ENOSYS = 0x10037,          // Function not supported.
    PAL_ENOTCONN = 0x10038,        // The socket is not connected.
    PAL_ENOTDIR = 0x10039,         // Not a directory or a symbolic link to a directory.
    PAL_ENOTEMPTY = 0x1003A,       // Directory not empty.
    PAL_ENOTRECOVERABLE = 0x1003B, // State not recoverable.
    PAL_ENOTSOCK = 0x1003C,        // Not a socket.
    PAL_ENOTSUP = 0x1003D,         // Not supported (same value as EOPNOTSUP).
    PAL_ENOTTY = 0x1003E,          // Inappropriate I/O control operation.
    PAL_ENXIO = 0x1003F,           // No such device or address.
    PAL_EOVERFLOW = 0x10040,       // Value too large to be stored in data type.
    PAL_EOWNERDEAD = 0x10041,      // Previous owner died.
    PAL_EPERM = 0x10042,           // Operation not permitted.
    PAL_EPIPE = 0x10043,           // Broken pipe.
    PAL_EPROTO = 0x10044,          // Protocol error.
    PAL_EPROTONOSUPPORT = 0x10045, // Protocol not supported.
    PAL_EPROTOTYPE = 0x10046,      // Protocol wrong type for socket.
    PAL_ERANGE = 0x10047,          // Result too large.
    PAL_EROFS = 0x10048,           // Read-only file system.
    PAL_ESPIPE = 0x10049,          // Invalid seek.
    PAL_ESRCH = 0x1004A,           // No such process.
    PAL_ESTALE = 0x1004B,          // Reserved.
    PAL_ETIMEDOUT = 0x1004D,       // Connection timed out.
    PAL_ETXTBSY = 0x1004E,         // Text file busy.
    PAL_EXDEV = 0x1004F,           // Cross-device link.

    // POSIX permits these to have the same value and we make them
    // always equal so that we cannot introduce a dependency on
    // distinguishing between them that would not work on all
    // platforms.
    PAL_EOPNOTSUPP = PAL_ENOTSUP, // Operation not supported on socket
    PAL_EWOULDBLOCK = PAL_EAGAIN, // Operation would block

    // This one is not part of POSIX, but is a catch-all for the case
    // where we cannot convert the raw errno value to something above.
    PAL_ENONSTANDARD = 0x1FFFF,
};

/**
 * Converts the given raw numeric value obtained via errno ->
 * GetLastWin32Error() to a standard numeric value defined by enum
 * Error above. If the value is not recognized, returns
 * PAL_ENONSTANDARD.
 */
extern "C" Error ConvertErrorPlatformToPal(int32_t platformErrno);

/**
 * Converts the given PAL Error value to a platform-specific errno
 * value. This is to be used when we want to synthesize a given error
 * and obtain the appropriate error message via StrErrorR.
 */
extern "C" int32_t ConvertErrorPalToPlatform(Error error);

/**
 * Obtains the system error message for the given raw numeric value
 * obtained by errno/ Marhsal.GetLastWin32Error().
 *
 * By design, this does not take a PAL errno, but a raw system errno,
 * so that:
 *
 *  1. We don't waste cycles converting back and forth (generally, if
 *     we have a PAL errno, we had a platform errno just a few
 *     instructions ago.)
 *
 *  2. We don't lose the ability to get the system error message for
 *     non-standard, platform-specific errors.
 *
 * Note that buffer may or may not be used and the error message is
 * passed back via the return value.
 *
 * If the buffer was too small to fit the full message, null is
 * returned and the buffer is filled with as much of the message
 * as possible and null-terminated.
 */
extern "C" const char* StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize);
