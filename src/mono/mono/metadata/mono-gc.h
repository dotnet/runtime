/*
 * mono-gc.h: GC related public interface
 *
 */
#ifndef __METADATA_MONO_GC_H__
#define __METADATA_MONO_GC_H__

#include <mono/metadata/object.h>

MONO_BEGIN_DECLS

typedef int (*MonoGCReferences) (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, uintptr_t *offsets, void *data);

typedef enum {
	// Roots external to Mono.  Embedders may only use this value.
	MONO_ROOT_SOURCE_EXTERNAL = 0,
	// Thread stack.  Must not be used to register roots.
	MONO_ROOT_SOURCE_STACK = 1,
	// Roots in the finalizer queue.  Must not be used to register roots.
	MONO_ROOT_SOURCE_FINALIZER_QUEUE = 2,
	// Managed static variables.
	MONO_ROOT_SOURCE_STATIC = 3,
	// Static variables with ThreadStaticAttribute.
	MONO_ROOT_SOURCE_THREAD_STATIC = 4,
	// Static variables with ContextStaticAttribute.
	MONO_ROOT_SOURCE_CONTEXT_STATIC = 5,
	// GCHandle structures.
	MONO_ROOT_SOURCE_GC_HANDLE = 6,
	// Roots in the just-in-time compiler.
	MONO_ROOT_SOURCE_JIT = 7,
	// Roots in the threading subsystem.
	MONO_ROOT_SOURCE_THREADING = 8,
	// Roots in application domains.
	MONO_ROOT_SOURCE_DOMAIN = 9,
	// Roots in reflection code.
	MONO_ROOT_SOURCE_REFLECTION = 10,
	// Roots from P/Invoke or other marshaling.
	MONO_ROOT_SOURCE_MARSHAL = 11,
	// Roots in the thread pool data structures.
	MONO_ROOT_SOURCE_THREAD_POOL = 12,
	// Roots in the debugger agent.
	MONO_ROOT_SOURCE_DEBUGGER = 13,
	// Handle structures, used for object passed to internal functions
	MONO_ROOT_SOURCE_HANDLE = 14,
} MonoGCRootSource;

MONO_API void   mono_gc_collect         (int generation);
MONO_API int    mono_gc_max_generation  (void);
MONO_API int    mono_gc_get_generation  (MonoObject *object);
MONO_API int    mono_gc_collection_count (int generation);
MONO_API int64_t mono_gc_get_used_size   (void);
MONO_API int64_t mono_gc_get_heap_size   (void);
MONO_API MonoBoolean mono_gc_pending_finalizers (void);
MONO_API void     mono_gc_finalize_notify    (void);
MONO_API int    mono_gc_invoke_finalizers (void);
/* heap walking is only valid in the pre-stop-world event callback */
MONO_API int    mono_gc_walk_heap        (int flags, MonoGCReferences callback, void *data);

MONO_END_DECLS

#endif /* __METADATA_MONO_GC_H__ */

