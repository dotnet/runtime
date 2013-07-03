/*
 * mono-gc.h: GC related public interface
 *
 */
#ifndef __METADATA_MONO_GC_H__
#define __METADATA_MONO_GC_H__

#include <mono/metadata/object.h>

MONO_BEGIN_DECLS

typedef int (*MonoGCReferences) (MonoObject *obj, MonoClass *klass, uintptr_t size, uintptr_t num, MonoObject **refs, uintptr_t *offsets, void *data);

MONO_API void   mono_gc_collect         (int generation);
MONO_API int    mono_gc_max_generation  (void);
MONO_API int    mono_gc_get_generation  (MonoObject *object);
MONO_API int    mono_gc_collection_count (int generation);
MONO_API int64_t mono_gc_get_used_size   (void);
MONO_API int64_t mono_gc_get_heap_size   (void);
MONO_API int    mono_gc_invoke_finalizers (void);
/* heap walking is only valid in the pre-stop-world event callback */
MONO_API int    mono_gc_walk_heap        (int flags, MonoGCReferences callback, void *data);

MONO_END_DECLS

#endif /* __METADATA_MONO_GC_H__ */

