/*
 * metadata/gc-internal.h: GC icalls.
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef __MONO_METADATA_GC_H__
#define __MONO_METADATA_GC_H__

#include <glib.h>
#include <mono/metadata/object-internals.h>

void   mono_object_register_finalizer               (MonoObject  *obj);
void   ves_icall_System_GC_InternalCollect          (int          generation);
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection);
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj);
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj);
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj);
void   ves_icall_System_GC_WaitForPendingFinalizers (void);

MonoObject *ves_icall_System_GCHandle_GetTarget (guint32 handle);
guint32     ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type);
void        ves_icall_System_GCHandle_FreeHandle (guint32 handle);
gpointer    ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle);

extern void mono_gc_init (void);
extern void mono_gc_base_init (void);
extern void mono_gc_cleanup (void);
extern void mono_gc_enable (void);
extern void mono_gc_disable (void);

/*
 * Return whenever the current thread is registered with the GC (i.e. started
 * by the GC pthread wrappers on unix.
 */
extern gboolean mono_gc_is_gc_thread (void);

/*
 * Try to register a foreign thread with the GC, if we fail or the backend
 * can't cope with this concept - we return FALSE.
 */
extern gboolean mono_gc_register_thread (void *baseptr);

/* only valid after the RECLAIM_START GC event and before RECLAIM_END
 * Not exported in public headers, but can be linked to (unsupported).
 */
extern gboolean mono_object_is_alive (MonoObject* obj);
extern gboolean mono_gc_is_finalizer_thread (MonoThread *thread);
extern gpointer mono_gc_out_of_memory (size_t size);
extern void     mono_gc_enable_events (void);

/* disappearing link functionality */
void        mono_gc_weak_link_add    (void **link_addr, MonoObject *obj);
void        mono_gc_weak_link_remove (void **link_addr);
MonoObject *mono_gc_weak_link_get    (void **link_addr);

/* simple interface for data structures needed in the runtime */
void* mono_gc_make_descr_from_bitmap (unsigned int *bitmap, int numbits);
/* desc is the result from mono_gc_make_descr*. A NULL value means
 * all the words contain GC pointers.
 * The memory is non-moving and it will be explicitly deallocated.
 * size bytes will be available from the returned address (ie, descr
 * must not be stored in the returned memory)
 */
void* mono_gc_alloc_fixed            (size_t size, void *descr);
void  mono_gc_free_fixed             (void* addr);

/* make sure the gchandle was allocated for an object in domain */
gboolean mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain);

/* if there are finalizers to run, run them. Returns the number of finalizers run */
int      mono_gc_invoke_finalizers  (void);
gboolean mono_gc_pending_finalizers (void);
void     mono_gc_finalize_notify    (void);

#endif /* __MONO_METADATA_GC_H__ */

