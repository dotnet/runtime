// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "pal_config.h"
#include "pal_errno.h"
#include "pal_utilities.h"

#include <netdb.h>
#include <string.h>
#include <assert.h>

int32_t SystemNative_ConvertErrorPlatformToPal(int32_t platformErrno)
{
    return ConvertErrorPlatformToPal(platformErrno);
}

int32_t SystemNative_ConvertErrorPalToPlatform(int32_t error)
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
        case Error_ENODATA:
            return ENODATA;
        case Error_EHOSTNOTFOUND:
            return -(Error_EHOSTNOTFOUND);
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

static int32_t SystemNative_ConvertErrorPalToGai(int32_t error)
{
    switch (error)
    {
        case -(Error_EHOSTNOTFOUND):
            return EAI_NONAME;
    }
    // Fall-through for unknown codes. gai_strerror() will handle that.

    return error;
}



const char* SystemNative_StrErrorR(int32_t platformErrno, char* buffer, int32_t bufferSize)
{
    assert(buffer != NULL);
    assert(bufferSize > 0);

    if (bufferSize < 0)
        return NULL;

    if (platformErrno < 0)
    {
        // Not a system error
        SafeStringCopy(buffer, (size_t)bufferSize, gai_strerror(SystemNative_ConvertErrorPalToGai(platformErrno)));
        return buffer;
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
