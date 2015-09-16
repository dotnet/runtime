// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_config.h"
#include "pal_errno.h"
#include "pal_utilities.h"

#include <errno.h>
#include <string.h>
#include <assert.h>

extern "C" Error ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0:
            return PAL_SUCCESS;
        case E2BIG:
            return PAL_E2BIG;
        case EACCES:
            return PAL_EACCES;
        case EADDRINUSE:
            return PAL_EADDRINUSE;
        case EADDRNOTAVAIL:
            return PAL_EADDRNOTAVAIL;
        case EAFNOSUPPORT:
            return PAL_EAFNOSUPPORT;
        case EAGAIN:
            return PAL_EAGAIN;
        case EALREADY:
            return PAL_EALREADY;
        case EBADF:
            return PAL_EBADF;
        case EBADMSG:
            return PAL_EBADMSG;
        case EBUSY:
            return PAL_EBUSY;
        case ECANCELED:
            return PAL_ECANCELED;
        case ECHILD:
            return PAL_ECHILD;
        case ECONNABORTED:
            return PAL_ECONNABORTED;
        case ECONNREFUSED:
            return PAL_ECONNREFUSED;
        case ECONNRESET:
            return PAL_ECONNRESET;
        case EDEADLK:
            return PAL_EDEADLK;
        case EDESTADDRREQ:
            return PAL_EDESTADDRREQ;
        case EDOM:
            return PAL_EDOM;
        case EDQUOT:
            return PAL_EDQUOT;
        case EEXIST:
            return PAL_EEXIST;
        case EFAULT:
            return PAL_EFAULT;
        case EFBIG:
            return PAL_EFBIG;
        case EHOSTUNREACH:
            return PAL_EHOSTUNREACH;
        case EIDRM:
            return PAL_EIDRM;
        case EILSEQ:
            return PAL_EILSEQ;
        case EINPROGRESS:
            return PAL_EINPROGRESS;
        case EINTR:
            return PAL_EINTR;
        case EINVAL:
            return PAL_EINVAL;
        case EIO:
            return PAL_EIO;
        case EISCONN:
            return PAL_EISCONN;
        case EISDIR:
            return PAL_EISDIR;
        case ELOOP:
            return PAL_ELOOP;
        case EMFILE:
            return PAL_EMFILE;
        case EMLINK:
            return PAL_EMLINK;
        case EMSGSIZE:
            return PAL_EMSGSIZE;
        case EMULTIHOP:
            return PAL_EMULTIHOP;
        case ENAMETOOLONG:
            return PAL_ENAMETOOLONG;
        case ENETDOWN:
            return PAL_ENETDOWN;
        case ENETRESET:
            return PAL_ENETRESET;
        case ENETUNREACH:
            return PAL_ENETUNREACH;
        case ENFILE:
            return PAL_ENFILE;
        case ENOBUFS:
            return PAL_ENOBUFS;
        case ENODEV:
            return PAL_ENODEV;
        case ENOENT:
            return PAL_ENOENT;
        case ENOEXEC:
            return PAL_ENOEXEC;
        case ENOLCK:
            return PAL_ENOLCK;
        case ENOLINK:
            return PAL_ENOLINK;
        case ENOMEM:
            return PAL_ENOMEM;
        case ENOMSG:
            return PAL_ENOMSG;
        case ENOPROTOOPT:
            return PAL_ENOPROTOOPT;
        case ENOSPC:
            return PAL_ENOSPC;
        case ENOSYS:
            return PAL_ENOSYS;
        case ENOTCONN:
            return PAL_ENOTCONN;
        case ENOTDIR:
            return PAL_ENOTDIR;
        case ENOTEMPTY:
            return PAL_ENOTEMPTY;
        case ENOTRECOVERABLE:
            return PAL_ENOTRECOVERABLE;
        case ENOTSOCK:
            return PAL_ENOTSOCK;
        case ENOTSUP:
            return PAL_ENOTSUP;
        case ENOTTY:
            return PAL_ENOTTY;
        case ENXIO:
            return PAL_ENXIO;
        case EOVERFLOW:
            return PAL_EOVERFLOW;
        case EOWNERDEAD:
            return PAL_EOWNERDEAD;
        case EPERM:
            return PAL_EPERM;
        case EPIPE:
            return PAL_EPIPE;
        case EPROTO:
            return PAL_EPROTO;
        case EPROTONOSUPPORT:
            return PAL_EPROTONOSUPPORT;
        case EPROTOTYPE:
            return PAL_EPROTOTYPE;
        case ERANGE:
            return PAL_ERANGE;
        case EROFS:
            return PAL_EROFS;
        case ESPIPE:
            return PAL_ESPIPE;
        case ESRCH:
            return PAL_ESRCH;
        case ESTALE:
            return PAL_ESTALE;
        case ETIMEDOUT:
            return PAL_ETIMEDOUT;
        case ETXTBSY:
            return PAL_ETXTBSY;
        case EXDEV:
            return PAL_EXDEV;

// #if because these will trigger duplicate case label warnings when
// they have the same value, which is permitted by POSIX and common.
#if EOPNOTSUPP != ENOTSUP
        case EOPNOTSUPP:
            return PAL_EOPNOTSUPP;
#endif
#if EWOULDBLOCK != EAGAIN
        case EWOULDBLOCK:
            return PAL_EWOULDBLOCK;
#endif
    }

    return PAL_ENONSTANDARD;
}

