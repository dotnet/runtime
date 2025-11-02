// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#ifndef MINIPAL_COM_MEMORY_H
#define MINIPAL_COM_MEMORY_H

#include <stdlib.h>

#ifdef __cplusplus
    extern "C"
    {
#endif // __cplusplus

#ifndef HOST_WINDOWS
inline void* CoTaskMemAlloc(size_t cb)
{
    return malloc(cb);
}

inline void CoTaskMemFree(void* pv)
{
    free(pv);
}
#endif

#ifdef __cplusplus
    }
#endif // __cplusplus

#endif // MINIPAL_COM_MEMORY_H
