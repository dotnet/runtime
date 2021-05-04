// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DEBUGGER-CORECLR-COMPAT.H
//

#ifndef __DBG_CORECLR_MONO_COMPAT_H__
#define __DBG_CORECLR_MONO_COMPAT_H__

#define g_malloc malloc
#define g_free free
#define g_assert assert
#define g_realloc realloc
#include "stdafx.h"

static inline int32_t dbg_rt_atomic_inc_int32_t(volatile int32_t* value)
{
    STATIC_CONTRACT_NOTHROW;
    return static_cast<int32_t>(InterlockedIncrement((volatile LONG*)(value)));
}

#endif