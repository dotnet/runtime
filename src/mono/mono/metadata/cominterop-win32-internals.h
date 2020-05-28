/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_COMINTEROP_WIN32_INTERNALS_H__
#define __MONO_METADATA_COMINTEROP_WIN32_INTERNALS_H__

#include <config.h>
#include <glib.h>

// On some Windows platforms the implementation of below methods are hosted
// in separate source files like cominterop-win32-*.c. On other platforms,
// the implementation is kept in cominterop.c and declared as static and in some
// cases even inline.
#if defined(HOST_WIN32) && !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT)

guint32
mono_marshal_win_safearray_get_dim (gpointer safearray);

int
mono_marshal_win_safe_array_get_lbound (gpointer psa, guint nDim, glong* plLbound);

int
mono_marshal_win_safe_array_get_ubound (gpointer psa, guint nDim, glong* plUbound);

int
mono_marshal_win_safearray_get_value (gpointer safearray, gpointer indices, gpointer *result);

void
mono_marshal_win_safearray_end (gpointer safearray, gpointer indices);

gboolean
mono_marshal_win_safearray_create_internal (UINT cDims, SAFEARRAYBOUND *rgsabound, gpointer *newsafearray);

int
mono_marshal_win_safearray_set_value (gpointer safearray, gpointer indices, gpointer value);

#endif /* HOST_WIN32 && !G_HAVE_API_SUPPORT(HAVE_CLASSIC_WINAPI_SUPPORT | HAVE_UWP_WINAPI_SUPPORT) */

#endif /* __MONO_METADATA_COMINTEROP_WIN32_INTERNALS_H__ */
