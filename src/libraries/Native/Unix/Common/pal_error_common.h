// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "pal_compiler.h"
#include "pal_types.h"
#include "pal_config.h"
#include "pal_utilities.h"
#include <string.h>
#include <assert.h>
#include <errno.h>
#include <netdb.h>

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
typedef enum
{
    Error_SUCCESS = 0,

    Error_E2BIG = 0x10001,           // Argument list too long.
    Error_EACCES = 0x10002,          // Permission denied.
    Error_EADDRINUSE = 0x10003,      // Address in use.
    Error_EADDRNOTAVAIL = 0x10004,   // Address not available.
    Error_EAFNOSUPPORT = 0x10005,    // Address family not supported.
    Error_EAGAIN = 0x10006,          // Resource unavailable, try again (same value as EWOULDBLOCK),
    Error_EALREADY = 0x10007,        // Connection already in progress.
    Error_EBADF = 0x10008,           // Bad file descriptor.
    Error_EBADMSG = 0x10009,         // Bad message.
    Error_EBUSY = 0x1000A,           // Device or resource busy.
    Error_ECANCELED = 0x1000B,       // Operation canceled.
    Error_ECHILD = 0x1000C,          // No child processes.
    Error_ECONNABORTED = 0x1000D,    // Connection aborted.
    Error_ECONNREFUSED = 0x1000E,    // Connection refused.
    Error_ECONNRESET = 0x1000F,      // Connection reset.
    Error_EDEADLK = 0x10010,         // Resource deadlock would occur.
    Error_EDESTADDRREQ = 0x10011,    // Destination address required.
    Error_EDOM = 0x10012,            // Mathematics argument out of domain of function.
    Error_EDQUOT = 0x10013,          // Reserved.
    Error_EEXIST = 0x10014,          // File exists.
    Error_EFAULT = 0x10015,          // Bad address.
    Error_EFBIG = 0x10016,           // File too large.
    Error_EHOSTUNREACH = 0x10017,    // Host is unreachable.
    Error_EIDRM = 0x10018,           // Identifier removed.
    Error_EILSEQ = 0x10019,          // Illegal byte sequence.
    Error_EINPROGRESS = 0x1001A,     // Operation in progress.
    Error_EINTR = 0x1001B,           // Interrupted function.
    Error_EINVAL = 0x1001C,          // Invalid argument.
    Error_EIO = 0x1001D,             // I/O error.
    Error_EISCONN = 0x1001E,         // Socket is connected.
    Error_EISDIR = 0x1001F,          // Is a directory.
    Error_ELOOP = 0x10020,           // Too many levels of symbolic links.
    Error_EMFILE = 0x10021,          // File descriptor value too large.
    Error_EMLINK = 0x10022,          // Too many links.
    Error_EMSGSIZE = 0x10023,        // Message too large.
    Error_EMULTIHOP = 0x10024,       // Reserved.
    Error_ENAMETOOLONG = 0x10025,    // Filename too long.
    Error_ENETDOWN = 0x10026,        // Network is down.
    Error_ENETRESET = 0x10027,       // Connection aborted by network.
    Error_ENETUNREACH = 0x10028,     // Network unreachable.
    Error_ENFILE = 0x10029,          // Too many files open in system.
    Error_ENOBUFS = 0x1002A,         // No buffer space available.
    Error_ENODEV = 0x1002C,          // No such device.
    Error_ENOENT = 0x1002D,          // No such file or directory.
    Error_ENOEXEC = 0x1002E,         // Executable file format error.
    Error_ENOLCK = 0x1002F,          // No locks available.
    Error_ENOLINK = 0x10030,         // Reserved.
    Error_ENOMEM = 0x10031,          // Not enough space.
    Error_ENOMSG = 0x10032,          // No message of the desired type.
    Error_ENOPROTOOPT = 0x10033,     // Protocol not available.
    Error_ENOSPC = 0x10034,          // No space left on device.
    Error_ENOSYS = 0x10037,          // Function not supported.
    Error_ENOTCONN = 0x10038,        // The socket is not connected.
    Error_ENOTDIR = 0x10039,         // Not a directory or a symbolic link to a directory.
    Error_ENOTEMPTY = 0x1003A,       // Directory not empty.
    Error_ENOTRECOVERABLE = 0x1003B, // State not recoverable.
    Error_ENOTSOCK = 0x1003C,        // Not a socket.
    Error_ENOTSUP = 0x1003D,         // Not supported (same value as EOPNOTSUP).
    Error_ENOTTY = 0x1003E,          // Inappropriate I/O control operation.
    Error_ENXIO = 0x1003F,           // No such device or address.
    Error_EOVERFLOW = 0x10040,       // Value too large to be stored in data type.
    Error_EOWNERDEAD = 0x10041,      // Previous owner died.
    Error_EPERM = 0x10042,           // Operation not permitted.
    Error_EPIPE = 0x10043,           // Broken pipe.
    Error_EPROTO = 0x10044,          // Protocol error.
    Error_EPROTONOSUPPORT = 0x10045, // Protocol not supported.
    Error_EPROTOTYPE = 0x10046,      // Protocol wrong type for socket.
    Error_ERANGE = 0x10047,          // Result too large.
    Error_EROFS = 0x10048,           // Read-only file system.
    Error_ESPIPE = 0x10049,          // Invalid seek.
    Error_ESRCH = 0x1004A,           // No such process.
    Error_ESTALE = 0x1004B,          // Reserved.
    Error_ETIMEDOUT = 0x1004D,       // Connection timed out.
    Error_ETXTBSY = 0x1004E,         // Text file busy.
    Error_EXDEV = 0x1004F,           // Cross-device link.
    Error_ESOCKTNOSUPPORT = 0x1005E, // Socket type not supported.
    Error_EPFNOSUPPORT = 0x10060,    // Protocol family not supported.
    Error_ESHUTDOWN = 0x1006C,       // Socket shutdown.
    Error_EHOSTDOWN = 0x10070,       // Host is down.
    Error_ENODATA = 0x10071,         // No data available.

    // Error codes to track errors beyond kernel.
    Error_EHOSTNOTFOUND = 0x20001,   // Name lookup failed.
    Error_ESOCKETERROR = 0x20002,    // Unidentified socket error.

    // POSIX permits these to have the same value and we make them
    // always equal so that we cannot introduce a dependency on
    // distinguishing between them that would not work on all
    // platforms.
    Error_EOPNOTSUPP = Error_ENOTSUP, // Operation not supported on socket
    Error_EWOULDBLOCK = Error_EAGAIN, // Operation would block

    // This one is not part of POSIX, but is a catch-all for the case
    // where we cannot convert the raw errno value to something above.
    Error_ENONSTANDARD = 0x1FFFF,
} Error;

