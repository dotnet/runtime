// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
#ifndef _MONO_METADATA_MONO_GC_TYPES_H
#define _MONO_METADATA_MONO_GC_TYPES_H

#include <mono/metadata/details/object-types.h>

MONO_BEGIN_DECLS

typedef int (*MonoGCReferences) (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, uintptr_t *offsets, void *data);

/**
 * This enum is used by the profiler API when reporting root registration.
 */
typedef enum {
	/**
	 * Roots external to Mono. Embedders may only use this value.
	 */
	MONO_ROOT_SOURCE_EXTERNAL = 0,
	/**
	 * Thread call stack.
	 *
	 * The \c key parameter is a thread ID as a \c uintptr_t.
	 */
	MONO_ROOT_SOURCE_STACK = 1,
	/**
	 * Roots in the finalizer queue. This is a pseudo-root.
	 */
	MONO_ROOT_SOURCE_FINALIZER_QUEUE = 2,
	/**
	 * Managed \c static variables.
	 *
	 * The \c key parameter is a \c MonoVTable pointer.
	 */
	MONO_ROOT_SOURCE_STATIC = 3,
	/**
	 * Managed \c static variables with \c ThreadStaticAttribute.
	 *
	 * The \c key parameter is a thread ID as a \c uintptr_t.
	 */
	MONO_ROOT_SOURCE_THREAD_STATIC = 4,
	/**
	 * Managed \c static variables with \c ContextStaticAttribute.
	 *
	 * The \c key parameter is a \c MonoAppContext pointer.
	 */
	MONO_ROOT_SOURCE_CONTEXT_STATIC = 5,
	/**
	 * \c GCHandle structures.
	 */
	MONO_ROOT_SOURCE_GC_HANDLE = 6,
	/**
	 * Roots in the just-in-time compiler.
	 */
	MONO_ROOT_SOURCE_JIT = 7,
	/**
	 * Roots in the threading subsystem.
	 *
	 * The \c key parameter, if not \c NULL, is a thread ID as a \c uintptr_t.
	 */
	MONO_ROOT_SOURCE_THREADING = 8,
	/**
	 * Roots in application domains.
	 *
	 * The \c key parameter, if not \c NULL, is a \c MonoDomain pointer.
	 */
	MONO_ROOT_SOURCE_DOMAIN = 9,
	/**
	 * Roots in reflection code.
	 *
	 * The \c key parameter, if not \c NULL, is a \c MonoVTable pointer.
	 */
	MONO_ROOT_SOURCE_REFLECTION = 10,
	/**
	 * Roots from P/Invoke or other marshaling infrastructure.
	 */
	MONO_ROOT_SOURCE_MARSHAL = 11,
	/**
	 * Roots in the thread pool data structures.
	 */
	MONO_ROOT_SOURCE_THREAD_POOL = 12,
	/**
	 * Roots in the debugger agent.
	 */
	MONO_ROOT_SOURCE_DEBUGGER = 13,
	/**
	 * Roots in the runtime handle stack. This is a pseudo-root.
	 *
	 * The \c key parameter is a thread ID as a \c uintptr_t.
	 */
	MONO_ROOT_SOURCE_HANDLE = 14,
	/**
	 * Roots in the ephemeron arrays. This is a pseudo-root.
	 */
	MONO_ROOT_SOURCE_EPHEMERON = 15,
	/**
	 * Roots in the toggleref arrays. This is a pseudo-root.
	 */
	MONO_ROOT_SOURCE_TOGGLEREF = 16,
} MonoGCRootSource;

typedef enum {
	MONO_GC_HANDLE_TYPE_MIN = 0,
	MONO_GC_HANDLE_WEAK = MONO_GC_HANDLE_TYPE_MIN,
	MONO_GC_HANDLE_WEAK_TRACK_RESURRECTION,
	MONO_GC_HANDLE_NORMAL,
	MONO_GC_HANDLE_PINNED,
	MONO_GC_HANDLE_TYPE_MAX,
} MonoGCHandleType;

MONO_END_DECLS

#endif /* _MONO_METADATA_MONO_GC_TYPES_H */
