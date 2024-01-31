/**
 * \file
 * COM Interop Support
 *
 *
 * (C) 2002 Ximian, Inc.  http://www.ximian.com
 *
 */

#ifndef __MONO_COMINTEROP_H__
#define __MONO_COMINTEROP_H__

#include <mono/metadata/method-builder.h>
#include <mono/metadata/method-builder-ilgen.h>
#include <mono/metadata/marshal.h>

void
mono_cominterop_init (void);

MONO_API MONO_RT_EXTERNAL_ONLY MonoString *
mono_string_from_bstr (/*mono_bstr*/gpointer bstr);

MonoStringHandle
mono_string_from_bstr_checked (mono_bstr_const bstr, MonoError *error);

MONO_API void
mono_free_bstr (/*mono_bstr_const*/gpointer bstr);

#endif /* __MONO_COMINTEROP_H__ */