extern "C" int32_t ConvertErrorPalToPlatform(Error error)
{
    switch (error)
    {
        case PAL_SUCCESS:
            return 0;
        case PAL_E2BIG:
            return E2BIG;
        case PAL_EACCES:
            return EACCES;
        case PAL_EADDRINUSE:
            return EADDRINUSE;
        case PAL_EADDRNOTAVAIL:
            return EADDRNOTAVAIL;
        case PAL_EAFNOSUPPORT:
            return EAFNOSUPPORT;
        case PAL_EAGAIN:
            return EAGAIN;
        case PAL_EALREADY:
            return EALREADY;
        case PAL_EBADF:
            return EBADF;
        case PAL_EBADMSG:
            return EBADMSG;
        case PAL_EBUSY:
            return EBUSY;
        case PAL_ECANCELED:
            return ECANCELED;
        case PAL_ECHILD:
            return ECHILD;
        case PAL_ECONNABORTED:
            return ECONNABORTED;
        case PAL_ECONNREFUSED:
            return ECONNREFUSED;
        case PAL_ECONNRESET:
            return ECONNRESET;
        case PAL_EDEADLK:
            return EDEADLK;
        case PAL_EDESTADDRREQ:
            return EDESTADDRREQ;
        case PAL_EDOM:
            return EDOM;
        case PAL_EDQUOT:
            return EDQUOT;
        case PAL_EEXIST:
            return EEXIST;
        case PAL_EFAULT:
            return EFAULT;
        case PAL_EFBIG:
            return EFBIG;
        case PAL_EHOSTUNREACH:
            return EHOSTUNREACH;
        case PAL_EIDRM:
            return EIDRM;
        case PAL_EILSEQ:
            return EILSEQ;
        case PAL_EINPROGRESS:
            return EINPROGRESS;
        case PAL_EINTR:
            return EINTR;
        case PAL_EINVAL:
            return EINVAL;
        case PAL_EIO:
            return EIO;
        case PAL_EISCONN:
            return EISCONN;
        case PAL_EISDIR:
            return EISDIR;
        case PAL_ELOOP:
            return ELOOP;
        case PAL_EMFILE:
            return EMFILE;
        case PAL_EMLINK:
            return EMLINK;
        case PAL_EMSGSIZE:
            return EMSGSIZE;
        case PAL_EMULTIHOP:
            return EMULTIHOP;
        case PAL_ENAMETOOLONG:
            return ENAMETOOLONG;
        case PAL_ENETDOWN:
            return ENETDOWN;
        case PAL_ENETRESET:
            return ENETRESET;
        case PAL_ENETUNREACH:
            return ENETUNREACH;
        case PAL_ENFILE:
            return ENFILE;
        case PAL_ENOBUFS:
            return ENOBUFS;
        case PAL_ENODEV:
            return ENODEV;
        case PAL_ENOENT:
            return ENOENT;
        case PAL_ENOEXEC:
            return ENOEXEC;
        case PAL_ENOLCK:
            return ENOLCK;
        case PAL_ENOLINK:
            return ENOLINK;
        case PAL_ENOMEM:
            return ENOMEM;
        case PAL_ENOMSG:
            return ENOMSG;
        case PAL_ENOPROTOOPT:
            return ENOPROTOOPT;
        case PAL_ENOSPC:
            return ENOSPC;
        case PAL_ENOSYS:
            return ENOSYS;
        case PAL_ENOTCONN:
            return ENOTCONN;
        case PAL_ENOTDIR:
            return ENOTDIR;
        case PAL_ENOTEMPTY:
            return ENOTEMPTY;
        case PAL_ENOTRECOVERABLE:
            return ENOTRECOVERABLE;
        case PAL_ENOTSOCK:
            return ENOTSOCK;
        case PAL_ENOTSUP:
            return ENOTSUP;
        case PAL_ENOTTY:
            return ENOTTY;
        case PAL_ENXIO:
            return ENXIO;
        case PAL_EOVERFLOW:
            return EOVERFLOW;
        case PAL_EOWNERDEAD:
            return EOWNERDEAD;
        case PAL_EPERM:
            return EPERM;
        case PAL_EPIPE:
            return EPIPE;
        case PAL_EPROTO:
            return EPROTO;
        case PAL_EPROTONOSUPPORT:
            return EPROTONOSUPPORT;
        case PAL_EPROTOTYPE:
            return EPROTOTYPE;
        case PAL_ERANGE:
            return ERANGE;
        case PAL_EROFS:
            return EROFS;
        case PAL_ESPIPE:
            return ESPIPE;
        case PAL_ESRCH:
            return ESRCH;
        case PAL_ESTALE:
            return ESTALE;
        case PAL_ETIMEDOUT:
            return ETIMEDOUT;
        case PAL_ETXTBSY:
            return ETXTBSY;
        case PAL_EXDEV:
            return EXDEV;
        case PAL_ENONSTANDARD:
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
    assert(false && "Unknown error code");
    return -1;
}

extern "C" const char* StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr);
    assert(bufferSize > 0);

    if (bufferSize < 0)
        return nullptr;

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
    const char* message = strerror_r(platformErrno, buffer, UnsignedCast(bufferSize));
    assert(message != nullptr);
    return message;
#else
    int error = strerror_r(platformErrno, buffer, UnsignedCast(bufferSize));
    if (error == ERANGE)
    {
        // Buffer is too small to hold the entire message, but has
        // still been filled to the extent possible and null-terminated.
        return nullptr;
    }

    // The only other valid error codes are 0 for success or EINVAL for
    // an unkown error, but in the latter case a reasonable string (e.g
    // "Unknown error: 0x123") is returned.
    assert(error == 0 || error == EINVAL);
    return buffer;
#endif
}