/*
  Some pal errors don't have a corresponding errno value.
  We define values for these errors.
  We want these values to be distinct from real errno values.
  We base of the Error enum values which are chosen to be out of the
  typical errno range, and make them negative because POSIX
  requires errno values to be positive.
*/
#define EHOSTNOTFOUND (-Error_EHOSTNOTFOUND)
#define ESOCKETERROR  (-Error_ESOCKETERROR)

inline static int32_t ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0:
            return Error_SUCCESS;
        case E2BIG:
            return Error_E2BIG;
        case EACCES:
            return Error_EACCES;
        case EADDRINUSE:
            return Error_EADDRINUSE;
        case EADDRNOTAVAIL:
            return Error_EADDRNOTAVAIL;
        case EAFNOSUPPORT:
            return Error_EAFNOSUPPORT;
        case EAGAIN:
            return Error_EAGAIN;
        case EALREADY:
            return Error_EALREADY;
        case EBADF:
            return Error_EBADF;
        case EBADMSG:
            return Error_EBADMSG;
        case EBUSY:
            return Error_EBUSY;
        case ECANCELED:
            return Error_ECANCELED;
        case ECHILD:
            return Error_ECHILD;
        case ECONNABORTED:
            return Error_ECONNABORTED;
        case ECONNREFUSED:
            return Error_ECONNREFUSED;
        case ECONNRESET:
            return Error_ECONNRESET;
        case EDEADLK:
            return Error_EDEADLK;
        case EDESTADDRREQ:
            return Error_EDESTADDRREQ;
        case EDOM:
            return Error_EDOM;
        case EDQUOT:
            return Error_EDQUOT;
        case EEXIST:
            return Error_EEXIST;
        case EFAULT:
            return Error_EFAULT;
        case EFBIG:
            return Error_EFBIG;
        case EHOSTUNREACH:
            return Error_EHOSTUNREACH;
        case EIDRM:
            return Error_EIDRM;
        case EILSEQ:
            return Error_EILSEQ;
        case EINPROGRESS:
            return Error_EINPROGRESS;
        case EINTR:
            return Error_EINTR;
        case EINVAL:
            return Error_EINVAL;
        case EIO:
            return Error_EIO;
        case EISCONN:
            return Error_EISCONN;
        case EISDIR:
            return Error_EISDIR;
        case ELOOP:
            return Error_ELOOP;
        case EMFILE:
            return Error_EMFILE;
        case EMLINK:
            return Error_EMLINK;
        case EMSGSIZE:
            return Error_EMSGSIZE;
        case EMULTIHOP:
            return Error_EMULTIHOP;
        case ENAMETOOLONG:
            return Error_ENAMETOOLONG;
        case ENETDOWN:
            return Error_ENETDOWN;
        case ENETRESET:
            return Error_ENETRESET;
        case ENETUNREACH:
            return Error_ENETUNREACH;
        case ENFILE:
            return Error_ENFILE;
        case ENOBUFS:
            return Error_ENOBUFS;
        case ENODEV:
            return Error_ENODEV;
        case ENOENT:
            return Error_ENOENT;
        case ENOEXEC:
            return Error_ENOEXEC;
        case ENOLCK:
            return Error_ENOLCK;
        case ENOLINK:
            return Error_ENOLINK;
        case ENOMEM:
            return Error_ENOMEM;
        case ENOMSG:
            return Error_ENOMSG;
        case ENOPROTOOPT:
            return Error_ENOPROTOOPT;
        case ENOSPC:
            return Error_ENOSPC;
        case ENOSYS:
            return Error_ENOSYS;
        case ENOTCONN:
            return Error_ENOTCONN;
        case ENOTDIR:
            return Error_ENOTDIR;
