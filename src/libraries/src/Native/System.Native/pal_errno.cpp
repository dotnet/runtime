// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "../config.h"
#include "pal_errno.h"

#include <errno.h>
#include <string.h>
#include <assert.h>

extern "C"
Error ConvertErrorPlatformToPal(int32_t platformErrno)
{
    switch (platformErrno)
    {
        case 0:                return Error::PAL_SUCCESS;
        case E2BIG:            return Error::PAL_E2BIG;
        case EACCES:           return Error::PAL_EACCES;
        case EADDRINUSE:       return Error::PAL_EADDRINUSE;
        case EADDRNOTAVAIL:    return Error::PAL_EADDRNOTAVAIL;
        case EAFNOSUPPORT:     return Error::PAL_EAFNOSUPPORT;
        case EAGAIN:           return Error::PAL_EAGAIN;
        case EALREADY:         return Error::PAL_EALREADY;
        case EBADF:            return Error::PAL_EBADF;
        case EBADMSG:          return Error::PAL_EBADMSG;
        case EBUSY:            return Error::PAL_EBUSY;
        case ECANCELED:        return Error::PAL_ECANCELED;
        case ECHILD:           return Error::PAL_ECHILD;
        case ECONNABORTED:     return Error::PAL_ECONNABORTED;
        case ECONNREFUSED:     return Error::PAL_ECONNREFUSED;
        case ECONNRESET:       return Error::PAL_ECONNRESET;
        case EDEADLK:          return Error::PAL_EDEADLK;
        case EDESTADDRREQ:     return Error::PAL_EDESTADDRREQ;
        case EDOM:             return Error::PAL_EDOM;
        case EDQUOT:           return Error::PAL_EDQUOT;
        case EEXIST:           return Error::PAL_EEXIST;
        case EFAULT:           return Error::PAL_EFAULT;
        case EFBIG:            return Error::PAL_EFBIG;
        case EHOSTUNREACH:     return Error::PAL_EHOSTUNREACH;
        case EIDRM:            return Error::PAL_EIDRM;
        case EILSEQ:           return Error::PAL_EILSEQ;
        case EINPROGRESS:      return Error::PAL_EINPROGRESS;
        case EINTR:            return Error::PAL_EINTR;
        case EINVAL:           return Error::PAL_EINVAL;
        case EIO:              return Error::PAL_EIO;
        case EISCONN:          return Error::PAL_EISCONN;
        case EISDIR:           return Error::PAL_EISDIR;
        case ELOOP:            return Error::PAL_ELOOP;
        case EMFILE:           return Error::PAL_EMFILE;
        case EMLINK:           return Error::PAL_EMLINK;
        case EMSGSIZE:         return Error::PAL_EMSGSIZE;
        case EMULTIHOP:        return Error::PAL_EMULTIHOP;
        case ENAMETOOLONG:     return Error::PAL_ENAMETOOLONG;
        case ENETDOWN:         return Error::PAL_ENETDOWN;
        case ENETRESET:        return Error::PAL_ENETRESET;
        case ENETUNREACH:      return Error::PAL_ENETUNREACH;
        case ENFILE:           return Error::PAL_ENFILE;
        case ENOBUFS:          return Error::PAL_ENOBUFS;
        case ENODEV:           return Error::PAL_ENODEV;
        case ENOENT:           return Error::PAL_ENOENT;
        case ENOEXEC:          return Error::PAL_ENOEXEC;
        case ENOLCK:           return Error::PAL_ENOLCK;
        case ENOLINK:          return Error::PAL_ENOLINK;
        case ENOMEM:           return Error::PAL_ENOMEM;
        case ENOMSG:           return Error::PAL_ENOMSG;
        case ENOPROTOOPT:      return Error::PAL_ENOPROTOOPT;
        case ENOSPC:           return Error::PAL_ENOSPC;
        case ENOSYS:           return Error::PAL_ENOSYS;
        case ENOTCONN:         return Error::PAL_ENOTCONN;
        case ENOTDIR:          return Error::PAL_ENOTDIR;
        case ENOTEMPTY:        return Error::PAL_ENOTEMPTY;
        case ENOTRECOVERABLE:  return Error::PAL_ENOTRECOVERABLE;
        case ENOTSOCK:         return Error::PAL_ENOTSOCK;
        case ENOTSUP:          return Error::PAL_ENOTSUP;
        case ENOTTY:           return Error::PAL_ENOTTY;
        case ENXIO:            return Error::PAL_ENXIO;
        case EOVERFLOW:        return Error::PAL_EOVERFLOW;
        case EOWNERDEAD:       return Error::PAL_EOWNERDEAD;
        case EPERM:            return Error::PAL_EPERM;
        case EPIPE:            return Error::PAL_EPIPE;
        case EPROTO:           return Error::PAL_EPROTO;
        case EPROTONOSUPPORT:  return Error::PAL_EPROTONOSUPPORT;
        case EPROTOTYPE:       return Error::PAL_EPROTOTYPE;
        case ERANGE:           return Error::PAL_ERANGE;
        case EROFS:            return Error::PAL_EROFS;
        case ESPIPE:           return Error::PAL_ESPIPE;
        case ESRCH:            return Error::PAL_ESRCH;
        case ESTALE:           return Error::PAL_ESTALE;
        case ETIMEDOUT:        return Error::PAL_ETIMEDOUT;
        case ETXTBSY:          return Error::PAL_ETXTBSY;
        case EXDEV:            return Error::PAL_EXDEV;

// #if because these will trigger duplicate case label warnings when
// they have the same value, which is permitted by POSIX and common.
#if EOPNOTSUPP != ENOTSUP
        case EOPNOTSUPP:       return Error::PAL_EOPNOTSUPP;
#endif
#if EWOULDBLOCK != EAGAIN
        case EWOULDBLOCK:      return Error::PAL_EWOULDBLOCK;
#endif
    }

    return Error::PAL_ENONSTANDARD;
}

