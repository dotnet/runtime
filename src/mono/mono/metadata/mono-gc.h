/*
 * mono-gc.h: GC related public interface
 *
 */
#ifndef __METADATA_MONO_GC_H__
#define __METADATA_MONO_GC_H__

#include <mono/metadata/object.h>

G_BEGIN_DECLS

void   mono_gc_collect         (int generation);
int    mono_gc_max_generation  (void);
int    mono_gc_get_generation  (MonoObject *object);
int    mono_gc_collection_count (int generation);
gint64 mono_gc_get_used_size   (void);
gint64 mono_gc_get_heap_size   (void);
int    mono_gc_invoke_finalizers (void);

G_END_DECLS

#endif /* __METADATA_MONO_GC_H__ */