#if ENOTEMPTY != EEXIST // AIX defines this
        case ENOTEMPTY:
            return Error_ENOTEMPTY;
#endif
#ifdef ENOTRECOVERABLE // not available in NetBSD
        case ENOTRECOVERABLE:
            return Error_ENOTRECOVERABLE;
#endif
        case ENOTSOCK:
            return Error_ENOTSOCK;
        case ENOTSUP:
            return Error_ENOTSUP;
        case ENOTTY:
            return Error_ENOTTY;
        case ENXIO:
            return Error_ENXIO;
        case EOVERFLOW:
            return Error_EOVERFLOW;
#ifdef EOWNERDEAD // not available in NetBSD
        case EOWNERDEAD:
            return Error_EOWNERDEAD;
#endif
        case EPERM:
            return Error_EPERM;
        case EPIPE:
            return Error_EPIPE;
        case EPROTO:
            return Error_EPROTO;
        case EPROTONOSUPPORT:
            return Error_EPROTONOSUPPORT;
        case EPROTOTYPE:
            return Error_EPROTOTYPE;
        case ERANGE:
            return Error_ERANGE;
        case EROFS:
            return Error_EROFS;
        case ESPIPE:
            return Error_ESPIPE;
        case ESRCH:
            return Error_ESRCH;
        case ESTALE:
            return Error_ESTALE;
        case ETIMEDOUT:
            return Error_ETIMEDOUT;
        case ETXTBSY:
            return Error_ETXTBSY;
        case EXDEV:
            return Error_EXDEV;
#ifdef ESOCKTNOSUPPORT
        case ESOCKTNOSUPPORT:
            return Error_ESOCKTNOSUPPORT;
#endif
        case EPFNOSUPPORT:
            return Error_EPFNOSUPPORT;
        case ESHUTDOWN:
            return Error_ESHUTDOWN;
        case EHOSTDOWN:
            return Error_EHOSTDOWN;
#ifdef ENODATA // not available in FreeBSD
        case ENODATA:
            return Error_ENODATA;
#endif