extern "C"
int32_t ConvertErrorPalToPlatform(Error error)
{
    switch (error)
    {
        case Error::PAL_SUCCESS:          return 0;
        case Error::PAL_E2BIG:            return E2BIG;
        case Error::PAL_EACCES:           return EACCES;
        case Error::PAL_EADDRINUSE:       return EADDRINUSE;
        case Error::PAL_EADDRNOTAVAIL:    return EADDRNOTAVAIL;
        case Error::PAL_EAFNOSUPPORT:     return EAFNOSUPPORT;
        case Error::PAL_EAGAIN:           return EAGAIN;
        case Error::PAL_EALREADY:         return EALREADY;
        case Error::PAL_EBADF:            return EBADF;
        case Error::PAL_EBADMSG:          return EBADMSG;
        case Error::PAL_EBUSY:            return EBUSY;
        case Error::PAL_ECANCELED:        return ECANCELED;
        case Error::PAL_ECHILD:           return ECHILD;
        case Error::PAL_ECONNABORTED:     return ECONNABORTED;
        case Error::PAL_ECONNREFUSED:     return ECONNREFUSED;
        case Error::PAL_ECONNRESET:       return ECONNRESET;
        case Error::PAL_EDEADLK:          return EDEADLK;
        case Error::PAL_EDESTADDRREQ:     return EDESTADDRREQ;
        case Error::PAL_EDOM:             return EDOM;
        case Error::PAL_EDQUOT:           return EDQUOT;
        case Error::PAL_EEXIST:           return EEXIST;
        case Error::PAL_EFAULT:           return EFAULT;
        case Error::PAL_EFBIG:            return EFBIG;
        case Error::PAL_EHOSTUNREACH:     return EHOSTUNREACH;
        case Error::PAL_EIDRM:            return EIDRM;
        case Error::PAL_EILSEQ:           return EILSEQ;
        case Error::PAL_EINPROGRESS:      return EINPROGRESS;
        case Error::PAL_EINTR:            return EINTR;
        case Error::PAL_EINVAL:           return EINVAL;
        case Error::PAL_EIO:              return EIO;
        case Error::PAL_EISCONN:          return EISCONN;
        case Error::PAL_EISDIR:           return EISDIR;
        case Error::PAL_ELOOP:            return ELOOP;
        case Error::PAL_EMFILE:           return EMFILE;
        case Error::PAL_EMLINK:           return EMLINK;
        case Error::PAL_EMSGSIZE:         return EMSGSIZE;
        case Error::PAL_EMULTIHOP:        return EMULTIHOP;
        case Error::PAL_ENAMETOOLONG:     return ENAMETOOLONG;
        case Error::PAL_ENETDOWN:         return ENETDOWN;
        case Error::PAL_ENETRESET:        return ENETRESET;
        case Error::PAL_ENETUNREACH:      return ENETUNREACH;
        case Error::PAL_ENFILE:           return ENFILE;
        case Error::PAL_ENOBUFS:          return ENOBUFS;
        case Error::PAL_ENODEV:           return ENODEV;
        case Error::PAL_ENOENT:           return ENOENT;
        case Error::PAL_ENOEXEC:          return ENOEXEC;
        case Error::PAL_ENOLCK:           return ENOLCK;
        case Error::PAL_ENOLINK:          return ENOLINK;
        case Error::PAL_ENOMEM:           return ENOMEM;
        case Error::PAL_ENOMSG:           return ENOMSG;
        case Error::PAL_ENOPROTOOPT:      return ENOPROTOOPT;
        case Error::PAL_ENOSPC:           return ENOSPC;
        case Error::PAL_ENOSYS:           return ENOSYS;
        case Error::PAL_ENOTCONN:         return ENOTCONN;
        case Error::PAL_ENOTDIR:          return ENOTDIR;
        case Error::PAL_ENOTEMPTY:        return ENOTEMPTY;
        case Error::PAL_ENOTRECOVERABLE:  return ENOTRECOVERABLE;
        case Error::PAL_ENOTSOCK:         return ENOTSOCK;
        case Error::PAL_ENOTSUP:          return ENOTSUP;
        case Error::PAL_ENOTTY:           return ENOTTY;
        case Error::PAL_ENXIO:            return ENXIO;
        case Error::PAL_EOVERFLOW:        return EOVERFLOW;
        case Error::PAL_EOWNERDEAD:       return EOWNERDEAD;
        case Error::PAL_EPERM:            return EPERM;
        case Error::PAL_EPIPE:            return EPIPE;
        case Error::PAL_EPROTO:           return EPROTO;
        case Error::PAL_EPROTONOSUPPORT:  return EPROTONOSUPPORT;
        case Error::PAL_EPROTOTYPE:       return EPROTOTYPE;
        case Error::PAL_ERANGE:           return ERANGE;
        case Error::PAL_EROFS:            return EROFS;
        case Error::PAL_ESPIPE:           return ESPIPE;
        case Error::PAL_ESRCH:            return ESRCH;
        case Error::PAL_ESTALE:           return ESTALE;
        case Error::PAL_ETIMEDOUT:        return ETIMEDOUT;
        case Error::PAL_ETXTBSY:          return ETXTBSY;
        case Error::PAL_EXDEV:            return EXDEV;
        case Error::PAL_ENONSTANDARD:     break; // fall through to assert
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
    assert(false);
    return -1;
}

extern "C"
const char* StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    assert(buffer != nullptr);
    assert(bufferSize > 0);

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
    const char* message = strerror_r(platformErrno, buffer, bufferSize);
    assert(message != nullptr);
    return message;
#else
    int error = strerror_r(platformErrno, buffer, bufferSize);
    if (error == ERANGE)
    {
        // Buffer is too small to hold the entire message, but has
        // still been filled to the extent possible and null-terminated.
        return nullptr; 
    }

    // The only other valid error codes are 0 for success or EINVAL for
    // an unkown error, but in the latter case a reasonable string (e.g
    // "Unknown error: 0x123) is returned.
    assert(error == 0 || error == EINVAL);
    return buffer;
#endif
}
