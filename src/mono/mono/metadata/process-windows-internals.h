/*
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_PROCESS_WINDOWS_INTERNALS_H__
#define __MONO_METADATA_PROCESS_WINDOWS_INTERNALS_H__

#include <config.h>
#include <glib.h>

#ifdef HOST_WIN32
#include "mono/metadata/process.h"
#include "mono/metadata/process-internals.h"
#include "mono/metadata/object.h"
#include "mono/metadata/object-internals.h"
#include "mono/metadata/exception.h"

// On platforms not using classic WIN API support the  implementation of bellow methods are hosted in separate source file
// process-windows-*.c. On platforms using classic WIN API the implementation is still keept in process.c and still declared
// static and in some places even inlined.
#if !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT)
gboolean
mono_process_win_enum_processes (DWORD *pids, DWORD count, DWORD *needed);
#endif  /* !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT) */

#endif /* HOST_WIN32 */

#endif /* __MONO_METADATA_PROCESS_WINDOWS_INTERNALS_H__ */