// #if because these will trigger duplicate case label warnings when
// they have the same value, which is permitted by POSIX and common.
#if EOPNOTSUPP != ENOTSUP
        case EOPNOTSUPP:
            return Error_EOPNOTSUPP;
#endif
#if EWOULDBLOCK != EAGAIN
        case EWOULDBLOCK:
            return Error_EWOULDBLOCK;
#endif
    }

    return Error_ENONSTANDARD;
}

inline static int32_t ConvertErrorPalToPlatform(int32_t error)
{
    switch (error)
    {
        case Error_SUCCESS:
            return 0;
        case Error_E2BIG:
            return E2BIG;
        case Error_EACCES:
            return EACCES;
        case Error_EADDRINUSE:
            return EADDRINUSE;
        case Error_EADDRNOTAVAIL:
            return EADDRNOTAVAIL;
        case Error_EAFNOSUPPORT:
            return EAFNOSUPPORT;
        case Error_EAGAIN:
            return EAGAIN;
        case Error_EALREADY:
            return EALREADY;
        case Error_EBADF:
            return EBADF;
        case Error_EBADMSG:
            return EBADMSG;
        case Error_EBUSY:
            return EBUSY;
        case Error_ECANCELED:
            return ECANCELED;
        case Error_ECHILD:
            return ECHILD;
        case Error_ECONNABORTED:
            return ECONNABORTED;
        case Error_ECONNREFUSED:
            return ECONNREFUSED;
        case Error_ECONNRESET:
            return ECONNRESET;
        case Error_EDEADLK:
            return EDEADLK;
        case Error_EDESTADDRREQ:
            return EDESTADDRREQ;
        case Error_EDOM:
            return EDOM;
        case Error_EDQUOT:
            return EDQUOT;
        case Error_EEXIST:
            return EEXIST;
        case Error_EFAULT:
            return EFAULT;
        case Error_EFBIG:
            return EFBIG;
        case Error_EHOSTUNREACH:
            return EHOSTUNREACH;
        case Error_EIDRM:
            return EIDRM;
        case Error_EILSEQ:
            return EILSEQ;
        case Error_EINPROGRESS:
            return EINPROGRESS;
        case Error_EINTR:
            return EINTR;
        case Error_EINVAL:
            return EINVAL;
        case Error_EIO:
            return EIO;
        case Error_EISCONN:
            return EISCONN;
        case Error_EISDIR:
            return EISDIR;
        case Error_ELOOP:
            return ELOOP;
        case Error_EMFILE:
            return EMFILE;
        case Error_EMLINK:
            return EMLINK;
        case Error_EMSGSIZE:
            return EMSGSIZE;
        case Error_EMULTIHOP:
            return EMULTIHOP;
        case Error_ENAMETOOLONG:
            return ENAMETOOLONG;
        case Error_ENETDOWN:
            return ENETDOWN;
        case Error_ENETRESET:
            return ENETRESET;
        case Error_ENETUNREACH:
            return ENETUNREACH;
        case Error_ENFILE:
            return ENFILE;
        case Error_ENOBUFS:
            return ENOBUFS;
        case Error_ENODEV:
            return ENODEV;
        case Error_ENOENT:
            return ENOENT;
        case Error_ENOEXEC:
            return ENOEXEC;
        case Error_ENOLCK:
            return ENOLCK;
        case Error_ENOLINK:
            return ENOLINK;
        case Error_ENOMEM:
            return ENOMEM;
        case Error_ENOMSG:
            return ENOMSG;
        case Error_ENOPROTOOPT:
            return ENOPROTOOPT;
        case Error_ENOSPC:
            return ENOSPC;
        case Error_ENOSYS:
            return ENOSYS;
        case Error_ENOTCONN:
            return ENOTCONN;
        case Error_ENOTDIR:
            return ENOTDIR;
        case Error_ENOTEMPTY:
            return ENOTEMPTY;
#ifdef ENOTRECOVERABLE // not available in NetBSD
        case Error_ENOTRECOVERABLE:
            return ENOTRECOVERABLE;
#endif
        case Error_ENOTSOCK:
            return ENOTSOCK;
        case Error_ENOTSUP:
            return ENOTSUP;
        case Error_ENOTTY:
            return ENOTTY;
        case Error_ENXIO:
            return ENXIO;
        case Error_EOVERFLOW:
            return EOVERFLOW;
#ifdef EOWNERDEAD // not available in NetBSD
        case Error_EOWNERDEAD:
            return EOWNERDEAD;
#endif
        case Error_EPERM:
            return EPERM;
        case Error_EPIPE:
            return EPIPE;
        case Error_EPROTO:
            return EPROTO;
        case Error_EPROTONOSUPPORT:
            return EPROTONOSUPPORT;
        case Error_EPROTOTYPE:
            return EPROTOTYPE;
        case Error_ERANGE:
            return ERANGE;
        case Error_EROFS:
            return EROFS;
        case Error_ESPIPE:
            return ESPIPE;
        case Error_ESRCH:
            return ESRCH;
        case Error_ESTALE:
            return ESTALE;
        case Error_ETIMEDOUT:
            return ETIMEDOUT;
        case Error_ETXTBSY:
            return ETXTBSY;
        case Error_EXDEV:
            return EXDEV;
        case Error_EPFNOSUPPORT:
            return EPFNOSUPPORT;
#ifdef ESOCKTNOSUPPORT
        case Error_ESOCKTNOSUPPORT:
            return ESOCKTNOSUPPORT;
#endif
        case Error_ESHUTDOWN:
            return ESHUTDOWN;
        case Error_EHOSTDOWN:
            return EHOSTDOWN;
#ifdef ENODATA // not available in FreeBSD
        case Error_ENODATA:
            return ENODATA;
#endif
        case Error_EHOSTNOTFOUND:
            return EHOSTNOTFOUND;
        case Error_ESOCKETERROR:
            return ESOCKETERROR;

        case Error_ENONSTANDARD:
            break; // fall through to assert
    }

    // We should not use this function to round-trip platform -> pal
    // -> platform. It's here only to synthesize a platform number
    // from the fixed set above. Note that the assert is outside the
    // switch rather than in a default case block because not
    // having a default will trigger a warning (as error) if there's
    // an enum value we haven't handled. Should that trigger, make
    // note that there is probably a corresponding missing case in the
    // other direction above, but the compiler can't warn in that case
    // because the platform values are not part of an enum.
    assert_err(false, "Unknown error code", (int) error);
    return -1;
}

