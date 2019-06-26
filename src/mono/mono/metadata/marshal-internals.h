/**
 * \file
 * Copyright 2016 Microsoft
 * Licensed under the MIT license. See LICENSE file in the project root for full license information.
 */
#ifndef __MONO_METADATA_MARSHAL_INTERNALS_H__
#define __MONO_METADATA_MARSHAL_INTERNALS_H__

#include <config.h>
#include <glib.h>
#include <mono/metadata/object-internals.h>

MonoObjectHandle
mono_marshal_xdomain_copy_value_handle (MonoObjectHandle val, MonoError *error);

// On Windows platform implementation of bellow methods are hosted in separate source file
// marshal-windows.c or marshal-windows-*.c. On other platforms the implementation is still keept
// in marshal.c still declared as static and in some places even inlined.
#ifdef HOST_WIN32
void*
mono_marshal_alloc_co_task_mem (size_t size);

void
mono_marshal_free_co_task_mem (void *ptr);

gpointer
mono_marshal_realloc_co_task_mem (gpointer ptr, size_t size);

void*
mono_marshal_alloc_hglobal (size_t size, MonoError *error);

gpointer
mono_marshal_realloc_hglobal (gpointer ptr, size_t size);

void
mono_marshal_free_hglobal (void *ptr);

#endif // HOST_WIN32

typedef enum {
	TYPECHECK_OBJECT_ARG_POS = 0,
	TYPECHECK_CLASS_ARG_POS = 1,
	TYPECHECK_CACHE_ARG_POS = 2
} MarshalTypeCheckPositions;

#endif /* __MONO_METADATA_MARSHAL_INTERNALS_H__ */
