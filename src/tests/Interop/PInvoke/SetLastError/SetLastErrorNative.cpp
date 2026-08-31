// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <platformdefines.h>

#ifndef WINDOWS
#include <errno.h>
#endif

extern "C" DLL_EXPORT void STDMETHODCALLTYPE SetError(int err, bool shouldSetError)
{
    if (!shouldSetError)
        return;

#ifdef WINDOWS
    ::SetLastError(err);
#else
    errno = err;
#endif
}