static bool TryConvertErrorToGai(int32_t error, int32_t* gaiError)
{
    assert(gaiError != NULL);

    switch (error)
    {
        case EHOSTNOTFOUND:
            *gaiError = EAI_NONAME;
            return true;
        default:
            *gaiError = error;
            return false;
    }
}



inline static const char* StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    assert(buffer != NULL);
    assert(bufferSize > 0);

    if (bufferSize < 0)
        return NULL;

    if (platformErrno < 0)
    {
        // Not a system error
        int32_t gaiError;
        if (TryConvertErrorToGai(platformErrno, &gaiError))
        {
            SafeStringCopy(buffer, (size_t)bufferSize, gai_strerror(gaiError));
            return buffer;
        }
        else if (platformErrno == ESOCKETERROR)
        {
            SafeStringCopy(buffer, (size_t)bufferSize, "Unknown socket error");
            return buffer;
        }
    }

// Note that we must use strerror_r because plain strerror is not
// thread-safe.
//
// However, there are two versions of strerror_r:
//    - GNU:   char* strerror_r(int, char*, size_t);
//    - POSIX: int   strerror_r(int, char*, size_t);
//
// The former may or may not use the supplied buffer, and returns
// the error message string. The latter stores the error message
// string into the supplied buffer and returns an error code.

#if HAVE_GNU_STRERROR_R
    const char* message = strerror_r(platformErrno, buffer, (uint32_t) bufferSize);
    assert(message != NULL);
    return message;
#else
    int error = strerror_r(platformErrno, buffer, (uint32_t) bufferSize);
    if (error == ERANGE)
    {
        // Buffer is too small to hold the entire message, but has
        // still been filled to the extent possible and null-terminated.
        return NULL;
    }

    // The only other valid error codes are 0 for success or EINVAL for
    // an unknown error, but in the latter case a reasonable string (e.g
    // "Unknown error: 0x123") is returned.
    assert_err(error == 0 || error == EINVAL, "invalid error", error);
    return buffer;
#endif
}
