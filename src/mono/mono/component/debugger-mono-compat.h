// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// File: DEBUGGER-MONO-COMPAT.H
//

#ifndef __DBG_MONO_MONO_COMPAT_H__
#define __DBG_MONO_MONO_COMPAT_H__

#include <glib.h>
#include <mono/utils/atomic.h>

static
inline
int32_t
dbg_rt_atomic_inc_int32_t (volatile int32_t *value)
{
	return (int32_t)mono_atomic_inc_i32 ((volatile gint32 *)value);
}

#endif