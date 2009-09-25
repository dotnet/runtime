/*
 * metadata/gc-internal.h: Internal GC interface
 *
 * Author: Paolo Molaro <lupus@ximian.com>
 *
 * (C) 2002 Ximian, Inc.
 */

#ifndef __MONO_METADATA_GC_INTERNAL_H__
#define __MONO_METADATA_GC_INTERNAL_H__

#include <glib.h>
#include <mono/metadata/object-internals.h>
#include <mono/metadata/threads-types.h>
#include <mono/utils/gc_wrapper.h>

#define mono_domain_finalizers_lock(domain) EnterCriticalSection (&(domain)->finalizable_objects_hash_lock);
#define mono_domain_finalizers_unlock(domain) LeaveCriticalSection (&(domain)->finalizable_objects_hash_lock);

#define MONO_GC_REGISTER_ROOT(x) mono_gc_register_root ((char*)&(x), sizeof(x), NULL)

#define MONO_GC_UNREGISTER_ROOT(x) mono_gc_deregister_root ((char*)&(x))

void   mono_object_register_finalizer               (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_InternalCollect          (int          generation) MONO_INTERNAL;
gint64 ves_icall_System_GC_GetTotalMemory           (MonoBoolean  forceCollection) MONO_INTERNAL;
void   ves_icall_System_GC_KeepAlive                (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_ReRegisterForFinalize    (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_SuppressFinalize         (MonoObject  *obj) MONO_INTERNAL;
void   ves_icall_System_GC_WaitForPendingFinalizers (void) MONO_INTERNAL;

MonoObject *ves_icall_System_GCHandle_GetTarget (guint32 handle) MONO_INTERNAL;
guint32     ves_icall_System_GCHandle_GetTargetHandle (MonoObject *obj, guint32 handle, gint32 type) MONO_INTERNAL;
void        ves_icall_System_GCHandle_FreeHandle (guint32 handle) MONO_INTERNAL;
gpointer    ves_icall_System_GCHandle_GetAddrOfPinnedObject (guint32 handle) MONO_INTERNAL;

extern void mono_gc_init (void) MONO_INTERNAL;
extern void mono_gc_base_init (void) MONO_INTERNAL;
extern void mono_gc_cleanup (void) MONO_INTERNAL;
extern void mono_gc_enable (void) MONO_INTERNAL;
extern void mono_gc_disable (void) MONO_INTERNAL;

/*
 * Return whenever the current thread is registered with the GC (i.e. started
 * by the GC pthread wrappers on unix.
 */
extern gboolean mono_gc_is_gc_thread (void) MONO_INTERNAL;

/*
 * Try to register a foreign thread with the GC, if we fail or the backend
 * can't cope with this concept - we return FALSE.
 */
extern gboolean mono_gc_register_thread (void *baseptr) MONO_INTERNAL;

extern gboolean mono_gc_is_finalizer_internal_thread (MonoInternalThread *thread) MONO_INTERNAL;

/* only valid after the RECLAIM_START GC event and before RECLAIM_END
 * Not exported in public headers, but can be linked to (unsupported).
 */
extern gboolean mono_object_is_alive (MonoObject* obj);
extern gboolean mono_gc_is_finalizer_thread (MonoThread *thread);
extern gpointer mono_gc_out_of_memory (size_t size);
extern void     mono_gc_enable_events (void);

/* disappearing link functionality */
void        mono_gc_weak_link_add    (void **link_addr, MonoObject *obj, gboolean track) MONO_INTERNAL;
void        mono_gc_weak_link_remove (void **link_addr) MONO_INTERNAL;
MonoObject *mono_gc_weak_link_get    (void **link_addr) MONO_INTERNAL;

#ifndef HAVE_SGEN_GC
void    mono_gc_add_weak_track_handle    (MonoObject *obj, guint32 gchandle) MONO_INTERNAL;
void    mono_gc_change_weak_track_handle (MonoObject *old_obj, MonoObject *obj, guint32 gchandle) MONO_INTERNAL;
void    mono_gc_remove_weak_track_handle (guint32 gchandle) MONO_INTERNAL;
GSList* mono_gc_remove_weak_track_object (MonoDomain *domain, MonoObject *obj) MONO_INTERNAL;
#endif

MonoBoolean
GCHandle_CheckCurrentDomain (guint32 gchandle) MONO_INTERNAL;

/* simple interface for data structures needed in the runtime */
void* mono_gc_make_descr_from_bitmap (gsize *bitmap, int numbits) MONO_INTERNAL;

/* User defined marking function */
/* It should work like this:
 * foreach (ref in GC references in the are structure pointed to by ADDR)
 *    *ref = mark_func (*ref)
 */
typedef void *(*MonoGCCopyFunc) (void *addr);
typedef void (*MonoGCMarkFunc) (void *addr, MonoGCCopyFunc mark_func);

/* Create a descriptor with a user defined marking function */
void *mono_gc_make_root_descr_user (MonoGCMarkFunc marker);

/* desc is the result from mono_gc_make_descr*. A NULL value means
 * all the words might contain GC pointers.
 * The memory is non-moving and it will be explicitly deallocated.
 * size bytes will be available from the returned address (ie, descr
 * must not be stored in the returned memory)
 */
void* mono_gc_alloc_fixed            (size_t size, void *descr) MONO_INTERNAL;
void  mono_gc_free_fixed             (void* addr) MONO_INTERNAL;

/* make sure the gchandle was allocated for an object in domain */
gboolean mono_gchandle_is_in_domain (guint32 gchandle, MonoDomain *domain) MONO_INTERNAL;
void     mono_gchandle_free_domain  (MonoDomain *domain) MONO_INTERNAL;

typedef void (*FinalizerThreadCallback) (gpointer user_data);

/* if there are finalizers to run, run them. Returns the number of finalizers run */
gboolean mono_gc_pending_finalizers (void) MONO_INTERNAL;
void     mono_gc_finalize_notify    (void) MONO_INTERNAL;

void* mono_gc_alloc_pinned_obj (MonoVTable *vtable, size_t size) MONO_INTERNAL;
void* mono_gc_alloc_obj (MonoVTable *vtable, size_t size) MONO_INTERNAL;
void* mono_gc_make_descr_for_string (gsize *bitmap, int numbits) MONO_INTERNAL;
void* mono_gc_make_descr_for_object (gsize *bitmap, int numbits, size_t obj_size) MONO_INTERNAL;
void* mono_gc_make_descr_for_array (int vector, gsize *elem_bitmap, int numbits, size_t elem_size) MONO_INTERNAL;

void  mono_gc_register_for_finalization (MonoObject *obj, void *user_data) MONO_INTERNAL;
void  mono_gc_add_memory_pressure (gint64 value) MONO_INTERNAL;
int   mono_gc_register_root (char *start, size_t size, void *descr) MONO_INTERNAL;
void  mono_gc_deregister_root (char* addr) MONO_INTERNAL;
int   mono_gc_finalizers_for_domain (MonoDomain *domain, MonoObject **out_array, int out_size) MONO_INTERNAL;
void  mono_gc_run_finalize (void *obj, void *data) MONO_INTERNAL;
void  mono_gc_clear_domain (MonoDomain * domain) MONO_INTERNAL;

/* 
 * Register a root which can only be written using a write barrier.
 * Writes to the root must be done using a write barrier (MONO_ROOT_SETREF).
 * If the root uses an user defined mark routine, the writes are not required to be
 * to the area between START and START+SIZE.
 * The write barrier allows the GC to avoid scanning this root at each collection, so it
 * is more efficient.
 * FIXME: Add an API for clearing remset entries if a root with a user defined
 * mark routine is deleted.
 */
int mono_gc_register_root_wbarrier (char *start, size_t size, void *descr) MONO_INTERNAL;

void mono_gc_wbarrier_set_root (gpointer ptr, MonoObject *value) MONO_INTERNAL;

/* Set a field of a root registered using mono_gc_register_root_wbarrier () */
#define MONO_ROOT_SETREF(s,fieldname,value) do {	\
	mono_gc_wbarrier_set_root (&((s)->fieldname), (MonoObject*)value); \
} while (0)

void  mono_gc_finalize_threadpool_threads (void) MONO_INTERNAL;

/* fast allocation support */
MonoMethod* mono_gc_get_managed_allocator (MonoVTable *vtable, gboolean for_box) MONO_INTERNAL;
int mono_gc_get_managed_allocator_type (MonoMethod *managed_alloc) MONO_INTERNAL;
MonoMethod *mono_gc_get_managed_allocator_by_type (int atype) MONO_INTERNAL;

guint32 mono_gc_get_managed_allocator_types (void) MONO_INTERNAL;

/* Fast write barriers */
MonoMethod* mono_gc_get_write_barrier (void) MONO_INTERNAL;

/* helper for the managed alloc support */
MonoString *mono_string_alloc (int length) MONO_INTERNAL;

/* 
 * Functions supplied by the runtime and called by the GC. Currently only used
 * by SGEN.
 */
typedef struct {
	/* 
	 * Function called during thread startup/attach to allocate thread-local data 
	 * needed by the other functions.
	 */
	gpointer (*thread_attach_func) (void);
	/* FIXME: Add a cleanup function too */
	/* 
	 * Function called from every thread when suspending for GC. It can save
	 * data needed for marking from thread stacks. user_data is the data returned 
	 * by attach_func. This might called with GC locks held and the word stopped,
	 * so it shouldn't do any synchronization etc.
	 */
	void (*thread_suspend_func) (gpointer user_data, void *sigcontext);
	/* 
	 * Function called to mark from thread stacks. user_data is the data returned 
	 * by attach_func. This is called twice, with the word stopped:
	 * - in the first pass, it should mark areas of the stack using
	 *   conservative marking by calling mono_gc_conservatively_scan_area ().
	 * - in the second pass, it should mark the remaining areas of the stack
	 *   using precise marking by calling mono_gc_scan_object ().
	 */
	void (*thread_mark_func) (gpointer user_data, guint8 *stack_start, guint8 *stack_end, gboolean precise);
} MonoGCCallbacks;

/* Set the callback functions callable by the GC */
void mono_gc_set_gc_callbacks (MonoGCCallbacks *callbacks) MONO_INTERNAL;

/* Functions callable from the thread mark func */

/* Scan the memory area between START and END conservatively */
void mono_gc_conservatively_scan_area (void *start, void *end) MONO_INTERNAL;

/* Scan OBJ, returning its new address */
void *mono_gc_scan_object (void *obj) MONO_INTERNAL;

/* Return the bitmap encoded by a descriptor */
gsize* mono_gc_get_bitmap_for_descr (void *descr, int *numbits) MONO_INTERNAL;

#endif /* __MONO_METADATA_GC_INTERNAL_H__ */

